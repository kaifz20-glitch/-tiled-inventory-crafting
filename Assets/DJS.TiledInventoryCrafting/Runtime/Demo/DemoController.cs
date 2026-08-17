using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TiledInventory
{
    /// <summary>
    /// Drives the RPG demo scenario (collect materials → craft sword): a HUD with
    /// "Gather" buttons, gold/XP/level readout, save/load/reset, a trade toggle, and
    /// feedback (audio + particles + achievement toasts). All demo-only — the product
    /// systems never reference it.
    /// </summary>
    [DisallowMultipleComponent]
    public class DemoController : MonoBehaviour
    {
        [Header("Systems (auto-resolved when empty)")]
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private CraftingSystem crafting;
        [SerializeField] private EquipmentSystem equipment;
        [SerializeField] private PlayerProfile profile;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private AchievementTracker achievements;
        [SerializeField] private InventoryCraftingUI ui;
        [SerializeField] private AudioFeedback audioFeedback;

        [Header("Demo items")]
        [SerializeField] private ItemDefinition wood;
        [SerializeField] private ItemDefinition ironOre;
        [SerializeField] private ItemDefinition leather;

        private Text hudText;
        private Text toastText;
        private float toastUntil;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (crafting == null) crafting = GetComponent<CraftingSystem>();
            if (equipment == null) equipment = GetComponent<EquipmentSystem>();
            if (profile == null) profile = GetComponent<PlayerProfile>();
            if (saveManager == null) saveManager = GetComponent<SaveManager>();
            if (achievements == null) achievements = GetComponent<AchievementTracker>();
            if (ui == null) ui = GetComponent<InventoryCraftingUI>();
            if (audioFeedback == null) audioFeedback = GetComponent<AudioFeedback>();
        }

        private void Start()
        {
            PopulateShowcase();
            BuildHud();
            HookEvents();
            RefreshHud();
        }

        /// <summary>
        /// Fill the bag with starter materials and pre-equip starter gear so the demo
        /// screen is presentable immediately (great for store-page screenshots). Only
        /// runs while the inventory is empty, so saved games are never clobbered.
        /// </summary>
        public void PopulateShowcase()
        {
            if (inventory == null) return;
            var grid = inventory.MainGrid;
            for (int i = 0; i < grid.Count; i++)
            {
                if (!grid.GetSlot(i).IsEmpty) return;
            }

            ItemDefinition Find(string name)
            {
                foreach (var it in Registry.AllItems)
                    if (it != null && it.name == name) return it;
                return null;
            }

            inventory.AddItem(new ItemStack(Find("Wood"), 12));
            inventory.AddItem(new ItemStack(Find("IronOre"), 9));
            inventory.AddItem(new ItemStack(Find("Iron"), 6));
            inventory.AddItem(new ItemStack(Find("Leather"), 4));
            inventory.AddItem(new ItemStack(Find("GoldCoin"), 30));
            inventory.AddItem(new ItemStack(Find("Potion"), 2));

            TryEquip(Find("Sword"));
            TryEquip(Find("Helmet"));
            TryEquip(Find("Chestplate"));
        }

        private void TryEquip(ItemDefinition item)
        {
            if (item == null || equipment == null || inventory == null) return;
            inventory.AddItem(new ItemStack(item, 1));
            equipment.Equip(item);
        }

        private void HookEvents()
        {
            if (profile != null) profile.Changed += RefreshHud;
            if (crafting != null)
            {
                crafting.JobCompleted += job => OnCraftComplete(job);
                crafting.JobFailed += _ => audioFeedback?.PlayFail();
            }
            if (equipment != null) equipment.EquipmentChanged += _ => audioFeedback?.PlayEquip();
            if (achievements != null) achievements.Unlocked += ShowAchievementToast;
        }

        private void OnCraftComplete(CraftJob job)
        {
            audioFeedback?.PlayCraftComplete();
            if (Camera.main != null)
            {
                var cam = Camera.main.transform;
                ParticleBurst.Play(cam.position + cam.forward * 4f + Vector3.up * 1.5f,
                    job != null && job.recipe != null && job.recipe.Outputs.Count > 0 && job.recipe.Outputs[0].item != null
                        ? RarityColors.Get(job.recipe.Outputs[0].item.Rarity)
                        : Color.yellow);
            }
        }

        // ------------------------------------------------------------------ HUD

        private void BuildHud()
        {
            if (ui == null || ui.Canvas == null) return;
            var root = (RectTransform)ui.Canvas.transform;

            var hudPanel = UIFactory.CreateRect(root, "DemoHud", new Vector2(0f, 0.955f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var bg = hudPanel.gameObject.AddComponent<Image>();
            bg.sprite = UIFactory.GetRoundedGradientSprite(ui.Palette.panelHeaderTop, ui.Palette.panelHeaderBottom, 12f, false, true);
            bg.type = Image.Type.Sliced;

            var accentLine = UIFactory.CreateRect(hudPanel, "Accent", new Vector2(0f, 0f), new Vector2(1f, 0.08f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            var accentImg = accentLine.gameObject.AddComponent<Image>();
            accentImg.sprite = UIFactory.GetRoundedFillSprite(2f);
            accentImg.type = Image.Type.Sliced;
            accentImg.color = new Color(ui.Palette.accent.r, ui.Palette.accent.g, ui.Palette.accent.b, 0.85f);

            var title = UIFactory.CreateText(hudPanel, "Title", "Tiled Inventory — Demo", 18, ui.Palette.accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0.01f, 0f);
            title.rectTransform.anchorMax = new Vector2(0.15f, 1f);

            hudText = UIFactory.CreateText(hudPanel, "Stats", "", 19, Color.white, TextAnchor.MiddleLeft);
            hudText.rectTransform.anchorMin = new Vector2(0.16f, 0f);
            hudText.rectTransform.anchorMax = new Vector2(0.36f, 1f);

            var buttons = new (string label, System.Action action)[]
            {
                ("Gather Wood +5", () => AddMaterial(wood, 5)),
                ("Gather Iron Ore +3", () => AddMaterial(ironOre, 3)),
                ("Gather Leather +2", () => AddMaterial(leather, 2)),
                ("+50 Gold", () => profile?.AddGold(50)),
                ("+XP", () => profile?.AddXp(60)),
                ("Save", () => saveManager?.Save()),
                ("Load", () => saveManager?.Load()),
                ("Reset", ResetDemo),
                ("Trade", () => ui?.ToggleTradePanel())
            };

            var row = UIFactory.CreateRect(hudPanel, "Buttons", new Vector2(0.37f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            foreach (var (label, action) in buttons)
            {
                var btnRect = UIFactory.CreateRect(row, label, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                UIFactory.SetHeight(btnRect, 26f);
                var btn = UIFactory.CreateButton(btnRect, "Btn", label, () => action(), ui.Palette, 16);
                var le = btnRect.gameObject.GetComponent<LayoutElement>();
                le.preferredWidth = 132f;
                le.minWidth = 132f;
            }

            // achievement toast
            toastText = UIFactory.CreateText(root, "AchievementToast", "", 24, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold);
            toastText.rectTransform.anchorMin = new Vector2(0.25f, 0.82f);
            toastText.rectTransform.anchorMax = new Vector2(0.75f, 0.9f);
            toastText.rectTransform.offsetMin = Vector2.zero;
            toastText.rectTransform.offsetMax = Vector2.zero;
            toastText.gameObject.SetActive(false);
        }

        private void AddMaterial(ItemDefinition item, int count)
        {
            if (item == null || inventory == null) return;
            int leftover = inventory.AddItem(new ItemStack(item, count));
            if (leftover > 0) ShowToast("Inventory full!", new Color(1f, 0.4f, 0.4f));
        }

        private void ResetDemo()
        {
            if (inventory != null) inventory.MainGrid.Clear();
            if (crafting != null) crafting.CancelAll();
            if (profile != null) profile.SetAll(1, 0, 0, 1);
            ShowToast("Demo reset", Color.white);
        }

        private void RefreshHud()
        {
            if (hudText == null) return;
            hudText.text = profile == null
                ? "Profile missing"
                : $"Level {profile.Level}  ·  XP {profile.Xp}  ·  Gold {profile.Gold}  ·  Crafting Skill {profile.CraftingSkill}";
        }

        private void ShowAchievementToast(AchievementDefinition achievement)
        {
            if (achievement == null) return;
            ShowToast($"Achievement unlocked: {achievement.Title}", new Color(1f, 0.85f, 0.3f));
            audioFeedback?.PlayCraftComplete();
        }

        private void ShowToast(string message, Color color)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastText.color = color;
            toastText.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + 3f;
        }

        private void Update()
        {
            if (toastText != null && toastText.gameObject.activeSelf && Time.unscaledTime >= toastUntil)
                toastText.gameObject.SetActive(false);
        }
    }
}
