using System.Collections.Generic;
using System.Linq;
using DJS.TiledInventoryCrafting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    /// <summary>
    /// Edit-mode verification of the visual recipe editor (no play mode needed): builds the
    /// graph window, creates item + recipe nodes, wires them together, and confirms the
    /// themed visuals applied (canvas, node headers, port pills, edges) plus the live
    /// preview and export path. Run via: Tools &gt; Tiled Inventory &gt; Verify Recipe Graph.
    /// </summary>
    public static class GraphEditorVerify
    {
        private static int passed;
        private static int failed;

        [MenuItem("Tools/Tiled Inventory/Verify Recipe Graph")]
        public static void Verify()
        {
            passed = 0;
            failed = 0;
            EditorSceneManager.OpenScene(DemoContentBuilder.DemoFolder + "/Demo.unity");

            var window = EditorWindow.GetWindow<RecipeGraphWindow>();
            var graph = window.GraphView;
            Check("window builds", graph != null);

            if (graph == null)
            {
                Debug.LogError("GRAPHVERIFY FAIL: window build");
                return;
            }

            Check("canvas themed", graph.style.backgroundColor.value.a > 0f);

            graph.AddItemSourceNode(new Vector2(80f, 80f));
            graph.AddItemSourceNode(new Vector2(80f, 240f));
            graph.AddRecipeNode(new Vector2(420f, 120f));

            var sources = graph.GetItemSourceNodes();
            var recipes = graph.GetRecipeNodes();
            Check("nodes created", sources.Count == 2 && recipes.Count == 1);

            var wood = Registry.FindItem("demo_wood");
            var iron = Registry.FindItem("demo_iron");
            var sword = Registry.FindItem("demo_sword");
            Check("registry items loaded", wood != null && iron != null && sword != null);

            if (sources.Count == 2 && recipes.Count == 1 && wood != null && iron != null && sword != null)
            {
                // set item + count through the node UI elements (same path as a user drag)
                sources[0].contentContainer.Q<ObjectField>().value = wood;
                sources[0].contentContainer.Q<IntegerField>().value = 5;
                sources[1].contentContainer.Q<ObjectField>().value = iron;
                sources[1].contentContainer.Q<IntegerField>().value = 2;

                var recipe = recipes[0];
                recipe.contentContainer.Q<TextField>().value = "Verify Sword";
                var outputField = recipe.contentContainer.Query<ObjectField>().First();
                if (outputField != null) outputField.value = sword;

                Check("item node themed",
                    sources[0].titleContainer.style.borderBottomWidth.value == 2f &&
                    sources[0].titleContainer.style.backgroundColor.value.a > 0f);
                Check("item accent follows rarity",
                    sources[0].titleContainer.style.borderBottomColor.value == RarityColors.Get(wood.Rarity));
                Check("recipe node themed",
                    recipe.titleContainer.style.borderBottomWidth.value == 2f &&
                    recipe.titleContainer.style.borderBottomColor.value == new RarityPalette().accent);
                Check("port pills themed",
                    recipe.InputPort.style.backgroundColor.value.a > 0f &&
                    sources[0].OutputPort.style.backgroundColor.value.a > 0f);

                // connect item sources into the recipe input. GraphView defers its change
                // callback to the next editor tick (which never comes inside a headless
                // -executeMethod), so run the same styling/sync the callback performs.
                var newEdges = new List<Edge>();
                foreach (var source in sources)
                {
                    var edge = new Edge { output = source.OutputPort, input = recipe.InputPort };
                    graph.AddElement(edge);
                    RecipeGraphTheme.StyleEdge(edge);
                    newEdges.Add(edge);
                }
                recipe.SyncInputPort();

                var edges = graph.GetEdges();
                Check("edges created", edges.Count == 2);
                Check("sources resolve", graph.GetSourcesFor(recipe).Count == 2);
                Check("edges styled", newEdges.All(e => e.edgeControl.edgeWidth == 3));
                Check("input port synced", recipe.InputPort.portName.Contains("+"));

                var def = recipe.BuildDefinition();
                Check("definition builds",
                    def != null && def.Inputs.Count == 2 && def.Outputs.Count == 1 &&
                    def.Inputs.Any(s => s.item == wood && s.count == 5) &&
                    def.Inputs.Any(s => s.item == iron && s.count == 2));
                if (def != null) Object.DestroyImmediate(def);
            }

            Check("preview populated", window.PreviewText != null && window.PreviewText.Contains("→"));

            Debug.Log($"GRAPHVERIFY: {passed} passed, {failed} failed {(failed == 0 ? "— ALL PASS" : "— FAILURES")}");
            EditorUtility.DisplayDialog("Recipe Graph Verification", $"{passed} passed, {failed} failed", "OK");
        }

        private static void Check(string name, bool condition, string detail = "")
        {
            if (condition) { passed++; }
            else { failed++; Debug.LogError($"GRAPHVERIFY FAIL: {name} {detail}"); }
        }
    }
}
