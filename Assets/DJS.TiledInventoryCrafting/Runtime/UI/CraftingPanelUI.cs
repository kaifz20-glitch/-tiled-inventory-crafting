using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// The crafting UI: a scrollable list of recipes (locked ones grayed out with the
    /// reason), a craft button per recipe, and a live queue with progress bars.
    /// Dropping an item onto a recipe row ("drag to craft") queues it when possible.
    /// </summary>
    public class CraftingPanelUI : MonoBehaviour
    {
        private CraftingSystem crafting;
        private InventorySystem inventory;
        private PlayerProfile profile;
        private List<RecipeDefinition> recipes = new List<RecipeDefinition>();
        private RarityPalette palette = new RarityPalette();

        private RectTransform recipeListRoot;
        private RectTransform queueRoot;
        private Text statusText;

        private readonly List<RecipeRow> rows = new List<RecipeRow>();
        private readonly List<QueueRow> queueRows = new List<QueueRow>();
        private RecipeDefinition highlightedRecipe;

        public CraftingSystem Crafting => crafting;
        public event System.Action<RecipeDefinition> CraftRequested;

        public void Bind(CraftingSystem crafting, InventorySystem inventory, PlayerProfile profile,
            List<RecipeDefinition> recipes, RarityPalette palette,
            RectTransform recipeListRoot, RectTransform queueRoot, Text statusText)
        {
            this.crafting = crafting;
            this.inventory = inventory;
            this.profile = profile;
            this.recipes = recipes ?? new List<RecipeDefinition>();
            if (palette != null) this.palette = palette;
            this.recipeListRoot = recipeListRoot;
            this.queueRoot = queueRoot;
            this.statusText = statusText;

            crafting.QueueChanged += RefreshQueue;
            crafting.JobCompleted += _ => SetStatus("Craft complete!", palette.success);
            crafting.JobFailed += _ => SetStatus("Craft failed...", palette.danger);
            if (inventory != null) inventory.Changed += RefreshAllRows;
            if (profile != null) profile.Changed += RefreshAllRows;

            RebuildRecipes();
            RefreshQueue();
        }

        private void OnDestroy()
        {
            if (crafting != null) crafting.QueueChanged -= RefreshQueue;
            if (inventory != null) inventory.Changed -= RefreshAllRows;
            if (profile != null) profile.Changed -= RefreshAllRows;
        }

        // ------------------------------------------------------------------ recipe list

        public void RebuildRecipes()
        {
            foreach (var row in rows) row.Destroy();
            rows.Clear();
            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;
                var row = new RecipeRow();
                row.Build(recipeListRoot, recipe, palette, OnCraftClicked, this);
                rows.Add(row);
            }
            RefreshAllRows();
        }

        private void OnCraftClicked(RecipeDefinition recipe)
        {
            CraftRequested?.Invoke(recipe);
            if (crafting == null) return;
            var rejection = crafting.TryQueue(recipe);
            if (rejection == CraftRejection.None)
                SetStatus($"Queued: {recipe.DisplayName}", palette.textPrimary);
            else
                SetStatus($"Can't craft: {Describe(rejection)}", palette.danger);
        }

        /// <summary>Called when an item is dropped onto a recipe row (drag to craft).</summary>
        public void HandleDropOnRecipe(RecipeDefinition recipe)
        {
            highlightedRecipe = recipe;
            RefreshAllRows();
            if (crafting != null)
            {
                var rejection = crafting.TryQueue(recipe);
                if (rejection == CraftRejection.None)
                    SetStatus($"Queued: {recipe.DisplayName}", palette.success);
                else
                    SetStatus($"Can't craft: {Describe(rejection)}", palette.danger);
            }
        }

        private static string Describe(CraftRejection rejection)
        {
            switch (rejection)
            {
                case CraftRejection.MissingMaterials: return "missing materials";
                case CraftRejection.LevelTooLow: return "level too low";
                case CraftRejection.NotEnoughGold: return "not enough gold";
                case CraftRejection.NotEnoughXp: return "not enough XP";
                case CraftRejection.OnCooldown: return "on cooldown";
                default: return "unknown";
            }
        }

        private void RefreshAllRows()
        {
            foreach (var row in rows) row.RefreshState();
        }

        // ------------------------------------------------------------------ queue

        public void RefreshQueue()
        {
            foreach (var qr in queueRows) qr.Destroy();
            queueRows.Clear();
            if (crafting == null) return;

            for (int i = 0; i < crafting.Queue.Count; i++)
            {
                var job = crafting.Queue[i];
                if (job == null || job.recipe == null) continue;
                var qr = new QueueRow();
                qr.Build(queueRoot, job, i, palette, crafting);
                queueRows.Add(qr);
            }
        }

        private void Update()
        {
            // live progress bars for the active job
            if (queueRows.Count > 0)
                foreach (var qr in queueRows) qr.RefreshProgress();
        }

        private void SetStatus(string text, Color color)
        {
            if (statusText != null)
            {
                statusText.text = text;
                statusText.color = color;
            }
        }

        // ================================================================== recipe row

        private class RecipeRow
        {
            private RectTransform root;
            private RecipeDefinition recipe;
            private CraftingPanelUI panel;
            private RarityPalette palette;
            private Image bg;
            private Image accent;
            private Text nameText;
            private Text summaryText;
            private Text reasonText;
            private Button craftButton;
            private Image lockOverlay;

            public void Build(RectTransform parent, RecipeDefinition recipe, RarityPalette palette,
                System.Action<RecipeDefinition> onCraft, CraftingPanelUI panel)
            {
                this.recipe = recipe;
                this.palette = palette;
                this.panel = panel;

                root = UIFactory.CreateRect(parent, "Recipe_" + recipe.name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                UIFactory.SetHeight(root, 74f);

                bg = root.gameObject.AddComponent<Image>();
                bg.sprite = UIFactory.GetRoundedFillSprite(8f);
                bg.type = Image.Type.Sliced;
                bg.color = palette.rowBackground;

                // left accent bar tinted by the output's rarity
                var accentRect = UIFactory.CreateRect(root, "Accent", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                accentRect.sizeDelta = new Vector2(5f, 0f);
                accent = accentRect.gameObject.AddComponent<Image>();
                accent.sprite = UIFactory.GetRoundedFillSprite(2.5f);
                accent.type = Image.Type.Sliced;
                accent.raycastTarget = false;

                var iconRect = UIFactory.CreateAnchored(root, "Icon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 56f));
                iconRect.offsetMin = new Vector2(6f, -28f);
                iconRect.offsetMax = new Vector2(62f, 28f);
                var iconImg = iconRect.gameObject.AddComponent<Image>();
                iconImg.sprite = recipe.Icon != null ? recipe.Icon : UIFactory.GetSolidSprite(new Color(0.35f, 0.4f, 0.5f));
                iconImg.color = Color.white;

                nameText = UIFactory.CreateText(root, "Name", recipe.DisplayName, 20, palette.textPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
                nameText.rectTransform.anchorMin = new Vector2(0f, 0.55f);
                nameText.rectTransform.anchorMax = new Vector2(0.72f, 1f);
                nameText.rectTransform.offsetMin = new Vector2(72f, 0f);
                nameText.rectTransform.offsetMax = new Vector2(-8f, 0f);

                summaryText = UIFactory.CreateText(root, "Summary", recipe.GetSummary(), 15, palette.textSecondary, TextAnchor.MiddleLeft);
                summaryText.rectTransform.anchorMin = new Vector2(0f, 0f);
                summaryText.rectTransform.anchorMax = new Vector2(0.72f, 0.5f);
                summaryText.rectTransform.offsetMin = new Vector2(72f, 0f);
                summaryText.rectTransform.offsetMax = new Vector2(-8f, 0f);

                reasonText = UIFactory.CreateText(root, "Reason", "", 14, palette.danger, TextAnchor.MiddleLeft);
                reasonText.rectTransform.anchorMin = new Vector2(0f, 0f);
                reasonText.rectTransform.anchorMax = new Vector2(0.72f, 0.34f);
                reasonText.rectTransform.offsetMin = new Vector2(72f, 0f);
                reasonText.rectTransform.offsetMax = new Vector2(-8f, 0f);

                var craftRect = UIFactory.CreateRect(root, "CraftButton", new Vector2(0.74f, 0f), new Vector2(0.98f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                craftButton = UIFactory.CreateButton(craftRect, "Btn", "Craft", () => onCraft?.Invoke(recipe), palette, 17);
                craftButton.GetComponentInChildren<Text>().raycastTarget = false;

                lockOverlay = UIFactory.CreateStretch(root, "Lock").gameObject.AddComponent<Image>();
                lockOverlay.color = new Color(0f, 0f, 0f, 0f);
                lockOverlay.raycastTarget = false;

                // drag-to-craft target
                var dropHandler = root.gameObject.AddComponent<RecipeRowDropHandler>();
                dropHandler.Init(recipe, panel);

                RefreshState();
            }

            public void RefreshState()
            {
                if (recipe == null || panel == null || panel.crafting == null || panel.inventory == null || panel.profile == null) return;

                var crafting = panel.crafting;
                var profile = panel.profile;

                bool highlighted = panel.highlightedRecipe == recipe;
                bool canCraft = crafting.CanQueue(recipe) == CraftRejection.None;

                bg.color = canCraft || highlighted
                    ? (highlighted ? new Color(0.18f, 0.30f, 0.24f, 1f) : palette.rowBackground)
                    : palette.rowBackgroundLocked;

                if (accent != null)
                {
                    var outItem = recipe.Outputs.Count > 0 ? recipe.Outputs[0].item : null;
                    var rar = outItem != null ? palette.Get(outItem.Rarity) : palette.accent;
                    accent.color = canCraft
                        ? new Color(rar.r, rar.g, rar.b, 0.95f)
                        : new Color(0.42f, 0.44f, 0.48f, 0.5f);
                }

                craftButton.interactable = canCraft;
                lockOverlay.color = canCraft ? new Color(0f, 0f, 0f, 0f) : palette.lockedOverlay;
                nameText.color = canCraft ? palette.textPrimary : palette.textDisabled;
                summaryText.color = canCraft ? palette.textSecondary : palette.textDisabled;

                string reason = GetReason(crafting, profile);
                reasonText.text = reason;
                reasonText.gameObject.SetActive(!canCraft && !string.IsNullOrEmpty(reason));

                craftButton.GetComponentInChildren<Text>().color = canCraft ? Color.white : palette.textDisabled;
            }

            private string GetReason(CraftingSystem crafting, PlayerProfile profile)
            {
                if (profile.Level < recipe.LevelRequirement) return $"Requires level {recipe.LevelRequirement}";
                if (recipe.GoldCost > 0 && profile.Gold < recipe.GoldCost) return $"Needs {recipe.GoldCost} gold";
                if (recipe.XpCost > 0 && profile.Xp < recipe.XpCost) return $"Needs {recipe.XpCost} XP";
                float cd = crafting.GetCooldownRemaining(recipe);
                if (cd > 0f) return $"Cooldown {cd:F0}s";
                return "Missing materials";
            }

            public void Destroy()
            {
                if (root != null) Object.Destroy(root.gameObject);
            }
        }

        /// <summary>Receives drops onto a recipe row (drag to craft).</summary>
        private class RecipeRowDropHandler : MonoBehaviour, IDropHandler
        {
            private RecipeDefinition recipe;
            private CraftingPanelUI panel;

            public void Init(RecipeDefinition recipe, CraftingPanelUI panel)
            {
                this.recipe = recipe;
                this.panel = panel;
            }

            public void OnDrop(PointerEventData eventData)
            {
                var drag = DragDropService.Instance;
                if (drag == null || !drag.IsDragging) return;
                drag.EndDrag();
                panel?.HandleDropOnRecipe(recipe);
            }
        }

        // ================================================================== queue row

        private class QueueRow
        {
            private RectTransform root;
            private CraftJob job;
            private RectTransform fill;
            private Text label;
            private Button cancelButton;

            public void Build(RectTransform parent, CraftJob job, int queueIndex, RarityPalette palette, CraftingSystem crafting)
            {
                this.job = job;
                root = UIFactory.CreateRect(parent, "Queue_" + job.recipe.name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                UIFactory.SetHeight(root, 34f);

                var bg = root.gameObject.AddComponent<Image>();
                bg.sprite = UIFactory.GetRoundedFillSprite(6f);
                bg.type = Image.Type.Sliced;
                bg.color = palette.rowBackground;

                label = UIFactory.CreateText(root, "Label", job.recipe.DisplayName, 16, palette.textPrimary, TextAnchor.MiddleLeft);
                label.rectTransform.anchorMin = new Vector2(0f, 0.6f);
                label.rectTransform.anchorMax = new Vector2(0.8f, 1f);
                label.rectTransform.offsetMin = new Vector2(8f, 0f);
                label.rectTransform.offsetMax = new Vector2(-4f, 0f);

                var barRect = UIFactory.CreateRect(root, "Bar", new Vector2(0f, 0f), new Vector2(0.8f, 0.5f), new Vector2(0.5f, 0f),
                    new Vector2(8f, 3f), new Vector2(-4f, -4f));
                fill = UIFactory.CreateProgressBar(barRect, "Fill", palette.success, 10f);

                if (job.state == CraftJobState.Queued)
                {
                    var cancelRect = UIFactory.CreateRect(root, "Cancel", new Vector2(0.84f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    cancelButton = UIFactory.CreateButton(cancelRect, "Btn", "X", () => crafting.Cancel(queueIndex), palette, 15);
                }
            }

            public void RefreshProgress()
            {
                if (fill == null || job == null) return;
                UIFactory.SetProgress(fill, job.Progress);
            }

            public void Destroy()
            {
                if (root != null) Object.Destroy(root.gameObject);
            }
        }
    }
}
