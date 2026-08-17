using System;
using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>A stack of an item. Used everywhere an item quantity travels:
    /// inventory slots, recipe inputs/outputs, trades, save data.</summary>
    [Serializable]
    public struct ItemStack
    {
        public ItemDefinition item;
        public int count;

        public ItemStack(ItemDefinition item, int count = 1)
        {
            this.item = item;
            this.count = count;
        }

        public bool IsEmpty => item == null || count <= 0;

        public static ItemStack Empty => new ItemStack(null, 0);

        public override string ToString() => item == null ? "Empty" : $"{item.DisplayName} x{count}";
    }
}
