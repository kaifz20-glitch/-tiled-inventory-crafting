using System.Collections.Generic;
using DJS.TiledInventoryCrafting;
using UnityEditor;
using UnityEngine;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    /// <summary>
    /// Self-check against the product's success criteria: demo scene present, demo
    /// content complete, recipes valid, ids unique, and the "craft a sword from
    /// 5 wood + 2 iron" path verified. Run from
    /// <c>Tools &gt; Tiled Inventory &gt; Validate Package</c>.
    /// </summary>
    public static class PackageValidator
    {
        private static readonly List<string> errors = new List<string>();
        private static readonly List<string> warnings = new List<string>();
        private static readonly List<string> info = new List<string>();

        [MenuItem("Tools/Tiled Inventory/Validate Package")]
        public static void Validate()
        {
            errors.Clear();
            warnings.Clear();
            info.Clear();

            CheckScene();
            CheckItems();
            CheckRecipes();
            CheckAchievements();
            CheckSwordScenario();

            string report = BuildReport();
            Debug.Log(report);
            if (errors.Count == 0)
                EditorUtility.DisplayDialog("Tiled Inventory — Validation", report, "OK");
            else
                EditorUtility.DisplayDialog("Tiled Inventory — Validation", report, "Fix issues");
        }

        private static string BuildReport()
        {
            var lines = new List<string> { "TILED INVENTORY & CRAFTING — VALIDATION REPORT", "" };
            if (errors.Count == 0) lines.Add("✓ No errors.");
            foreach (var e in errors) lines.Add("✗ " + e);
            if (warnings.Count == 0) lines.Add("✓ No warnings.");
            foreach (var w in warnings) lines.Add("! " + w);
            if (info.Count > 0) { lines.Add(""); lines.Add("Info:"); foreach (var i in info) lines.Add("  · " + i); }
            return string.Join("\n", lines);
        }

        // ------------------------------------------------------------------ checks

        private static void CheckScene()
        {
            string scenePath = DemoContentBuilder.DemoFolder + "/Demo.unity";
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (scene == null)
            {
                errors.Add($"Demo scene missing. Run Tools → Tiled Inventory → Build Demo Scene ({scenePath}).");
                return;
            }
            info.Add($"Demo scene found: {scenePath}");
        }

        private static List<ItemDefinition> CheckItems()
        {
            var items = LoadAll<ItemDefinition>(DemoContentBuilder.ItemsFolder);
            if (items.Count == 0)
            {
                errors.Add("No demo items found. Run Tools → Tiled Inventory → Create Demo Content.");
                return items;
            }
            info.Add($"{items.Count} item definitions found.");

            var seen = new HashSet<string>();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Id)) errors.Add($"Item '{item.name}' has an empty Id.");
                else if (!seen.Add(item.Id)) errors.Add($"Duplicate item Id: '{item.Id}' ({item.name}).");
                if (item.MaxStack < 1) errors.Add($"Item '{item.name}' has maxStack < 1.");
            }
            return items;
        }

        private static void CheckRecipes()
        {
            var recipes = LoadAll<RecipeDefinition>(DemoContentBuilder.RecipesFolder);
            if (recipes.Count == 0)
            {
                errors.Add("No recipes found. Run Tools → Tiled Inventory → Create Demo Content.");
                return;
            }
            info.Add($"{recipes.Count} recipe definitions found.");

            var seen = new HashSet<string>();
            foreach (var recipe in recipes)
            {
                if (string.IsNullOrEmpty(recipe.Id)) errors.Add($"Recipe '{recipe.name}' has an empty Id.");
                else if (!seen.Add(recipe.Id)) errors.Add($"Duplicate recipe Id: '{recipe.Id}' ({recipe.name}).");

                if (recipe.Outputs.Count == 0)
                    errors.Add($"Recipe '{recipe.name}' has no outputs.");
                if (recipe.Inputs.Count == 0 && recipe.SpecialCosts.Count == 0)
                    warnings.Add($"Recipe '{recipe.name}' costs nothing (free craft).");

                foreach (var input in recipe.Inputs)
                    if (input.item == null) errors.Add($"Recipe '{recipe.name}' has an input with a missing item.");
                foreach (var output in recipe.Outputs)
                    if (output.item == null) errors.Add($"Recipe '{recipe.name}' has an output with a missing item.");
                foreach (var cost in recipe.SpecialCosts)
                    if (cost.item == null) errors.Add($"Recipe '{recipe.name}' has a special cost with a missing item.");
            }
        }

        private static void CheckAchievements()
        {
            var achievements = LoadAll<AchievementDefinition>(DemoContentBuilder.AchievementsFolder);
            if (achievements.Count == 0)
            {
                warnings.Add("No achievements found (Phase 3 feature is optional).");
                return;
            }
            info.Add($"{achievements.Count} achievement definitions found.");
            foreach (var achievement in achievements)
            {
                if (string.IsNullOrEmpty(achievement.StatKey))
                    errors.Add($"Achievement '{achievement.name}' has an empty statKey.");
                if (achievement.TargetValue < 1)
                    errors.Add($"Achievement '{achievement.name}' has targetValue < 1.");
            }
        }

        /// <summary>The headline success criterion: craft a sword from 5 wood + 2 iron.</summary>
        private static void CheckSwordScenario()
        {
            var items = LoadAll<ItemDefinition>(DemoContentBuilder.ItemsFolder);
            var recipes = LoadAll<RecipeDefinition>(DemoContentBuilder.RecipesFolder);

            var wood = items.Find(i => i != null && i.name == "Wood");
            var iron = items.Find(i => i != null && i.name == "Iron");
            var sword = items.Find(i => i != null && i.name == "Sword");
            var swordRecipe = recipes.Find(r => r != null && r.name.StartsWith("Sword"));

            if (wood == null || iron == null || sword == null)
            {
                errors.Add("Sword scenario incomplete: need Wood, Iron and Sword items.");
                return;
            }
            if (swordRecipe == null)
            {
                errors.Add("Sword scenario incomplete: no 'Sword' recipe.");
                return;
            }

            bool hasWood = false, hasIron = false, outputsSword = false;
            foreach (var input in swordRecipe.Inputs)
            {
                if (input.item == wood && input.count == 5) hasWood = true;
                if (input.item == iron && input.count == 2) hasIron = true;
            }
            foreach (var output in swordRecipe.Outputs)
                if (output.item == sword && output.count == 1) outputsSword = true;

            if (hasWood && hasIron && outputsSword)
                info.Add("Sword scenario OK: 5 Wood + 2 Iron → 1 Sword.");
            else
                errors.Add($"Sword recipe mismatch (expected 5 Wood + 2 Iron → 1 Sword), got: {swordRecipe.GetSummary()}.");
        }

        // ------------------------------------------------------------------ helpers

        private static List<T> LoadAll<T>(string folder) where T : Object
        {
            var list = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            return list;
        }
    }
}
