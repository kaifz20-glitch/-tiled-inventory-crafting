using System.IO;
using DJS.TiledInventoryCrafting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DJS.TiledInventoryCrafting.EditorTools
{
    /// <summary>
    /// Renders the demo UI to 1920×1080 PNGs without entering play mode, for store-page
    /// screenshots. Menu: Tools → Tiled Inventory → Capture Demo Preview.
    /// The generated images are saved next to the project:
    ///   demo_preview.png          — main view (inventory + equipment + crafting)
    ///   screenshot_trade.png      — trade drawer open
    ///   screenshot_crafting.png   — crafting queue populated with jobs
    /// </summary>
    public static class CaptureDemoPreview
    {
        [MenuItem("Tools/Tiled Inventory/Capture Demo Preview")]
        public static void Capture()
        {
            string scenePath = DemoContentBuilder.DemoFolder + "/Demo.unity";
            EditorSceneManager.OpenScene(scenePath);

            var ui = Object.FindObjectOfType<InventoryCraftingUI>();
            var demo = Object.FindObjectOfType<DemoController>();
            if (ui == null)
            {
                Debug.LogError("[DJS.TiledInventoryCrafting] No InventoryCraftingUI found in the demo scene.");
                return;
            }

            // build the UI and pre-populate the showcase content (edit mode, no play)
            ui.BuildUI();
            demo?.PopulateShowcase();

            var canvas = ui.Canvas;
            var camGo = new GameObject("_PreviewCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.075f, 0.095f, 0.14f);
            cam.orthographic = true;
            cam.orthographicSize = 5.4f; // 1080px tall overlay at 100 ppu

            var rt = new RenderTexture(1920, 1080, 24);
            rt.name = "PreviewRT";
            cam.targetTexture = rt;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;

            // shot 1 — main view
            SaveShot(cam, rt, "demo_preview.png");

            // shot 2 — trade drawer open
            ui.ToggleTradePanel(true);
            SaveShot(cam, rt, "screenshot_trade.png");
            ui.ToggleTradePanel(false);

            // shot 3 — crafting queue populated (queue a couple of craftable recipes)
            QueueShowcaseCrafts(ui);
            SaveShot(cam, rt, "screenshot_crafting.png");

            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(rt);

            // discard the runtime-built UI objects (canvas etc.) from the open scene
            EditorSceneManager.OpenScene(scenePath);
        }

        private static void QueueShowcaseCrafts(InventoryCraftingUI ui)
        {
            var crafting = ui != null ? ui.CraftingView?.Crafting : null;
            if (crafting == null) return;

            // queue every recipe the showcase inventory can currently afford
            foreach (var recipe in Registry.AllRecipes)
            {
                if (recipe == null) continue;
                if (crafting.CanQueue(recipe) == CraftRejection.None)
                {
                    crafting.TryQueue(recipe);
                    if (crafting.Queue.Count >= 3) break; // keep the row list tidy
                }
            }
        }

        private static void SaveShot(Camera cam, RenderTexture rt, string filename)
        {
            cam.Render();

            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            string path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[DJS.TiledInventoryCrafting] Preview saved to {path}");

            Object.DestroyImmediate(tex);
        }
    }
}
