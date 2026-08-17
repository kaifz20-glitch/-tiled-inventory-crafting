using System.Collections.Generic;
using DJS.TiledInventoryCrafting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    /// <summary>
    /// Builds the complete RPG demo scene from code: a camera, one GameObject hosting all
    /// systems, and the full inventory/crafting/trade UI wired up. This is what makes the
    /// "fresh import, no config" success criterion work — run
    /// <c>Tools &gt; Tiled Inventory &gt; Build Demo Scene</c> and press Play.
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string ScenePath = DemoContentBuilder.DemoFolder + "/Demo.unity";

        [MenuItem("Tools/Tiled Inventory/Build Demo Scene")]
        public static void Build()
        {
            DemoContentBuilder.EnsureFolders();
            DemoContentBuilder.EnsureAll();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            var systems = CreateSystems();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[DJS.TiledInventoryCrafting] Demo scene saved to {ScenePath}. Open it and press Play.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        private static void CreateCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.13f, 0.17f);
            camGo.transform.position = new Vector3(0f, 1f, -10f);
            camGo.AddComponent<AudioListener>();
        }

        private static GameObject CreateSystems()
        {
            var root = new GameObject("GameSystems");
            var inventorySystem = root.AddComponent<InventorySystem>();
            ConfigureGrids(inventorySystem);
            root.AddComponent<EquipmentSystem>();
            root.AddComponent<PlayerProfile>();
            var crafting = root.AddComponent<CraftingSystem>();
            root.AddComponent<CraftingStation>();
            root.AddComponent<SaveManager>();
            var achievements = root.AddComponent<AchievementTracker>();
            root.AddComponent<TradeSystem>();
            root.AddComponent<NetworkCoordinator>();
            root.AddComponent<CraftAnalytics>();
            root.AddComponent<AudioFeedback>();
            var ui = root.AddComponent<InventoryCraftingUI>();
            var demo = root.AddComponent<DemoController>();

            // resolve demo assets
            var items = LoadAll<ItemDefinition>(DemoContentBuilder.ItemsFolder);
            var recipes = LoadAll<RecipeDefinition>(DemoContentBuilder.RecipesFolder);
            var achievementDefs = LoadAll<AchievementDefinition>(DemoContentBuilder.AchievementsFolder);

            ItemDefinition FindItem(string name)
            {
                return items.Find(i => i != null && i.name == name);
            }

            // wire references explicitly (robust even if components move to other GameObjects)
            Wire(root, crafting, new Dictionary<string, object>
            {
                ["inventory"] = root.GetComponent<InventorySystem>(),
                ["profile"] = root.GetComponent<PlayerProfile>()
            });
            Wire(root, root.GetComponent<SaveManager>(), new Dictionary<string, object>
            {
                ["inventory"] = root.GetComponent<InventorySystem>(),
                ["crafting"] = crafting,
                ["profile"] = root.GetComponent<PlayerProfile>(),
                ["achievements"] = achievements
            });
            Wire(root, root.GetComponent<NetworkCoordinator>(), new Dictionary<string, object>
            {
                ["inventory"] = root.GetComponent<InventorySystem>(),
                ["crafting"] = crafting,
                ["trading"] = root.GetComponent<TradeSystem>()
            });
            Wire(root, root.GetComponent<TradeSystem>(), new Dictionary<string, object>
            {
                ["localInventory"] = root.GetComponent<InventorySystem>(),
                ["network"] = root.GetComponent<NetworkCoordinator>()
            });
            Wire(root, achievements, new Dictionary<string, object>
            {
                ["achievements"] = achievementDefs
            });
            Wire(root, ui, new Dictionary<string, object>
            {
                ["inventory"] = root.GetComponent<InventorySystem>(),
                ["equipment"] = root.GetComponent<EquipmentSystem>(),
                ["crafting"] = crafting,
                ["profile"] = root.GetComponent<PlayerProfile>(),
                ["trading"] = root.GetComponent<TradeSystem>(),
                ["recipes"] = recipes,
                ["tradeItems"] = items
            });
            Wire(root, demo, new Dictionary<string, object>
            {
                ["inventory"] = root.GetComponent<InventorySystem>(),
                ["crafting"] = crafting,
                ["equipment"] = root.GetComponent<EquipmentSystem>(),
                ["profile"] = root.GetComponent<PlayerProfile>(),
                ["saveManager"] = root.GetComponent<SaveManager>(),
                ["achievements"] = achievements,
                ["ui"] = ui,
                ["audioFeedback"] = root.GetComponent<AudioFeedback>(),
                ["wood"] = FindItem("Wood"),
                ["ironOre"] = FindItem("IronOre"),
                ["leather"] = FindItem("Leather")
            });

            return root;
        }

        /// <summary>Give the demo a roomy 9×8 bag (the 1×5 equipment grid is created by
        /// the systems at runtime), so the inventory panel reads as a proper grid instead
        /// of a handful of slots.</summary>
        private static void ConfigureGrids(InventorySystem inventorySystem)
        {
            var so = new SerializedObject(inventorySystem);
            so.FindProperty("defaultMainGridSize").vector2IntValue = new Vector2Int(9, 8);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

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

        /// <summary>Assign serialized fields by name, including list properties.</summary>
        private static void Wire(GameObject root, Object target, Dictionary<string, object> assignments)
        {
            var so = new SerializedObject(target);
            bool changed = false;
            foreach (var kv in assignments)
            {
                var prop = so.FindProperty(kv.Key);
                if (prop == null)
                {
                    Debug.LogWarning($"[DJS.TiledInventoryCrafting] No serialized field '{kv.Key}' on {target.GetType().Name}.");
                    continue;
                }
                if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
                {
                    var list = kv.Value as System.Collections.IList;
                    if (list == null) continue;
                    prop.ClearArray();
                    int i = 0;
                    foreach (var element in list)
                    {
                        prop.InsertArrayElementAtIndex(i);
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = element as Object;
                        i++;
                    }
                    changed = true;
                }
                else if (kv.Value is Object obj)
                {
                    prop.objectReferenceValue = obj;
                    changed = true;
                }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
