using System.IO;
using TiledInventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TiledInventory.EditorTools
{
    /// <summary>
    /// Renders the demo UI to a 1920×1080 PNG without entering play mode, for store-page
    /// screenshots. Menu: Tools → Tiled Inventory → Capture Demo Preview.
    /// The generated image is saved next to the project as <c>demo_preview.png</c>.
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
                Debug.LogError("[TiledInventory] No InventoryCraftingUI found in the demo scene.");
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

            cam.Render();

            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            string path = Path.Combine(Directory.GetCurrentDirectory(), "demo_preview.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[TiledInventory] Preview saved to {path}");

            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);
            rt.Release();
            Object.DestroyImmediate(rt);

            // discard the runtime-built UI objects (canvas etc.) from the open scene
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}
