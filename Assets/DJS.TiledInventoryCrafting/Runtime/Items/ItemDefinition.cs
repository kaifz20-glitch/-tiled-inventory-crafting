using System.Collections.Generic;
using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// The single source of truth for what an item IS: name, icon, rarity, category,
    /// stacking behaviour, equipment slot and stats. Instances of an item are never
    /// duplicated — inventories only hold references plus a count.
    ///
    /// Create new items from the menu: <c>Assets &gt; Create &gt; Tiled Inventory &gt; Item Definition</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiled Inventory/Item Definition", fileName = "Item", order = 0)]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "New Item";
        [TextArea(2, 5)]
        [SerializeField] private string description = "";

        [Header("Visual")]
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;

        [Header("Behaviour")]
        [SerializeField] private ItemCategory category = ItemCategory.Material;
        [SerializeField] private int maxStack = 99;
        [SerializeField] private EquipmentSlotType equippableSlot = EquipmentSlotType.None;
        [SerializeField] private List<StatModifier> stats = new List<StatModifier>();
        [SerializeField] private int sellValue = 1;

        /// <summary>Stable unique id. Generated automatically on creation; referenced by save files.</summary>
        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemRarity Rarity => rarity;
        public ItemCategory Category => category;
        public int MaxStack => maxStack;
        public EquipmentSlotType EquippableSlot => equippableSlot;
        public bool CanEquip => equippableSlot != EquipmentSlotType.None;
        public IReadOnlyList<StatModifier> Stats => stats;
        public int SellValue => sellValue;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString("N").Substring(0, 12);
            if (string.IsNullOrEmpty(displayName))
                displayName = name;
            if (maxStack < 1)
                maxStack = 1;
        }

        private void OnEnable() => Registry.RegisterItem(this);
        private void OnDisable() => Registry.UnregisterItem(this);

        /// <summary>Sum of a stat across all modifiers (e.g. total damage of a weapon).</summary>
        public int GetStat(StatType type)
        {
            int total = 0;
            for (int i = 0; i < stats.Count; i++)
                if (stats[i].stat == type)
                    total += stats[i].value;
            return total;
        }
    }
}
