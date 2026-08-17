using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Renders the equipment grid as five labeled body-part slots (head, chest, legs,
    /// weapon, accessory) plus a line with total equipped stats. Dragging an item onto
    /// the matching slot equips it; dragging it out unequips — all through the shared
    /// <see cref="SlotView"/> / <see cref="InventoryGrid.Move"/> path.
    /// </summary>
    public class EquipmentPanelUI : MonoBehaviour
    {
        private EquipmentSystem equipment;
        private RarityPalette palette = new RarityPalette();
        private readonly List<SlotView> views = new List<SlotView>();
        private Text statsText;

        public void Bind(EquipmentSystem equipment, RarityPalette palette, RectTransform slotColumn, Text statsText)
        {
            this.equipment = equipment;
            if (palette != null) this.palette = palette;
            this.statsText = statsText;

            var grid = equipment.Grid;
            var layout = slotColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            string[] names = { "Head", "Chest", "Legs", "Weapon", "Accessory" };
            for (int i = 0; i < grid.Count; i++)
            {
                var row = UIFactory.CreateRect(slotColumn, "Slot" + i, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                UIFactory.SetHeight(row, 84f);

                var slotRect = UIFactory.CreateAnchored(row, "Cell", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(76f, 76f));
                slotRect.offsetMin = new Vector2(0f, -38f);
                slotRect.offsetMax = new Vector2(76f, 38f);
                var view = slotRect.gameObject.AddComponent<SlotView>();
                string hint = names[i < names.Length ? i : i];
                view.Bind(grid, i, palette, hint);
                views.Add(view);

                var label = UIFactory.CreateText(row, "Label", names[i < names.Length ? i : i], 19,
                    palette.textSecondary, TextAnchor.MiddleLeft, FontStyle.Bold);
                label.rectTransform.anchorMin = new Vector2(86f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 1f);
                label.rectTransform.offsetMin = new Vector2(86f, 0f);
                label.rectTransform.offsetMax = new Vector2(0f, 0f);
            }

            equipment.EquipmentChanged += OnEquipmentChanged;
            RefreshStats();
        }

        private void OnEquipmentChanged(int _)
        {
            foreach (var view in views)
                view.Refresh();
            RefreshStats();
        }

        private void RefreshStats()
        {
            if (statsText == null || equipment == null) return;
            statsText.text = $"Total: {equipment.GetTotalStat(StatType.Damage)} dmg · {equipment.GetTotalStat(StatType.Armor)} armor";
        }
    }
}
