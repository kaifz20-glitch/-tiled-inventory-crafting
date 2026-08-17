using System.Collections.Generic;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Global id → asset lookup for items and recipes. Instances self-register in
    /// OnEnable, so any loaded asset is resolvable by id — which is how save files
    /// and network messages refer to content without hard references.
    /// </summary>
    public static class Registry
    {
        private static readonly Dictionary<string, ItemDefinition> items = new Dictionary<string, ItemDefinition>();
        private static readonly Dictionary<string, RecipeDefinition> recipes = new Dictionary<string, RecipeDefinition>();

        public static void RegisterItem(ItemDefinition item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) return;
            items[item.Id] = item;
        }

        public static void UnregisterItem(ItemDefinition item)
        {
            if (item != null) items.Remove(item.Id);
        }

        public static ItemDefinition FindItem(string id)
        {
            return id != null && items.TryGetValue(id, out var item) ? item : null;
        }

        public static void RegisterRecipe(RecipeDefinition recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.Id)) return;
            recipes[recipe.Id] = recipe;
        }

        public static void UnregisterRecipe(RecipeDefinition recipe)
        {
            if (recipe != null) recipes.Remove(recipe.Id);
        }

        public static RecipeDefinition FindRecipe(string id)
        {
            return id != null && recipes.TryGetValue(id, out var recipe) ? recipe : null;
        }

        public static IEnumerable<ItemDefinition> AllItems => items.Values;
        public static IEnumerable<RecipeDefinition> AllRecipes => recipes.Values;
    }
}
