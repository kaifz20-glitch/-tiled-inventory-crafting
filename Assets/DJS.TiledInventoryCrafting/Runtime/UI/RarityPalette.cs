using System;
using UnityEngine;

namespace TiledInventory
{
    /// <summary>
    /// All UI colors in one place so a game can re-skin the whole system from the
    /// inspector — no code changes (Phase 3 "visual customization").
    /// Assign an instance to <see cref="InventoryCraftingUI.Palette"/>.
    /// </summary>
    [Serializable]
    public class RarityPalette
    {
        [Header("Rarity colors")]
        public Color common = new Color(0.70f, 0.70f, 0.72f);
        public Color uncommon = new Color(0.32f, 0.86f, 0.42f);
        public Color rare = new Color(0.30f, 0.63f, 1.00f);
        public Color epic = new Color(0.78f, 0.44f, 1.00f);
        public Color legendary = new Color(1.00f, 0.79f, 0.22f);

        [Header("Surfaces")]
        public Color panelBackground = new Color(0.075f, 0.085f, 0.11f, 0.96f);
        public Color panelHeader = new Color(0.12f, 0.14f, 0.18f, 1f);
        public Color panelHeaderTop = new Color(0.17f, 0.20f, 0.27f, 1f);
        public Color panelHeaderBottom = new Color(0.11f, 0.13f, 0.18f, 1f);
        public Color slotBackground = new Color(0.105f, 0.115f, 0.15f, 0.98f);
        public Color slotBackgroundSpecial = new Color(0.09f, 0.15f, 0.23f, 0.98f);
        public Color slotEmpty = new Color(0.145f, 0.155f, 0.20f, 0.95f);
        public Color lockedOverlay = new Color(0f, 0f, 0f, 0.55f);
        public Color rowBackground = new Color(0.13f, 0.145f, 0.19f, 0.97f);
        public Color rowBackgroundLocked = new Color(0.085f, 0.095f, 0.12f, 0.97f);
        public Color buttonNormal = new Color(0.25f, 0.28f, 0.35f, 1f);
        public Color buttonHighlight = new Color(0.32f, 0.36f, 0.45f, 1f);
        public Color buttonPressed = new Color(0.17f, 0.19f, 0.24f, 1f);
        public Color buttonDisabled = new Color(0.20f, 0.21f, 0.25f, 0.6f);
        public Color textPrimary = new Color(0.93f, 0.94f, 0.96f, 1f);
        public Color textSecondary = new Color(0.68f, 0.71f, 0.76f, 1f);
        public Color textDisabled = new Color(0.48f, 0.50f, 0.55f, 1f);
        public Color success = new Color(0.35f, 0.85f, 0.45f, 1f);
        public Color warning = new Color(1f, 0.72f, 0.30f, 1f);
        public Color danger = new Color(1f, 0.35f, 0.35f, 1f);

        [Header("Accent")]
        [Tooltip("Used for header accents, highlights and emphasis.")]
        public Color accent = new Color(1f, 0.72f, 0.25f, 1f);

        public Color Get(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return common;
                case ItemRarity.Uncommon: return uncommon;
                case ItemRarity.Rare: return rare;
                case ItemRarity.Epic: return epic;
                case ItemRarity.Legendary: return legendary;
                default: return Color.white;
            }
        }
    }
}
