using System;
using UnityEngine;

namespace TiledInventory
{
    /// <summary>
    /// Typed equipment layer over the "Equipment" grid. Pins each equipment slot to one
    /// body part (via <see cref="SlotRestriction.equipmentSlotType"/>), exposes
    /// Equip/Unequip with automatic swap-into-inventory, and tracks total equipped stats.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InventorySystem))]
    public class EquipmentSystem : MonoBehaviour
    {
        private static readonly EquipmentSlotType[] SlotOrder =
        {
            EquipmentSlotType.Head,
            EquipmentSlotType.Chest,
            EquipmentSlotType.Legs,
            EquipmentSlotType.Weapon,
            EquipmentSlotType.Accessory
        };

        private InventorySystem inventory;
        private InventoryGrid grid;
        private bool initialized;

        /// <summary>Raised whenever an item is equipped or unequipped (arg = slot index, -1 = structural).</summary>
        public event Action<int> EquipmentChanged;

        /// <summary>The equipment grid. Initialized lazily so the component works even
        /// when created at runtime or accessed before Awake (e.g. edit-mode tooling).</summary>
        public InventoryGrid Grid
        {
            get
            {
                EnsureInitialized();
                return grid;
            }
        }

        /// <summary>Slot index of the equipment grid holding the given body part, or -1.</summary>
        public int GetSlotIndex(EquipmentSlotType type)
        {
            for (int i = 0; i < SlotOrder.Length; i++)
                if (SlotOrder[i] == type) return i;
            return -1;
        }

        /// <summary>Body part occupying a slot index, or None.</summary>
        public EquipmentSlotType GetSlotType(int index) =>
            index >= 0 && index < SlotOrder.Length ? SlotOrder[index] : EquipmentSlotType.None;

        /// <summary>What is equipped in a body part (null if empty).</summary>
        public ItemDefinition GetEquipped(EquipmentSlotType type)
        {
            int i = GetSlotIndex(type);
            if (i < 0) return null;
            var slot = Grid.GetSlot(i);
            return slot != null && !slot.IsEmpty ? slot.stack.item : null;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (inventory == null) return;
            grid = inventory.EquipmentGrid;
            SetupRestrictions();
            grid.Changed += () => EquipmentChanged?.Invoke(-1);
            grid.SlotChanged += i => EquipmentChanged?.Invoke(i);
        }

        /// <summary>Pin each equipment slot to exactly one body part.</summary>
        private void SetupRestrictions()
        {
            for (int i = 0; i < SlotOrder.Length; i++)
            {
                var slot = Grid.GetSlot(i);
                if (slot == null) continue;
                slot.restriction.locked = false;
                slot.restriction.equipmentSlotType = SlotOrder[i];
                slot.restriction.allowedCategories.Clear();
                slot.restriction.allowedItems.Clear();
            }
        }

        /// <summary>True when the item fits the body part of slot <paramref name="slotIndex"/>.</summary>
        public bool IsAllowed(ItemDefinition item, int slotIndex)
        {
            if (item == null || !item.CanEquip) return false;
            return GetSlotType(slotIndex) == item.EquippableSlot;
        }

        /// <summary>Equip an item from the main grid. Returns false if it cannot be equipped
        /// (wrong slot type, item not equippable, or item not found in inventory).
        /// Existing gear in that slot is swapped back into the inventory.</summary>
        public bool Equip(ItemDefinition item)
        {
            if (item == null || !item.CanEquip) return false;
            int slotIndex = GetSlotIndex(item.EquippableSlot);
            if (slotIndex < 0) return false;

            var target = Grid.GetSlot(slotIndex);
            if (target == null) return false;

            // find the item in the main bag
            var main = inventory.MainGrid;
            int sourceIndex = -1;
            for (int i = 0; i < main.Count; i++)
            {
                var s = main.GetSlot(i);
                if (s != null && !s.IsEmpty && s.stack.item == item) { sourceIndex = i; break; }
            }
            if (sourceIndex < 0) return false;

            if (!target.IsEmpty)
            {
                // swap: put current gear back into the bag first
                int leftover = main.Add(target.stack);
                if (leftover > 0) return false; // bag is full — refuse rather than lose gear
            }

            int removed = main.Remove(sourceIndex, 1);
            if (removed != 1) return false;

            target.stack = new ItemStack(item, 1);
            Grid.EmitChanged();
            return true;
        }

        /// <summary>Unequip a body part back into the main grid. False if the bag is full.</summary>
        public bool Unequip(EquipmentSlotType type)
        {
            int slotIndex = GetSlotIndex(type);
            var slot = Grid.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty) return false;
            int leftover = inventory.MainGrid.Add(slot.stack);
            if (leftover > 0) return false;
            slot.stack = ItemStack.Empty;
            Grid.EmitChanged();
            return true;
        }

        /// <summary>Total of a stat across everything currently equipped.</summary>
        public int GetTotalStat(StatType type)
        {
            int total = 0;
            for (int i = 0; i < Grid.Count; i++)
            {
                var slot = Grid.GetSlot(i);
                if (slot != null && !slot.IsEmpty)
                    total += slot.stack.item.GetStat(type);
            }
            return total;
        }
    }
}
