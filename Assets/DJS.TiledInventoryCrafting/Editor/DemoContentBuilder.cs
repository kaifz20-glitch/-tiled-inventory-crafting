using System.Collections.Generic;
using System.IO;
using DJS.TiledInventoryCrafting;
using UnityEditor;
using UnityEngine;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    /// <summary>
    /// Generates the demo content (items, recipes, achievements + generated icons) as
    /// real asset files, so the demo is self-contained on a fresh import. Idempotent:
    /// existing assets are found and reused, never duplicated.
    /// </summary>
    public static class DemoContentBuilder
    {
        public const string DemoFolder = "Assets/DJS.TiledInventoryCrafting/Demo";
        public const string ItemsFolder = DemoFolder + "/Items";
        public const string RecipesFolder = DemoFolder + "/Recipes";
        public const string AchievementsFolder = DemoFolder + "/Achievements";
        public const string IconsFolder = DemoFolder + "/Icons";

        // ------------------------------------------------------------------ entry

        [MenuItem("Tools/Tiled Inventory/Create Demo Content")]
        public static void EnsureAll()
        {
            EnsureFolders();
            var items = EnsureItems();
            var recipes = EnsureRecipes(items);
            EnsureAchievements(recipes);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DJS.TiledInventoryCrafting] Demo content ready: {items.Count} items, {recipes.Count} recipes.");
        }

        // ------------------------------------------------------------------ folders

        public static void EnsureFolders()
        {
            EnsureFolder("Assets/DJS.TiledInventoryCrafting");
            EnsureFolder(DemoFolder);
            EnsureFolder(ItemsFolder);
            EnsureFolder(RecipesFolder);
            EnsureFolder(AchievementsFolder);
            EnsureFolder(IconsFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        // ------------------------------------------------------------------ icons

        /// <summary>
        /// Generate an icon texture asset (sprite) with the painter for <paramref name="pattern"/>.
        /// Idempotent: when the rendered bytes are unchanged the existing asset is reused,
        /// so item/recipe references stay valid across rebuilds. A renderer change produces
        /// new bytes that overwrite the PNG in place (same GUID, new pixels).
        /// </summary>
        public static Sprite EnsureIcon(string name, Color baseColor, Color detailColor, int pattern)
        {
            string path = $"{IconsFolder}/{name}.png";
            const int size = 128;

            var painter = new IconPainter(size);
            RenderIcon(painter, pattern, baseColor, detailColor);
            var tex = painter.Build(name);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            if (File.Exists(path))
            {
                byte[] old = File.ReadAllBytes(path);
                if (old.Length == png.Length && BytesEqual(old, png))
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static void RenderIcon(IconPainter p, int pattern, Color baseColor, Color detailColor)
        {
            switch (pattern)
            {
                case 0: DrawWood(p, baseColor, detailColor); break;
                case 1: DrawOre(p, baseColor, detailColor); break;
                case 2: DrawIronBar(p, baseColor, detailColor); break;
                case 3: DrawLeather(p, baseColor, detailColor); break;
                case 4: DrawSword(p, baseColor, detailColor); break;
                case 5: DrawHelmet(p, baseColor, detailColor); break;
                case 6: DrawChest(p, baseColor, detailColor); break;
                case 7: DrawCoin(p, baseColor, detailColor); break;
                case 8: DrawPotion(p, baseColor, detailColor); break;
                case 9: DrawMedallion(p, baseColor, detailColor); break;
                case 10: DrawSwordBadge(p, baseColor, detailColor); break;
                default: DrawMedallion(p, baseColor, detailColor); break;
            }
        }

        private static void Plate(IconPainter p) => p.Plate(
            new Color(0.17f, 0.185f, 0.235f),
            new Color(0.105f, 0.115f, 0.15f),
            new Color(0.42f, 0.46f, 0.55f));

        private static Color Darken(Color c, float amt) => Color.Lerp(c, Color.black, amt);
        private static Color Lighten(Color c, float amt) => Color.Lerp(c, Color.white, amt);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.28f);

        private static void DrawWood(IconPainter p, Color base_, Color detail)
        {
            Plate(p);
            Color dark = Darken(base_, 0.35f);
            Color light = Lighten(base_, 0.25f);
            p.FillCapsule(28, 66, 98, 66, 20, base_);            // log body
            p.FillCircle(98, 66, 18, dark);                      // end grain
            p.FillCircle(98, 66, 12, base_);
            p.FillCircle(98, 66, 7, dark);
            p.FillCircle(98, 66, 2.5f, light);
            p.FillCapsule(38, 50, 92, 50, 1.6f, dark);           // grain strokes
            p.FillCapsule(34, 58, 88, 58, 1.3f, dark);
            p.FillCapsule(36, 76, 90, 76, 1.6f, dark);
            p.FillCapsule(40, 84, 86, 84, 1.2f, dark);
            p.FillCircle(56, 72, 5, dark);                       // knot
            p.FillCircle(56, 72, 2.4f, light);
        }

        private static void DrawOre(IconPainter p, Color rock, Color chunk)
        {
            Plate(p);
            Color dark = Darken(rock, 0.4f);
            p.FillCircle(48, 60, 24, rock);                      // lumpy blob
            p.FillCircle(72, 52, 19, rock);
            p.FillCircle(62, 76, 17, rock);
            p.FillCircle(60, 62, 26, rock);
            p.FillCircle(42, 54, 6, chunk);                      // embedded ore chunks
            p.FillCircle(66, 44, 5, chunk);
            p.FillCircle(58, 70, 5.5f, chunk);
            p.FillCircle(78, 62, 4.5f, chunk);
            p.FillCircle(50, 78, 4, chunk);
            p.FillCapsule(36, 68, 54, 60, 1.4f, dark);           // cracks
            p.FillCapsule(60, 82, 76, 72, 1.3f, dark);
            p.FillCapsule(74, 48, 84, 56, 1.2f, dark);
        }

        private static void DrawIronBar(IconPainter p, Color steel, Color dark)
        {
            Plate(p);
            Color light = Lighten(steel, 0.45f);
            p.FillRoundRect(64, 64, 38, 15, 7, steel);           // bar
            p.FillRoundRect(64, 57, 32, 3.4f, 2.5f, light);      // top sheen
            p.FillRoundRect(64, 72, 32, 2.6f, 2f, dark);         // bottom shade
            p.FillRoundRect(28, 64, 3.2f, 12, 2f, dark);         // end caps
            p.FillRoundRect(100, 64, 3.2f, 12, 2f, dark);
            p.FillCapsule(46, 62, 82, 62, 1.8f, light);          // center glint
        }

        private static void DrawLeather(IconPainter p, Color hide, Color stitch)
        {
            Plate(p);
            Color inner = Lighten(hide, 0.22f);
            p.FillRoundRectRot(64, 66, 34, 24, 9, -5f, hide);    // pelt
            p.FillRoundRectRot(64, 66, 26, 17, 6, -5f, inner);   // inner panel
            for (int i = 0; i < 6; i++)                          // stitching
            {
                float x = 44 + i * 8;
                p.FillCircle(x, 54, 1.7f, stitch);
                p.FillCircle(x + 4, 78, 1.7f, stitch);
            }
            p.FillCircle(38, 66, 2, stitch);
        }

        private static void DrawSword(IconPainter p, Color steel, Color grip)
        {
            Plate(p);
            Color light = Lighten(steel, 0.5f);
            Color dark = Darken(steel, 0.35f);
            p.FillCapsule(108, 28, 50, 80, 6.5f, steel);         // blade
            p.FillCapsule(107, 26, 49, 78, 2f, light);           // edge highlight
            p.FillCapsule(104, 31, 53, 77, 1.4f, dark);          // fuller
            p.FillCapsule(42, 68, 62, 90, 7f, Gold);             // guard
            p.FillCapsule(50, 80, 40, 92, 6f, grip);             // grip
            p.FillCircle(35, 97, 7f, Gold);                      // pommel
            p.FillCircle(35, 97, 3f, light);
        }

        private static void DrawHelmet(IconPainter p, Color steel, Color dark)
        {
            Plate(p);
            Color light = Lighten(steel, 0.45f);
            p.Fill((px, py) => Mathf.Max(
                Mathf.Sqrt((px - 64f) * (px - 64f) + (py - 66f) * (py - 66f)) - 33f,
                py - 70f), steel);                                // dome (clipped)
            p.FillRoundRect(64, 72, 30, 5, 3, steel);            // brim
            p.FillRoundRect(64, 52, 3.6f, 16, 2, dark);          // nose slit
            p.FillCircle(37, 72, 2.6f, Gold);                    // rivets
            p.FillCircle(91, 72, 2.6f, Gold);
            p.FillCapsule(42, 44, 58, 38, 3, light);             // top highlight
        }

        private static void DrawChest(IconPainter p, Color steel, Color dark)
        {
            Plate(p);
            Color light = Lighten(steel, 0.4f);
            p.FillRoundRect(64, 66, 26, 26, 9, steel);           // torso plate
            p.FillRoundRect(64, 62, 4.5f, 21, 3.5f, light);      // center ridge
            p.FillCircle(46, 46, 6, light);                      // pauldron bumps
            p.FillCircle(82, 46, 6, light);
            p.FillRoundRect(64, 74, 20, 3.2f, 1.6f, dark);       // belt
            p.FillRoundRect(64, 74, 4, 3.2f, 1.6f, Gold);        // buckle
            p.FillCircle(44, 60, 2.2f, Gold);                    // rivets
            p.FillCircle(84, 60, 2.2f, Gold);
            p.FillCircle(44, 76, 2.2f, Gold);
            p.FillCircle(84, 76, 2.2f, Gold);
        }

        private static void DrawCoin(IconPainter p, Color gold, Color dark)
        {
            Plate(p);
            Color light = Lighten(gold, 0.5f);
            p.FillCircle(64, 64, 27, gold);                      // coin
            p.FillCircle(64, 64, 21, Lighten(gold, 0.12f));      // inner face
            p.FillRing(64, 64, 24, 2.6f, light);                 // raised rim
            p.FillCapsule(53, 64, 75, 64, 2.4f, dark);           // emblem
            p.FillCapsule(64, 53, 64, 75, 2.4f, dark);
            p.FillCircle(64, 64, 3.2f, dark);
            p.FillCapsule(48, 46, 56, 40, 2.6f, light);          // shine
        }

        private static void DrawPotion(IconPainter p, Color liquid, Color liquidDark)
        {
            Plate(p);
            Color glass = new Color(0.82f, 0.88f, 0.95f);
            Color light = Lighten(liquid, 0.5f);
            p.FillCapsule(64, 36, 64, 58, 6.5f, glass, 0.5f);    // glass neck
            p.FillCircle(64, 76, 24, glass, 0.5f);               // glass body
            p.Fill((px, py) => Mathf.Max(
                Mathf.Sqrt((px - 64f) * (px - 64f) + (py - 76f) * (py - 76f)) - 21.5f,
                70f - py), liquid);                              // liquid
            p.FillRoundRect(64, 71, 14, 2.2f, 1.8f, light);      // liquid surface
            p.FillRoundRect(64, 30, 8, 8, 2.5f, new Color(0.55f, 0.38f, 0.22f)); // cork
            p.FillCapsule(50, 66, 56, 54, 2.4f, Color.white, 0.55f); // glass shine
            p.FillCircle(56, 80, 2.6f, light);                   // bubbles
            p.FillCircle(70, 86, 2f, light);
            p.FillCircle(64, 97, 6, liquidDark);                 // drip
        }

        private static void DrawMedallion(IconPainter p, Color base_, Color detail)
        {
            Plate(p);
            Color light = Lighten(base_, 0.45f);
            Color dark = Darken(base_, 0.35f);
            p.FillCircle(64, 64, 30, base_);                     // badge
            p.FillRing(64, 64, 24, 3, light);                    // rim
            p.FillCircle(64, 64, 20, dark);                      // inner
            p.FillCapsule(48, 64, 80, 64, 5, base_);             // star
            p.FillCapsule(64, 48, 64, 80, 5, base_);
            p.FillCircle(64, 64, 5, light);
        }

        private static void DrawSwordBadge(IconPainter p, Color gold, Color steel)
        {
            Plate(p);
            Color light = Lighten(gold, 0.45f);
            Color dark = Darken(gold, 0.35f);
            p.FillCircle(64, 64, 30, gold);                      // badge
            p.FillRing(64, 64, 24, 3, light);                    // rim
            p.FillCircle(64, 64, 20, dark);                      // inner
            p.FillCapsule(88, 42, 58, 68, 4, steel);             // mini sword
            p.FillCapsule(87, 40, 57, 66, 1.4f, Color.white);
            p.FillCapsule(50, 60, 60, 70, 4, light);             // guard
            p.FillCapsule(58, 68, 50, 76, 3.2f, new Color(0.45f, 0.28f, 0.10f)); // grip
            p.FillCircle(47, 79, 4, light);                      // pommel
        }

        // ------------------------------------------------------------------ items

        public static Dictionary<string, ItemDefinition> EnsureItems()
        {
            var items = new Dictionary<string, ItemDefinition>();

            items["wood"] = EnsureItem("Wood", "Wood", ItemCategory.Material, ItemRarity.Common,
                new Color(0.62f, 0.42f, 0.20f), new Color(0.34f, 0.21f, 0.10f), 0, 99, EquipmentSlotType.None,
                "Sturdy timber gathered from the forest. A blacksmith's bread and butter.");

            items["ironOre"] = EnsureItem("IronOre", "Iron Ore", ItemCategory.Material, ItemRarity.Common,
                new Color(0.50f, 0.42f, 0.36f), new Color(0.72f, 0.76f, 0.82f), 1, 99, EquipmentSlotType.None,
                "Raw ore pulled from the mine. Must be smelted before use.");

            items["iron"] = EnsureItem("Iron", "Iron Bar", ItemCategory.Material, ItemRarity.Uncommon,
                new Color(0.66f, 0.70f, 0.77f), new Color(0.40f, 0.44f, 0.52f), 2, 99, EquipmentSlotType.None,
                "A refined bar of iron, ready for the forge.");

            items["leather"] = EnsureItem("Leather", "Leather", ItemCategory.Material, ItemRarity.Common,
                new Color(0.66f, 0.52f, 0.38f), new Color(0.36f, 0.26f, 0.16f), 3, 99, EquipmentSlotType.None,
                "Cured hide. Soft, durable, and smells like adventure.");

            items["sword"] = EnsureItem("Sword", "Iron Sword", ItemCategory.Weapon, ItemRarity.Rare,
                new Color(0.85f, 0.87f, 0.90f), new Color(0.45f, 0.28f, 0.10f), 4, 1, EquipmentSlotType.Weapon,
                "A well-balanced blade forged from five planks of wood and two bars of iron.",
                new List<StatModifier> { new StatModifier { stat = StatType.Damage, value = 8 } });

            items["helmet"] = EnsureItem("Helmet", "Iron Helmet", ItemCategory.Helmet, ItemRarity.Uncommon,
                new Color(0.70f, 0.73f, 0.78f), new Color(0.38f, 0.42f, 0.50f), 5, 1, EquipmentSlotType.Head,
                "Protects what matters most.",
                new List<StatModifier> { new StatModifier { stat = StatType.Armor, value = 3 } });

            items["chestplate"] = EnsureItem("Chestplate", "Iron Chestplate", ItemCategory.Armor, ItemRarity.Uncommon,
                new Color(0.70f, 0.73f, 0.78f), new Color(0.38f, 0.42f, 0.50f), 6, 1, EquipmentSlotType.Chest,
                "Heavy plate that turns cuts into dents.",
                new List<StatModifier> { new StatModifier { stat = StatType.Armor, value = 5 } });

            items["goldCoin"] = EnsureItem("GoldCoin", "Gold Coin", ItemCategory.Currency, ItemRarity.Uncommon,
                new Color(1f, 0.80f, 0.30f), new Color(0.82f, 0.60f, 0.14f), 7, 999, EquipmentSlotType.None,
                "Shiny. Spendable. The universal language.");

            items["potion"] = EnsureItem("Potion", "Health Potion", ItemCategory.Consumable, ItemRarity.Epic,
                new Color(0.85f, 0.22f, 0.28f), new Color(0.55f, 0.08f, 0.12f), 8, 20, EquipmentSlotType.None,
                "A bubbling brew that mends wounds. Tastes like cherries and regret.");

            return items;
        }

        private static ItemDefinition EnsureItem(string assetName, string displayName, ItemCategory category,
            ItemRarity rarity, Color baseColor, Color detailColor, int pattern, int maxStack,
            EquipmentSlotType slot, string description, List<StatModifier> stats = null)
        {
            string path = $"{ItemsFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);

            // icons are re-rendered on every pass (byte-compare makes it a no-op when
            // nothing changed), so icon art updates propagate to existing assets too
            var icon = EnsureIcon(assetName, baseColor, detailColor, pattern);
            if (existing != null) return existing;

            var item = ScriptableObject.CreateInstance<ItemDefinition>();

            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = "demo_" + assetName.ToLowerInvariant();
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("rarity").enumValueIndex = (int)rarity;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("maxStack").intValue = maxStack;
            so.FindProperty("equippableSlot").enumValueIndex = (int)slot;
            so.FindProperty("sellValue").intValue = Mathf.Max(1, maxStack / 10 + 1);
            var statsProp = so.FindProperty("stats");
            statsProp.ClearArray();
            if (stats != null)
            {
                for (int i = 0; i < stats.Count; i++)
                {
                    statsProp.InsertArrayElementAtIndex(i);
                    var el = statsProp.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("stat").enumValueIndex = (int)stats[i].stat;
                    el.FindPropertyRelative("value").intValue = stats[i].value;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(item, path);
            EditorUtility.SetDirty(item);
            return item;
        }

        // ------------------------------------------------------------------ recipes

        public static List<RecipeDefinition> EnsureRecipes(Dictionary<string, ItemDefinition> items)
        {
            var recipes = new List<RecipeDefinition>();

            recipes.Add(EnsureRecipe("IronBar", "Smelt Iron Bar",
                new List<ItemStack> { new ItemStack(items["ironOre"], 3) },
                new List<ItemStack> { new ItemStack(items["iron"], 1) },
                2f, 1, 0, 0, null, 0f, 0f));

            recipes.Add(EnsureRecipe("Sword", "Forge Sword",
                new List<ItemStack> { new ItemStack(items["wood"], 5), new ItemStack(items["iron"], 2) },
                new List<ItemStack> { new ItemStack(items["sword"], 1) },
                3f, 1, 0, 0, null, 0f, 0f));

            recipes.Add(EnsureRecipe("Helmet", "Forge Helmet",
                new List<ItemStack> { new ItemStack(items["iron"], 2) },
                new List<ItemStack> { new ItemStack(items["helmet"], 1) },
                2f, 2, 0, 0, null, 0f, 0f));

            recipes.Add(EnsureRecipe("Chestplate", "Forge Chestplate",
                new List<ItemStack> { new ItemStack(items["iron"], 3) },
                new List<ItemStack> { new ItemStack(items["chestplate"], 1) },
                3f, 3, 0, 0, null, 0f, 0f));

            recipes.Add(EnsureRecipe("Potion", "Brew Potion",
                new List<ItemStack> { new ItemStack(items["leather"], 1), new ItemStack(items["wood"], 1) },
                new List<ItemStack> { new ItemStack(items["potion"], 1) },
                1.5f, 1, 10, 0,
                new List<ItemStack> { new ItemStack(items["goldCoin"], 1) },
                0.25f, 6f)); // failure chance + cooldown showcase

            return recipes;
        }

        private static RecipeDefinition EnsureRecipe(string assetName, string displayName,
            List<ItemStack> inputs, List<ItemStack> outputs, float craftTime, int levelRequirement,
            int goldCost, int xpCost, List<ItemStack> specialCosts, float failureChance, float cooldown)
        {
            string path = $"{RecipesFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
            if (existing != null) return existing;

            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            var so = new SerializedObject(recipe);
            so.FindProperty("id").stringValue = "demo_" + assetName.ToLowerInvariant();
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("craftTime").floatValue = craftTime;
            so.FindProperty("levelRequirement").intValue = levelRequirement;
            so.FindProperty("goldCost").intValue = goldCost;
            so.FindProperty("xpCost").intValue = xpCost;
            so.FindProperty("failureChance").floatValue = failureChance;
            so.FindProperty("failureChanceReductionPerSkill").floatValue = 0.005f;
            so.FindProperty("cooldownSeconds").floatValue = cooldown;
            WriteStacks(so.FindProperty("inputs"), inputs);
            WriteStacks(so.FindProperty("outputs"), outputs);
            WriteStacks(so.FindProperty("specialCosts"), specialCosts);
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(recipe, path);
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        private static void WriteStacks(SerializedProperty prop, List<ItemStack> stacks)
        {
            prop.ClearArray();
            if (stacks == null) return;
            for (int i = 0; i < stacks.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                var el = prop.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("item").objectReferenceValue = stacks[i].item;
                el.FindPropertyRelative("count").intValue = stacks[i].count;
            }
        }

        // ------------------------------------------------------------------ achievements

        public static List<AchievementDefinition> EnsureAchievements(List<RecipeDefinition> recipes)
        {
            var list = new List<AchievementDefinition>();

            list.Add(EnsureAchievement("Blacksmith", "Blacksmith", "Craft 3 items.",
                "craft.total", 3, new Color(0.70f, 0.50f, 0.90f), 9));

            var swordRecipe = recipes.Find(r => r != null && r.name.StartsWith("Sword"));
            if (swordRecipe != null)
            {
                list.Add(EnsureAchievement("SwordMaster", "Sword Master", "Forge your first sword.",
                    "craft.total." + swordRecipe.Id, 1, new Color(1f, 0.78f, 0.30f), 10));
            }
            else
            {
                Debug.LogWarning("[DJS.TiledInventoryCrafting] Sword recipe not found; 'Sword Master' achievement skipped.");
            }

            return list;
        }

        private static AchievementDefinition EnsureAchievement(string assetName, string title, string description,
            string statKey, int target, Color baseColor, int pattern)
        {
            string path = $"{AchievementsFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(path);
            if (existing != null) return existing;

            var achievement = ScriptableObject.CreateInstance<AchievementDefinition>();
            var icon = EnsureIcon(assetName + "Icon", baseColor, new Color(0.3f, 0.25f, 0.4f), pattern);
            var so = new SerializedObject(achievement);
            so.FindProperty("id").stringValue = "demo_" + assetName.ToLowerInvariant();
            so.FindProperty("title").stringValue = title;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("statKey").stringValue = statKey;
            so.FindProperty("targetValue").intValue = target;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(achievement, path);
            EditorUtility.SetDirty(achievement);
            return achievement;
        }
    }
}
