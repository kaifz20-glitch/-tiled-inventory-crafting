using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    /// <summary>
    /// Makes the package discoverable for new users. The docs live in a
    /// `Documentation~` folder, which Unity intentionally hides from the Project
    /// window — so this adds a menu entry to open them, and a one-time Welcome
    /// window on first import that walks through the quick start.
    /// </summary>
    public static class DocumentationWindow
    {
        private const string DocsFolder = "Assets/DJS.TiledInventoryCrafting/Documentation~";
        private const string WelcomePref = "DJS.TiledInventoryCrafting.WelcomeShown.v1";

        /// <summary>Menu item + auto-show once per installed version (EditorPrefs, not SessionState).</summary>
        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(WelcomePref, false)) return;
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                EditorPrefs.SetBool(WelcomePref, true);
                ShowWelcome();
            };
        }

        [MenuItem("Tools/Tiled Inventory/Open Documentation")]
        public static void OpenDocs()
        {
            if (Directory.Exists(DocsFolder))
                EditorUtility.RevealInFinder(Path.GetFullPath(DocsFolder));
            else
                Debug.LogWarning($"[TiledInventory] Docs folder not found at {DocsFolder}");
        }

        [MenuItem("Tools/Tiled Inventory/Welcome")]
        public static void ShowWelcome()
        {
            var window = EditorWindow.GetWindow<WelcomeWindow>(true, "Tiled Inventory & Crafting System", true);
            window.minSize = new Vector2(420, 380);
            window.maxSize = new Vector2(420, 380);
            window.Show();
        }
    }

    /// <summary>Modal quick-start window shown once on first import (reopen anytime from the menu).</summary>
    public class WelcomeWindow : EditorWindow
    {
        private void OnGUI()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            var bodyStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 12 };
            var codeStyle = new GUIStyle(EditorStyles.helpBox) { richText = true, wordWrap = true };

            GUILayout.Space(8);
            GUILayout.Label("Tiled Inventory & Crafting System", titleStyle);
            GUILayout.Space(4);
            GUILayout.Label("Grid inventory, crafting, equipment, trading and persistence — everything builds from code, no prefab wiring.", bodyStyle);

            GUILayout.Space(12);
            GUILayout.Label("Quick start (under 2 minutes)", EditorStyles.boldLabel);
            GUILayout.Space(4);
            GUILayout.Label("1.  Build the demo scene", bodyStyle);
            GUILayout.Label("    Tools → Tiled Inventory → Build Demo Scene", codeStyle);
            GUILayout.Label("2.  Open the demo scene and press Play", bodyStyle);
            GUILayout.Label("    Assets/DJS.TiledInventoryCrafting/Demo/Demo.unity", codeStyle);
            GUILayout.Label("3.  Gather materials → craft a sword → equip it", bodyStyle);

            GUILayout.Space(12);
            GUILayout.Label("Documentation", EditorStyles.boldLabel);
            GUILayout.Label("Full docs ship in the package (GettingStarted, API Reference, Customization, Multiplayer...). They live in the hidden Documentation~ folder — open them with the button below.", bodyStyle);

            GUILayout.Space(16);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Demo Scene", GUILayout.Height(32)))
                {
                    DJS.TiledInventoryCrafting.EditorTools.DemoSceneBuilder.Build();
                    Close();
                }
                if (GUILayout.Button("Open Documentation", GUILayout.Height(32)))
                {
                    DocumentationWindow.OpenDocs();
                }
            }
            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Demo Scene", GUILayout.Height(28)))
                {
                    var scenePath = DJS.TiledInventoryCrafting.EditorTools.DemoContentBuilder.DemoFolder + "/Demo.unity";
                    if (File.Exists(scenePath))
                    {
                        EditorSceneManager.OpenScene(scenePath);
                        Close();
                    }
                    else
                    {
                        Debug.LogWarning("[TiledInventory] Demo scene not found — run Build Demo Scene first.");
                    }
                }
                if (GUILayout.Button("Close", GUILayout.Height(28)))
                {
                    Close();
                }
            }
        }
    }
}
