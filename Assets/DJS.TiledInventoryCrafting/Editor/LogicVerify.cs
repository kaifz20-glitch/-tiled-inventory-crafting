using System.Collections.Generic;
using System.Reflection;
using TiledInventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TiledInventory.EditorTools
{
    /// <summary>
    /// Edit-mode verification of the core systems (no play mode needed): grid rules,
    /// crafting queue + completion, save/load round-trip, and the code-built UI.
    /// Run via: Tools &gt; Tiled Inventory &gt; Run Logic Verification.
    /// </summary>
    public static class LogicVerify
    {
        private static int passed;
        private static int failed;

        [MenuItem("Tools/Tiled Inventory/Run Logic Verification")]
        public static void Run()
        {
            RunCore();
            Debug.Log($"LOGICVERIFY: {passed} passed, {failed} failed {(failed == 0 ? "— ALL PASS" : "— FAILURES")}");
            EditorUtility.DisplayDialog("Logic Verification", $"{passed} passed, {failed} failed", "OK");
        }

        /// <summary>Batch/CI entry point: same checks as <see cref="Run"/> but no dialog.
        /// Exits with code 0 on success, 1 on failure. Run via:
        /// <c>Unity -batchmode -quit -projectPath &lt;path&gt; -executeMethod TiledInventory.EditorTools.LogicVerify.RunHeadless</c></summary>
        public static void RunHeadless()
        {
            RunCore();
            Debug.Log($"LOGICVERIFY: {passed} passed, {failed} failed {(failed == 0 ? "— ALL PASS" : "— FAILURES")}");
            EditorApplication.Exit(failed == 0 ? 0 : 1);
        }

        private static void RunCore()
        {
            passed = 0;
            failed = 0;
            EditorSceneManager.OpenScene(DemoContentBuilder.DemoFolder + "/Demo.unity");

            TestGrid();
            TestRestrictions();
            TestCrafting();
            TestSaveRoundTrip();
            TestUiBuild();
            TestDropItem();
        }

        private static void Check(string name, bool condition, string detail = "")
        {
            if (condition) { passed++; }
            else { failed++; Debug.LogError($"LOGICVERIFY FAIL: {name} {detail}"); }
        }

        // ------------------------------------------------------------------ grid

        private static void TestGrid()
        {
            var grid = new InventoryGrid("Test", new Vector2Int(3, 2));
            Check("grid count", grid.Count == 6);

            var wood = Registry.FindItem("demo_wood");
            var iron = Registry.FindItem("demo_iron");
            Check("registry items loaded", wood != null && iron != null);

            int leftover = grid.Add(new ItemStack(wood, 5));
            Check("add first stack", leftover == 0 && grid.CountItem(wood) == 5);

            leftover = grid.Add(new ItemStack(wood, 5));
            Check("merge into existing", leftover == 0 && grid.CountItem(wood) == 10 && grid.Count == 6);

            // consume across slots
            bool consumed = grid.Consume(wood, 3);
            Check("consume partial", consumed && grid.CountItem(wood) == 7);

            // max stack overflow on a single-slot grid -> leftover
            var overflow = new InventoryGrid("Overflow", new Vector2Int(1, 1));
            overflow.Add(new ItemStack(iron, 99));
            leftover = overflow.Add(new ItemStack(iron, 2));
            Check("max stack overflow returns leftover", leftover == 2 && overflow.CountItem(iron) == 99);

            // move + merge between two explicit stacks
            var moveGrid = new InventoryGrid("Move", new Vector2Int(2, 1));
            moveGrid.GetSlot(0).stack = new ItemStack(wood, 5);
            moveGrid.GetSlot(1).stack = new ItemStack(wood, 3);
            bool moved = moveGrid.Move(0, 1, 2);
            Check("move merges stacks", moved && moveGrid.GetSlot(0).stack.count == 3 && moveGrid.GetSlot(1).stack.count == 5);
        }

        private static void TestRestrictions()
        {
            var grid = new InventoryGrid("Restricted", new Vector2Int(2, 1));
            var wood = Registry.FindItem("demo_wood");
            var sword = Registry.FindItem("demo_sword");

            grid.GetSlot(0).restriction.allowedItems.Add(sword);
            Check("restriction allows item", grid.CanPlace(0, sword, 1));
            Check("restriction blocks other item", !grid.CanPlace(0, wood, 1));
            Check("unrestricted slot accepts", grid.CanPlace(1, wood, 1));

            grid.GetSlot(1).restriction.locked = true;
            Check("locked slot blocks placement", !grid.CanPlace(1, wood, 1));

            int leftover = grid.Add(new ItemStack(wood, 5));
            Check("add skips locked slot", leftover == 5);
        }

        // ------------------------------------------------------------------ crafting

        private static void TestCrafting()
        {
            var go = new GameObject("_craft_test");
            try
            {
                var inventory = go.AddComponent<InventorySystem>();
                var profile = go.AddComponent<PlayerProfile>();
                var crafting = go.AddComponent<CraftingSystem>();

                var so = new SerializedObject(crafting);
                so.FindProperty("inventory").objectReferenceValue = inventory;
                so.FindProperty("profile").objectReferenceValue = profile;
                so.ApplyModifiedPropertiesWithoutUndo();

                var wood = Registry.FindItem("demo_wood");
                var iron = Registry.FindItem("demo_iron");
                var swordRecipe = Registry.FindRecipe("demo_sword");

                // missing materials
                Check("reject: missing materials", crafting.CanQueue(swordRecipe) == CraftRejection.MissingMaterials);

                // add materials
                inventory.AddItem(new ItemStack(wood, 5));
                inventory.AddItem(new ItemStack(iron, 2));

                // level gate (chestplate requires level 3)
                var chestplateRecipe = Registry.FindRecipe("demo_chestplate");
                profile.SetAll(1, 0, 100, 1);
                Check("reject: level too low", crafting.CanQueue(chestplateRecipe) == CraftRejection.LevelTooLow);

                // gold gate (potion recipe costs 10 gold)
                var potionRecipe = Registry.FindRecipe("demo_potion");
                profile.SetAll(3, 0, 5, 1);
                Check("reject: not enough gold", crafting.CanQueue(potionRecipe) == CraftRejection.NotEnoughGold);
                profile.SetAll(3, 0, 100, 1);

                // success
                var rejection = crafting.TryQueue(swordRecipe);
                Check("queue accepts", rejection == CraftRejection.None);
                Check("materials consumed", inventory.MainGrid.CountItem(wood) == 0 && inventory.MainGrid.CountItem(iron) == 0);
                Check("queue has one job", crafting.Queue.Count == 1);

                // complete via reflection (Update is private, play-mode only)
                var job = crafting.Queue[0];
                var finish = typeof(CraftingSystem).GetMethod("FinishJob", BindingFlags.Instance | BindingFlags.NonPublic);
                finish?.Invoke(crafting, new object[] { job });
                Check("output granted", inventory.CountItem(Registry.FindItem("demo_sword")) == 1);
                Check("cooldown applied", crafting.GetCooldownRemaining(swordRecipe) > 0f || swordRecipe.CooldownSeconds == 0f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ------------------------------------------------------------------ save/load

        private static void TestSaveRoundTrip()
        {
            var go = new GameObject("_save_test");
            try
            {
                var inventory = go.AddComponent<InventorySystem>();
                var profile = go.AddComponent<PlayerProfile>();
                var crafting = go.AddComponent<CraftingSystem>();
                var achievements = go.AddComponent<AchievementTracker>();
                var save = go.AddComponent<SaveManager>();

                Wire(save, "inventory", inventory);
                Wire(save, "crafting", crafting);
                Wire(save, "profile", profile);
                Wire(save, "achievements", achievements);
                Wire(crafting, "inventory", inventory);
                Wire(crafting, "profile", profile);

                var wood = Registry.FindItem("demo_wood");
                inventory.AddItem(new ItemStack(wood, 4));
                profile.SetAll(2, 30, 77, 3);
                achievements.AddStat("craft.total", 5);
                achievements.AddStat("craft.total." + Registry.FindRecipe("demo_sword").Id, 1);

                save.Slot = "verify_slot";
                bool saved = save.Save();
                Check("save wrote file", saved);

                // mutate everything
                inventory.MainGrid.Clear();
                profile.SetAll(1, 0, 0, 1);

                bool loaded = save.Load();
                Check("load read file", loaded);
                Check("grid restored", inventory.MainGrid.CountItem(wood) == 4);
                Check("profile restored", profile.Level == 2 && profile.Gold == 77 && profile.CraftingSkill == 3);
                Check("achievements restored", achievements.GetStat("craft.total") == 5);

                save.DeleteSave();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void InvokeAwake(Object component)
        {
            var awake = component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake?.Invoke(component, null);
        }

        private static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        // ------------------------------------------------------------------ UI build

        private static void TestUiBuild()
        {
            var go = new GameObject("_ui_test");
            try
            {
                var inventory = go.AddComponent<InventorySystem>();
                var equipment = go.AddComponent<EquipmentSystem>();
                var profile = go.AddComponent<PlayerProfile>();
                var crafting = go.AddComponent<CraftingSystem>();
                var trading = go.AddComponent<TradeSystem>();
                var ui = go.AddComponent<InventoryCraftingUI>();

                // wire the systems BuildUI needs (Awake is skipped in edit mode)
                Wire(ui, "inventory", inventory);
                Wire(ui, "equipment", equipment);
                Wire(ui, "crafting", crafting);
                Wire(ui, "profile", profile);
                Wire(ui, "trading", trading);

                var recipes = new List<RecipeDefinition>();
                foreach (var recipe in Registry.AllRecipes) recipes.Add(recipe);
                SetList(ui, "recipes", recipes);
                var items = new List<ItemDefinition>();
                foreach (var item in Registry.AllItems) items.Add(item);
                SetList(ui, "tradeItems", items);

                ui.BuildUI();

                // edit mode does not fire Awake on AddComponent — simulate play mode
                InvokeAwake(ui.Drag);
                InvokeAwake(ui.Tooltip);

                Check("canvas created", ui.Canvas != null);
                Check("tooltip created", ui.Tooltip != null && Tooltip.Instance != null);
                Check("drag service created", ui.Drag != null && DragDropService.Instance != null);
                Check("inventory view bound", ui.InventoryView != null && ui.InventoryView.Grid == inventory.MainGrid);
                Check("crafting view bound", ui.CraftingView != null);
                Check("equipment view bound", ui.EquipmentView != null);
                Check("trade view bound", ui.TradeView != null);
                Check("drop zone present", ui.Canvas.transform.Find("InventoryPanel/DropZone") != null);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void SetList(Object target, string field, List<object> values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.ClearArray();
            for (int i = 0; i < values.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = (Object)values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetList(Object target, string field, List<RecipeDefinition> values)
        {
            var boxed = new List<object>();
            foreach (var v in values) boxed.Add(v);
            SetList(target, field, boxed);
        }

        // ------------------------------------------------------------------ drop item

        private static void TestDropItem()
        {
            var go = new GameObject("_drop_test");
            Canvas canvas = null;
            try
            {
                var inventory = go.AddComponent<InventorySystem>();
                var wood = Registry.FindItem("demo_wood");
                Check("drop: registry item", wood != null);

                inventory.AddItem(new ItemStack(wood, 5));
                var grid = inventory.MainGrid;
                Check("drop: item placed", grid.CountItem(wood) == 5);

                int index = -1;
                for (int i = 0; i < grid.Count; i++)
                    if (!grid.GetSlot(i).IsEmpty) { index = i; break; }
                Check("drop: found source slot", index >= 0);

                canvas = UIFactory.CreateCanvas("_drop_canvas", 10);
                var drag = go.AddComponent<DragDropService>();
                InvokeAwake(drag);

                drag.BeginDrag(grid, index, grid.GetSlot(index).stack, (RectTransform)canvas.transform);
                Check("drop: drag started", drag.IsDragging);

                bool dropped = drag.DropItem();
                Check("drop: removes item from grid", dropped && grid.CountItem(wood) == 0);
                Check("drop: drag ended", !drag.IsDragging);
            }
            finally
            {
                if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
                Object.DestroyImmediate(go);
            }
        }

        private static void SetList(Object target, string field, List<ItemDefinition> values)
        {
            var boxed = new List<object>();
            foreach (var v in values) boxed.Add(v);
            SetList(target, field, boxed);
        }
    }
}
