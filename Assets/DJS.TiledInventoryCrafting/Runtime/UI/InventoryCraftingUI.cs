using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TiledInventory
{
    /// <summary>
    /// Root of the whole UI. Given the game systems, it builds the complete screen from
    /// code — no prefabs required — and wires every panel together:
    ///
    ///   Equipment (left) · Inventory with toolbar (center) · Crafting (right) · Trade (bottom)
    ///
    /// Attach to the same GameObject as the systems (or wire references in the inspector).
    /// The palette is fully tweakable in the inspector (Phase 3 visual customization).
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryCraftingUI : MonoBehaviour
    {
        [Header("Systems (auto-resolved from this GameObject when empty)")]
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private EquipmentSystem equipment;
        [SerializeField] private CraftingSystem crafting;
        [SerializeField] private PlayerProfile profile;
        [SerializeField] private TradeSystem trading;

        [Header("Content")]
        [Tooltip("Recipes shown in the crafting panel. Leave empty to auto-discover from the Registry.")]
        [SerializeField] private List<RecipeDefinition> recipes = new List<RecipeDefinition>();
        [Tooltip("Items available for trade offers. Leave empty to auto-discover.")]
        [SerializeField] private List<ItemDefinition> tradeItems = new List<ItemDefinition>();

        [Header("Look")]
        [SerializeField] private RarityPalette palette = new RarityPalette();
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private int canvasSortingOrder = 10;

        public RarityPalette Palette => palette;
        public Canvas Canvas { get; private set; }
        public Tooltip Tooltip { get; private set; }
        public DragDropService Drag { get; private set; }
        public InventoryGridView InventoryView { get; private set; }
        public EquipmentPanelUI EquipmentView { get; private set; }
        public CraftingPanelUI CraftingView { get; private set; }
        public TradePanelUI TradeView { get; private set; }

        private RectTransform tradePanel;
        private bool tradeOpen;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (equipment == null) equipment = GetComponent<EquipmentSystem>();
            if (crafting == null) crafting = GetComponent<CraftingSystem>();
            if (profile == null) profile = GetComponent<PlayerProfile>();
            if (trading == null) trading = GetComponent<TradeSystem>();

            if (recipes.Count == 0)
            {
                recipes.Clear();
                foreach (var recipe in Registry.AllRecipes) recipes.Add(recipe);
            }
            if (tradeItems.Count == 0)
            {
                tradeItems.Clear();
                foreach (var item in Registry.AllItems) tradeItems.Add(item);
            }

            if (buildOnAwake) BuildUI();
        }

        // ------------------------------------------------------------------ build

        public void BuildUI()
        {
            Canvas = UIFactory.CreateCanvas("InventoryCraftingCanvas", canvasSortingOrder);
            var root = (RectTransform)Canvas.transform;

            // soft gradient backdrop so the panels pop against the game world
            var backdrop = UIFactory.CreateStretch(root, "Backdrop").gameObject.AddComponent<Image>();
            backdrop.sprite = UIFactory.GetGradientSprite(new Color(0.09f, 0.11f, 0.16f), new Color(0.045f, 0.055f, 0.08f));
            backdrop.raycastTarget = false;

            Drag = root.gameObject.AddComponent<DragDropService>();
            Tooltip = root.gameObject.AddComponent<Tooltip>();
            Tooltip.Palette = palette;

            // --- equipment (left column)
            var equipPanel = CreateSidePanel(root, "Equipment", new Vector2(144f, 60f), new Vector2(260f, 800f));
            var equipSlotColumn = UIFactory.CreateRect(equipPanel, "Slots", new Vector2(0f, 0.12f), new Vector2(1f, 0.88f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
            var equipStats = UIFactory.CreateText(equipPanel, "Stats", "", 16, palette.textSecondary, TextAnchor.MiddleCenter, FontStyle.Bold);
            equipStats.rectTransform.anchorMin = new Vector2(0f, 0.02f);
            equipStats.rectTransform.anchorMax = new Vector2(1f, 0.11f);
            equipStats.rectTransform.offsetMin = new Vector2(6f, 0f);
            equipStats.rectTransform.offsetMax = new Vector2(-6f, 0f);
            EquipmentView = equipPanel.gameObject.AddComponent<EquipmentPanelUI>();
            EquipmentView.Bind(equipment, palette, equipSlotColumn, equipStats);

            // --- inventory (center)
            var invPanel = CreateSidePanel(root, "Inventory", new Vector2(420f, 60f), new Vector2(780f, 800f));
            var toolbar = UIFactory.CreateRect(invPanel, "Toolbar", new Vector2(0f, 0.885f), new Vector2(1f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(-12f, 0f));
            var gridContainer = UIFactory.CreateRect(invPanel, "Grid", new Vector2(0f, 0.055f), new Vector2(1f, 0.885f), new Vector2(0.5f, 0.5f), new Vector2(12f, 6f), new Vector2(-12f, 0f));
            InventoryView = invPanel.gameObject.AddComponent<InventoryGridView>();
            InventoryView.Bind(inventory.MainGrid, palette, gridContainer);
            InventoryView.BuildToolbar(toolbar);

            // drop zone: drag an item here to remove it from the inventory entirely
            DropZone.Create(invPanel, palette);

            // --- crafting (right)
            var craftPanel = CreateSidePanel(root, "Crafting", new Vector2(1216f, 60f), new Vector2(560f, 800f));
            var recipeLabel = UIFactory.CreateText(craftPanel, "RecipesLabel", "Recipes", 18, palette.textSecondary, TextAnchor.MiddleLeft, FontStyle.Bold);
            recipeLabel.rectTransform.anchorMin = new Vector2(0f, 0.895f);
            recipeLabel.rectTransform.anchorMax = new Vector2(1f, 0.935f);
            recipeLabel.rectTransform.offsetMin = new Vector2(12f, 0f);
            recipeLabel.rectTransform.offsetMax = new Vector2(0f, 0f);
            var recipeScroll = UIFactory.CreateScrollView(craftPanel, "RecipeList", 6f);
            recipeScroll.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.40f);
            recipeScroll.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.895f);
            recipeScroll.GetComponent<RectTransform>().offsetMin = new Vector2(6f, 0f);
            recipeScroll.GetComponent<RectTransform>().offsetMax = new Vector2(-6f, 0f);

            var queueLabel = UIFactory.CreateText(craftPanel, "QueueLabel", "Queue", 18, palette.textSecondary, TextAnchor.MiddleLeft, FontStyle.Bold);
            queueLabel.rectTransform.anchorMin = new Vector2(0f, 0.345f);
            queueLabel.rectTransform.anchorMax = new Vector2(1f, 0.385f);
            queueLabel.rectTransform.offsetMin = new Vector2(12f, 0f);
            queueLabel.rectTransform.offsetMax = new Vector2(0f, 0f);
            var queueScroll = UIFactory.CreateScrollView(craftPanel, "QueueList", 4f);
            queueScroll.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.06f);
            queueScroll.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.345f);
            queueScroll.GetComponent<RectTransform>().offsetMin = new Vector2(6f, 0f);
            queueScroll.GetComponent<RectTransform>().offsetMax = new Vector2(-6f, 0f);

            var statusText = UIFactory.CreateText(craftPanel, "Status", "", 16, palette.textPrimary, TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(1f, 0.05f);
            statusText.rectTransform.offsetMin = new Vector2(6f, 0f);
            statusText.rectTransform.offsetMax = new Vector2(-6f, 0f);
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;

            CraftingView = craftPanel.gameObject.AddComponent<CraftingPanelUI>();
            CraftingView.Bind(crafting, inventory, profile, recipes, palette, recipeScroll.content, queueScroll.content, statusText);

            // --- trade (bottom drawer)
            tradePanel = BuildTradePanel(root);
            tradePanel.gameObject.SetActive(false);

            // --- close tooltip whenever the inventory changes (data may have moved)
            inventory.Changed += () => Tooltip?.Hide();
        }

        /// <summary>Panel with a gradient header (rounded top) over a rounded body.</summary>
        private RectTransform CreateSidePanel(RectTransform root, string title, Vector2 position, Vector2 size)
        {
            var panel = UIFactory.CreateRect(root, title + "Panel",
                new Vector2(position.x / 1920f, position.y / 1080f),
                new Vector2((position.x + size.x) / 1920f, (position.y + size.y) / 1080f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var body = UIFactory.CreateRect(panel, "Body", new Vector2(0f, 0f), new Vector2(1f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var bodyImg = body.gameObject.AddComponent<Image>();
            bodyImg.sprite = UIFactory.GetRoundedBottomSprite(10f);
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = palette.panelBackground;
            bodyImg.raycastTarget = false;

            var header = UIFactory.CreateRect(panel, "Header", new Vector2(0f, 0.94f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var headerImg = header.gameObject.AddComponent<Image>();
            headerImg.sprite = UIFactory.GetRoundedGradientSprite(palette.panelHeaderTop, palette.panelHeaderBottom, 10f, true, false);
            headerImg.type = Image.Type.Sliced;
            headerImg.raycastTarget = false;

            var underline = UIFactory.CreateRect(header, "Accent", new Vector2(0.28f, 0.07f), new Vector2(0.72f, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var underlineImg = underline.gameObject.AddComponent<Image>();
            underlineImg.sprite = UIFactory.GetRoundedFillSprite(1.5f);
            underlineImg.type = Image.Type.Sliced;
            underlineImg.color = new Color(palette.accent.r, palette.accent.g, palette.accent.b, 0.9f);
            underlineImg.raycastTarget = false;

            var headerText = UIFactory.CreateText(header, "Title", title, 22, palette.textPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            return panel;
        }

        private RectTransform BuildTradePanel(RectTransform root)
        {
            var panel = UIFactory.CreateRect(root, "TradePanel",
                new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = UIFactory.GetRoundedFillSprite(10f);
            bg.type = Image.Type.Sliced;
            bg.color = palette.panelBackground;

            var close = UIFactory.CreateButton(panel, "Close", "✕", () => ToggleTradePanel(false), palette, 20);
            close.GetComponent<RectTransform>().anchorMin = new Vector2(0.96f, 0.94f);
            close.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);

            var left = UIFactory.CreateRect(panel, "Left", new Vector2(0f, 0f), new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), new Vector2(-4f, 0f));
            var right = UIFactory.CreateRect(panel, "Right", new Vector2(0.5f, 0f), new Vector2(1f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(4f, 8f), new Vector2(-8f, 0f));

            TradeView = panel.gameObject.AddComponent<TradePanelUI>();
            TradeView.Bind(trading, inventory, palette, tradeItems, left, right);
            return panel;
        }

        // ------------------------------------------------------------------ toggle

        /// <summary>Show or hide the trade drawer.</summary>
        public void ToggleTradePanel(bool? open = null)
        {
            tradeOpen = open ?? !tradeOpen;
            if (tradePanel != null) tradePanel.gameObject.SetActive(tradeOpen);
        }
    }
}
