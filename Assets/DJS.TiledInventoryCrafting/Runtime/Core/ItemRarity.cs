using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>Rarity tiers. Rarity drives color-coding in every UI panel.</summary>
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>Central color lookup for rarity. Colors are tintable per-game; see
    /// <see cref="RarityPalette"/> in the UI namespace for customization.</summary>
    public static class RarityColors
    {
        public static readonly Color Common = new Color(0.70f, 0.70f, 0.72f);
        public static readonly Color Uncommon = new Color(0.32f, 0.86f, 0.42f);
        public static readonly Color Rare = new Color(0.30f, 0.63f, 1.00f);
        public static readonly Color Epic = new Color(0.78f, 0.44f, 1.00f);
        public static readonly Color Legendary = new Color(1.00f, 0.79f, 0.22f);

        public static Color Get(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Common;
                case ItemRarity.Uncommon: return Uncommon;
                case ItemRarity.Rare: return Rare;
                case ItemRarity.Epic: return Epic;
                case ItemRarity.Legendary: return Legendary;
                default: return Color.white;
            }
        }
    }
}
