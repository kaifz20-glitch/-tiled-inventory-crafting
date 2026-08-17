using System.Collections.Generic;
using UnityEngine;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// One craftable recipe: a list of inputs (materials), outputs, craft time,
    /// level requirement, resource costs (gold/XP/special materials), failure chance
    /// and cooldown. Plain data — the <see cref="CraftingSystem"/> executes it.
    ///
    /// Create recipes from the menu:
    /// <c>Assets &gt; Create &gt; Tiled Inventory &gt; Recipe Definition</c>,
    /// or visually with the node-graph editor (Window &gt; Tiled Inventory &gt; Recipe Graph Editor).
    /// </summary>
    [CreateAssetMenu(menuName = "Tiled Inventory/Recipe Definition", fileName = "Recipe", order = 1)]
    public class RecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "New Recipe";
        [SerializeField] private Sprite icon;

        [Header("Inputs → Outputs")]
        [SerializeField] private List<ItemStack> inputs = new List<ItemStack>();
        [SerializeField] private List<ItemStack> outputs = new List<ItemStack>();

        [Header("Requirements")]
        [SerializeField] private float craftTime = 2f;
        [SerializeField] private int levelRequirement = 1;

        [Header("Economy (Phase 2)")]
        [Tooltip("Gold removed from the player when the craft is queued.")]
        [SerializeField] private int goldCost;
        [Tooltip("XP removed from the player when the craft is queued.")]
        [SerializeField] private int xpCost;
        [Tooltip("Extra special materials consumed on top of inputs.")]
        [SerializeField] private List<ItemStack> specialCosts = new List<ItemStack>();

        [Header("Risk & Pace (Phase 2)")]
        [Tooltip("Base chance (0..1) the craft fails and consumes materials with no output.")]
        [Range(0f, 1f)]
        [SerializeField] private float failureChance;
        [Tooltip("Failure chance is reduced by craftingSkill * this amount.")]
        [SerializeField] private float failureChanceReductionPerSkill = 0.005f;
        [Tooltip("Seconds a player must wait after finishing this recipe before they can queue it again.")]
        [SerializeField] private float cooldownSeconds;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public IReadOnlyList<ItemStack> Inputs => inputs;
        public IReadOnlyList<ItemStack> Outputs => outputs;
        public float CraftTime => craftTime;
        public int LevelRequirement => levelRequirement;
        public int GoldCost => goldCost;
        public int XpCost => xpCost;
        public IReadOnlyList<ItemStack> SpecialCosts => specialCosts;
        public float FailureChance => failureChance;
        public float FailureChanceReductionPerSkill => failureChanceReductionPerSkill;
        public float CooldownSeconds => cooldownSeconds;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = System.Guid.NewGuid().ToString("N").Substring(0, 12);
            if (string.IsNullOrEmpty(displayName))
                displayName = name;
            if (craftTime < 0f) craftTime = 0f;
            if (cooldownSeconds < 0f) cooldownSeconds = 0f;
            for (int i = 0; i < inputs.Count; i++)
                if (inputs[i].count < 1) inputs[i] = new ItemStack(inputs[i].item, 1);
            for (int i = 0; i < outputs.Count; i++)
                if (outputs[i].count < 1) outputs[i] = new ItemStack(outputs[i].item, 1);
            for (int i = 0; i < specialCosts.Count; i++)
                if (specialCosts[i].count < 1) specialCosts[i] = new ItemStack(specialCosts[i].item, 1);
        }

        private void OnEnable() => Registry.RegisterRecipe(this);
        private void OnDisable() => Registry.UnregisterRecipe(this);

        /// <summary>Effective failure chance for a crafter with the given skill level.</summary>
        public float GetFailureChance(int craftingSkill)
        {
            float reduced = failureChance - craftingSkill * failureChanceReductionPerSkill;
            return Mathf.Clamp01(reduced);
        }

        /// <summary>Do the grids hold every material this recipe needs (inputs + special costs)?</summary>
        public bool HasMaterials(InventoryGrid grid)
        {
            for (int i = 0; i < inputs.Count; i++)
                if (!grid.Contains(inputs[i].item, inputs[i].count)) return false;
            for (int i = 0; i < specialCosts.Count; i++)
                if (!grid.Contains(specialCosts[i].item, specialCosts[i].count)) return false;
            return true;
        }

        /// <summary>Human-readable one-line summary, e.g. "5 Wood + 2 Iron → 1 Sword".</summary>
        public string GetSummary()
        {
            var parts = new List<string>();
            foreach (var input in inputs) parts.Add($"{input.count} {input.item?.DisplayName ?? "?"}");
            foreach (var cost in specialCosts) parts.Add($"{cost.count} {cost.item?.DisplayName ?? "?"}");
            string arrow = "→";
            var outParts = new List<string>();
            foreach (var output in outputs) outParts.Add($"{output.count} {output.item?.DisplayName ?? "?"}");
            return $"{string.Join(" + ", parts)} {arrow} {string.Join(" + ", outParts)}";
        }
    }
}
