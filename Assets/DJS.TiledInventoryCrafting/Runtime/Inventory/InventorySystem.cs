using System.Collections.Generic;
using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Owns all inventory grids for one player: the main bag plus the equipment grid.
    /// Grids are addressable by name so systems (crafting, trading, saving) can find them
    /// without hard-coded references.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventorySystem : MonoBehaviour
    {
        [Tooltip("Grids are looked up by name. Keep names stable — save files reference them.")]
        [SerializeField] private List<InventoryGrid> grids = new List<InventoryGrid>();

        [Tooltip("Dimensions of the default 'Main' bag grid when it does not exist yet. " +
                 "Change this to configure the bag size without touching code.")]
        [SerializeField] private Vector2Int defaultMainGridSize = new Vector2Int(5, 5);

        public Vector2Int DefaultMainGridSize => defaultMainGridSize;

        /// <summary>The default backpack grid. Created on demand if missing.</summary>
        public InventoryGrid MainGrid => GetOrCreateGrid("Main", defaultMainGridSize);

        /// <summary>The equipment grid (head/chest/legs/weapon/accessory).</summary>
        public InventoryGrid EquipmentGrid => GetOrCreateGrid("Equipment", new Vector2Int(1, 5));

        /// <summary>Raised after any grid in this system changes.</summary>
        public event System.Action Changed;

        public IReadOnlyList<InventoryGrid> Grids => grids;

        private readonly HashSet<InventoryGrid> subscribed = new HashSet<InventoryGrid>();

        private void Awake()
        {
            EnsureDefaults();
            foreach (var grid in grids)
                Subscribe(grid);
        }

        private void EnsureDefaults()
        {
            GetOrCreateGrid("Main", defaultMainGridSize);
            GetOrCreateGrid("Equipment", new Vector2Int(1, 5));
        }

        /// <summary>Subscribe a grid's events exactly once.</summary>
        private void Subscribe(InventoryGrid grid)
        {
            if (grid == null || !subscribed.Add(grid)) return;
            grid.Changed += () => Changed?.Invoke();
            grid.SlotChanged += _ => Changed?.Invoke();
        }

        /// <summary>Find a grid by name, or null.</summary>
        public InventoryGrid GetGrid(string name)
        {
            for (int i = 0; i < grids.Count; i++)
                if (grids[i].GridName == name)
                    return grids[i];
            return null;
        }

        /// <summary>Find a grid by name, creating it (with default restrictions) if missing.</summary>
        public InventoryGrid GetOrCreateGrid(string name, Vector2Int size)
        {
            var existing = GetGrid(name);
            if (existing != null) return existing;
            var grid = new InventoryGrid(name, size);
            grids.Add(grid);
            Subscribe(grid);
            Changed?.Invoke();
            return grid;
        }

        /// <summary>Add a stack to a named grid. Returns the leftover that did not fit.</summary>
        public int AddItem(string gridName, ItemStack stack)
        {
            var grid = GetOrCreateGrid(gridName, defaultMainGridSize);
            return grid.Add(stack);
        }

        /// <summary>Add a stack to the main grid. Returns the leftover that did not fit.</summary>
        public int AddItem(ItemStack stack) => MainGrid.Add(stack);

        /// <summary>Total count of an item across all grids.</summary>
        public int CountItem(ItemDefinition item)
        {
            int total = 0;
            for (int i = 0; i < grids.Count; i++)
                total += grids[i].CountItem(item);
            return total;
        }

        /// <summary>Consume an item from any grid, starting with the main grid.</summary>
        public bool Consume(ItemDefinition item, int count)
        {
            for (int i = 0; i < grids.Count; i++)
            {
                if (grids[i].Consume(item, count))
                    return true;
            }
            return false;
        }
    }
}
