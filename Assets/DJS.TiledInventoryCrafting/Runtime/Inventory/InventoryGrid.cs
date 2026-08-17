using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiledInventory
{
    /// <summary>Per-slot placement rules. Empty lists mean "no restriction".
    /// A locked slot rejects all placement and is drawn dimmed in the UI.</summary>
    [Serializable]
    public class SlotRestriction
    {
        public bool locked;
        /// <summary>If not None, only items for this body part may enter the slot
        /// (used by the equipment grid).</summary>
        public EquipmentSlotType equipmentSlotType = EquipmentSlotType.None;
        public List<ItemCategory> allowedCategories = new List<ItemCategory>();
        public List<ItemDefinition> allowedItems = new List<ItemDefinition>();

        public bool Allows(ItemDefinition item)
        {
            if (item == null) return false;
            if (locked) return false;
            if (equipmentSlotType != EquipmentSlotType.None && item.EquippableSlot != equipmentSlotType)
                return false;
            if (allowedItems.Count == 0 && allowedCategories.Count == 0) return true;
            for (int i = 0; i < allowedItems.Count; i++)
                if (allowedItems[i] == item) return true;
            for (int i = 0; i < allowedCategories.Count; i++)
                if (allowedCategories[i] == item.Category) return true;
            return false;
        }
    }

    /// <summary>One cell of a grid. Holds the stack plus its placement rules.</summary>
    [Serializable]
    public class InventorySlot
    {
        public ItemStack stack = ItemStack.Empty;
        public SlotRestriction restriction = new SlotRestriction();

        public bool IsEmpty => stack.IsEmpty;
    }

    /// <summary>
    /// A tiled inventory: a W×H set of slots, each with optional restrictions.
    /// Pure data + rules — no MonoBehaviour, no rendering. The UI layer
    /// (<see cref="InventoryGridView"/>) observes it; the save layer serializes it.
    ///
    /// Thread-safe? No. Everything runs on the main thread, same as Unity.
    /// </summary>
    [Serializable]
    public class InventoryGrid
    {
        [SerializeField] private string gridName = "Inventory";
        [SerializeField] private Vector2Int size = new Vector2Int(5, 5);
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        /// <summary>Raised when a single slot's contents change. Argument is the slot index.</summary>
        public event Action<int> SlotChanged;
        /// <summary>Raised for any structural change (resize, clear, bulk load).</summary>
        public event Action Changed;

        public string GridName { get => gridName; set => gridName = value; }
        public Vector2Int Size => size;
        public int Count => size.x * size.y;

        public InventoryGrid() { EnsureCapacity(); }

        public InventoryGrid(string name, Vector2Int size)
        {
            gridName = name;
            this.size = size;
            EnsureCapacity();
        }

        // ------------------------------------------------------------------ setup

        private void EnsureCapacity()
        {
            int target = Mathf.Max(0, size.x * size.y);
            while (slots.Count < target) slots.Add(new InventorySlot());
            if (slots.Count > target) slots.RemoveRange(target, slots.Count - target);
        }

        /// <summary>Change the grid dimensions. Existing contents are preserved where possible;
        /// slots outside the new bounds are dropped.</summary>
        public void Resize(Vector2Int newSize)
        {
            if (newSize.x < 1) newSize.x = 1;
            if (newSize.y < 1) newSize.y = 1;
            if (newSize == size) return;
            size = newSize;
            EnsureCapacity();
            Changed?.Invoke();
        }

        /// <summary>True when the index refers to an existing slot.</summary>
        public bool IsValidIndex(int index) => index >= 0 && index < slots.Count;

        public InventorySlot GetSlot(int index)
        {
            return IsValidIndex(index) ? slots[index] : null;
        }

        /// <summary>Convert a grid position to a slot index.</summary>
        public int IndexOf(Vector2Int position) => position.y * size.x + position.x;

        /// <summary>Convert a slot index to a grid position.</summary>
        public Vector2Int PositionOf(int index) => new Vector2Int(index % size.x, index / size.x);

        // ------------------------------------------------------------------ queries

        public bool IsEmpty(int index)
        {
            var s = GetSlot(index);
            return s == null || s.IsEmpty;
        }

        /// <summary>Would placing <paramref name="count"/> of <paramref name="item"/>
        /// into slot <paramref name="index"/> be legal? Checks bounds, restrictions,
        /// stack type and max stack. Ignores how much room is left elsewhere.</summary>
        public bool CanPlace(int index, ItemDefinition item, int count)
        {
            if (item == null || count <= 0 || !IsValidIndex(index)) return false;
            var slot = slots[index];
            if (!slot.restriction.Allows(item)) return false;
            if (slot.IsEmpty) return true;
            return slot.stack.item == item && slot.stack.count + count <= item.MaxStack;
        }

        /// <summary>Does at least one legal, non-locked slot exist for this stack?</summary>
        public bool CanPlaceAnywhere(ItemDefinition item, int count)
        {
            if (item == null || count <= 0) return false;
            for (int i = 0; i < slots.Count; i++)
                if (CanPlace(i, item, count)) return true;
            return false;
        }

        /// <summary>Total count of <paramref name="item"/> across all slots.</summary>
        public int CountItem(ItemDefinition item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (!s.IsEmpty && s.stack.item == item) total += s.stack.count;
            }
            return total;
        }

        /// <summary>Does the grid hold at least <paramref name="count"/> of <paramref name="item"/>?</summary>
        public bool Contains(ItemDefinition item, int count) => CountItem(item) >= count;

        // ------------------------------------------------------------------ mutations

        /// <summary>
        /// Add a stack to the grid: first merges into existing stacks of the same item,
        /// then fills empty slots. Returns the amount that could NOT be placed
        /// (0 when everything fit).
        /// </summary>
        public int Add(ItemStack stack)
        {
            if (stack.IsEmpty) return 0;
            int remaining = stack.count;

            // 1) merge into existing non-full stacks
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var s = slots[i];
                if (s.IsEmpty || s.stack.item != stack.item) continue;
                int room = stack.item.MaxStack - s.stack.count;
                if (room <= 0) continue;
                int take = Mathf.Min(room, remaining);
                s.stack.count += take;
                remaining -= take;
                NotifySlot(i);
            }

            // 2) fill empty slots
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var s = slots[i];
                if (!s.IsEmpty || !s.restriction.Allows(stack.item)) continue;
                int take = Mathf.Min(stack.item.MaxStack, remaining);
                s.stack = new ItemStack(stack.item, take);
                remaining -= take;
                NotifySlot(i);
            }

            if (remaining != stack.count) Changed?.Invoke();
            return remaining;
        }

        /// <summary>Remove up to <paramref name="count"/> items from one slot. Returns amount removed.</summary>
        public int Remove(int index, int count)
        {
            var s = GetSlot(index);
            if (s == null || s.IsEmpty || count <= 0) return 0;
            int removed = Mathf.Min(s.stack.count, count);
            s.stack.count -= removed;
            if (s.stack.count <= 0) s.stack = ItemStack.Empty;
            NotifySlot(index);
            Changed?.Invoke();
            return removed;
        }

        /// <summary>Consume <paramref name="count"/> of <paramref name="item"/> from across the grid
        /// (top-left first). Returns true when fully satisfied.</summary>
        public bool Consume(ItemDefinition item, int count)
        {
            if (item == null || count <= 0) return true;
            int remaining = count;
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var s = slots[i];
                if (s.IsEmpty || s.stack.item != item) continue;
                int take = Mathf.Min(s.stack.count, remaining);
                s.stack.count -= take;
                remaining -= take;
                if (s.stack.count <= 0) s.stack = ItemStack.Empty;
                NotifySlot(i);
            }
            if (remaining != count) Changed?.Invoke();
            return remaining == 0;
        }

        /// <summary>
        /// Move <paramref name="count"/> items from slot <paramref name="from"/> to slot <paramref name="to"/>.
        /// Handles stacking (merges up to max stack) and swapping (if the target holds a
        /// different item and there is no merge room). Enforces restrictions on the target
        /// slot. Returns true when the move happened.
        /// </summary>
        public bool Move(int from, int to, int count)
        {
            if (from == to || !IsValidIndex(from) || !IsValidIndex(to) || count <= 0) return false;
            var src = slots[from];
            var dst = slots[to];
            if (src.IsEmpty) return false;
            if (!dst.restriction.Allows(src.stack.item)) return false;

            if (dst.IsEmpty)
            {
                if (dst.restriction.locked) return false;
                int take = Mathf.Min(src.stack.count, count);
                dst.stack = new ItemStack(src.stack.item, take);
                src.stack.count -= take;
                if (src.stack.count <= 0) src.stack = ItemStack.Empty;
                NotifySlot(from);
                NotifySlot(to);
                Changed?.Invoke();
                return true;
            }

            // merge
            if (dst.stack.item == src.stack.item)
            {
                int room = dst.stack.item.MaxStack - dst.stack.count;
                if (room <= 0) return false;
                int take = Mathf.Min(room, Mathf.Min(src.stack.count, count));
                dst.stack.count += take;
                src.stack.count -= take;
                if (src.stack.count <= 0) src.stack = ItemStack.Empty;
                NotifySlot(from);
                NotifySlot(to);
                Changed?.Invoke();
                return true;
            }

            // swap (only when moving the full stack — partial swaps are confusing)
            if (count >= src.stack.count && src.restriction.Allows(dst.stack.item))
            {
                var tmp = src.stack;
                src.stack = dst.stack;
                dst.stack = tmp;
                NotifySlot(from);
                NotifySlot(to);
                Changed?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>Empty every slot. Restrictions are kept.</summary>
        public void Clear()
        {
            for (int i = 0; i < slots.Count; i++)
                slots[i].stack = ItemStack.Empty;
            Changed?.Invoke();
        }

        /// <summary>Raise <see cref="Changed"/> from outside the grid (equipment swaps,
        /// save restore, network sync). C# events cannot be invoked externally, so the
        /// grid exposes this one explicit notification point.</summary>
        public void EmitChanged() => Changed?.Invoke();

        // ------------------------------------------------------------------ internal

        private void NotifySlot(int index) => SlotChanged?.Invoke(index);
    }
}
