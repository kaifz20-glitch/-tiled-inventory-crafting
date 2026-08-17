using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TiledInventory
{
    public enum SortMode
    {
        Default,
        NameAsc,
        RarityDesc,
        Category,
        CountDesc
    }

    /// <summary>
    /// Renders an <see cref="InventoryGrid"/> as a tiled grid of <see cref="SlotView"/>s
    /// with rarity color-coding, plus a toolbar: text search, category filter and sorting.
    /// Subscribes to grid events so the view stays in sync with the data at all times.
    /// </summary>
    public class InventoryGridView : MonoBehaviour
    {
        [SerializeField] private float cellSize = 76f;
        [SerializeField] private float cellSpacing = 6f;

        private InventoryGrid grid;
        private RarityPalette palette = new RarityPalette();

        private RectTransform slotContainer;
        private GridLayoutGroup layout;
        private readonly Dictionary<int, SlotView> views = new Dictionary<int, SlotView>();

        private string searchText = "";
        private ItemCategory categoryFilter = (ItemCategory)(-1); // -1 = all
        private SortMode sortMode = SortMode.Default;

        public InventoryGrid Grid => grid;

        public void Bind(InventoryGrid grid, RarityPalette palette, RectTransform slotContainer)
        {
            this.grid = grid;
            if (palette != null) this.palette = palette;
            this.slotContainer = slotContainer;

            layout = slotContainer.gameObject.GetComponent<GridLayoutGroup>();
            if (layout == null)
            {
                layout = slotContainer.gameObject.AddComponent<GridLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                layout.startAxis = GridLayoutGroup.Axis.Horizontal;
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = grid.Size.x;
            }
            layout.cellSize = new Vector2(cellSize, cellSize);
            layout.spacing = new Vector2(cellSpacing, cellSpacing);
            layout.padding = new RectOffset(12, 12, 6, 6);

            grid.SlotChanged += OnSlotChanged;
            grid.Changed += Rebuild;

            Rebuild();
        }

        private void OnDestroy()
        {
            if (grid != null)
            {
                grid.SlotChanged -= OnSlotChanged;
                grid.Changed -= Rebuild;
            }
        }

        /// <summary>Destroy and recreate all slot views (grid resize, clear, load).</summary>
        public void Rebuild()
        {
            if (grid == null) return;
            foreach (var kv in views)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            views.Clear();

            if (layout != null) layout.constraintCount = grid.Size.x;

            for (int i = 0; i < grid.Count; i++)
            {
                var cell = UIFactory.CreateRect(slotContainer, "Slot" + i,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var view = cell.gameObject.AddComponent<SlotView>();
                view.Bind(grid, i, palette);
                views[i] = view;
            }

            ApplyFilterAndSort();
        }

        // ------------------------------------------------------------------ data → view

        private void OnSlotChanged(int index)
        {
            if (views.TryGetValue(index, out var view) && view != null)
                view.Refresh();
        }

        public void RefreshAll()
        {
            foreach (var kv in views)
                if (kv.Value != null) kv.Value.Refresh();
        }

        // ------------------------------------------------------------------ toolbar

        /// <summary>Build the toolbar (search input, category filter, sort) into <paramref name="toolbar"/>.</summary>
        public void BuildToolbar(RectTransform toolbar)
        {
            var paletteRef = palette;
            var searchField = UIFactory.CreateInputField(toolbar, "Search", "Search items...", 20, s =>
            {
                searchText = s.ToLowerInvariant();
                ApplyFilterAndSort();
            }, paletteRef.slotBackground);
            var searchRect = (RectTransform)searchField.transform;
            searchRect.anchorMin = new Vector2(0f, 0f);
            searchRect.anchorMax = new Vector2(0.48f, 1f);
            searchRect.offsetMin = new Vector2(0f, 0f);
            searchRect.offsetMax = new Vector2(0f, 0f);

            var categoryOptions = new List<string> { "All" };
            foreach (ItemCategory cat in Enum.GetValues(typeof(ItemCategory)))
                categoryOptions.Add(cat.ToString());
            var categorySelect = UIFactory.CreateSelect(toolbar, "Category", categoryOptions, 0, idx =>
            {
                categoryFilter = idx == 0 ? (ItemCategory)(-1) : (ItemCategory)(idx - 1);
                ApplyFilterAndSort();
            }, paletteRef, 18);
            categorySelect.Root.anchorMin = new Vector2(0.5f, 0f);
            categorySelect.Root.anchorMax = new Vector2(0.78f, 1f);
            categorySelect.Root.offsetMin = new Vector2(4f, 0f);
            categorySelect.Root.offsetMax = new Vector2(4f, 0f);

            var sortOptions = new List<string> { "Default", "Name", "Rarity", "Type", "Count" };
            var sortSelect = UIFactory.CreateSelect(toolbar, "Sort", sortOptions, 0, idx =>
            {
                sortMode = (SortMode)idx;
                ApplyFilterAndSort();
            }, paletteRef, 18);
            sortSelect.Root.anchorMin = new Vector2(0.8f, 0f);
            sortSelect.Root.anchorMax = new Vector2(1f, 1f);
            sortSelect.Root.offsetMin = new Vector2(4f, 0f);
            sortSelect.Root.offsetMax = new Vector2(0f, 0f);
        }

        // ------------------------------------------------------------------ filter/sort

        private void ApplyFilterAndSort()
        {
            if (grid == null) return;

            var order = new List<int>();
            for (int i = 0; i < grid.Count; i++)
            {
                var slot = grid.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;

                var item = slot.stack.item;
                if (!MatchesSearch(item)) continue;
                if (categoryFilter != (ItemCategory)(-1) && item.Category != categoryFilter) continue;
                order.Add(i);
            }

            switch (sortMode)
            {
                case SortMode.NameAsc:
                    order.Sort((a, b) => string.Compare(Label(a), Label(b), StringComparison.OrdinalIgnoreCase));
                    break;
                case SortMode.RarityDesc:
                    order.Sort((a, b) => grid.GetSlot(b).stack.item.Rarity.CompareTo(grid.GetSlot(a).stack.item.Rarity));
                    break;
                case SortMode.Category:
                    order.Sort((a, b) => grid.GetSlot(a).stack.item.Category.CompareTo(grid.GetSlot(b).stack.item.Category));
                    break;
                case SortMode.CountDesc:
                    order.Sort((a, b) => grid.GetSlot(b).stack.count.CompareTo(grid.GetSlot(a).stack.count));
                    break;
            }

            foreach (var kv in views)
            {
                if (kv.Value == null) continue;
                kv.Value.gameObject.SetActive(order.Contains(kv.Key));
            }
            for (int i = 0; i < order.Count; i++)
            {
                if (views.TryGetValue(order[i], out var view) && view != null)
                    view.transform.SetAsLastSibling();
            }
        }

        private bool MatchesSearch(ItemDefinition item)
        {
            if (string.IsNullOrEmpty(searchText)) return true;
            return item.DisplayName.ToLowerInvariant().Contains(searchText) ||
                   (item.Description != null && item.Description.ToLowerInvariant().Contains(searchText));
        }

        private string Label(int index)
        {
            var slot = grid.GetSlot(index);
            return slot != null && !slot.IsEmpty ? slot.stack.item.DisplayName : "";
        }
    }
}
