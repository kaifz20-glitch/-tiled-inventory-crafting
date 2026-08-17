using System;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>Simple numeric stats an item can carry (damage, armor, ...).
    /// Extend the enum to add game-specific stats.</summary>
    public enum StatType
    {
        Damage,
        Armor,
        Durability,
        Speed,
        Luck
    }

    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public int value;
    }
}
