using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TiledInventory
{
    /// <summary>
    /// Builds the entire UI from code. This is what makes the package "ship as-is":
    /// no prefabs to wire, no fonts to import — the demo and the product UI are created
    /// at runtime. All helpers are thin wrappers over standard uGUI components.
    /// </summary>
    public static class UIFactory
    {
        private static readonly Dictionary<Color, Sprite> solidSprites = new Dictionary<Color, Sprite>();
        private static readonly Dictionary<float, Sprite> roundedFillSprites = new Dictionary<float, Sprite>();
        private static readonly Dictionary<float, Sprite> roundedTopSprites = new Dictionary<float, Sprite>();
        private static readonly Dictionary<float, Sprite> roundedBottomSprites = new Dictionary<float, Sprite>();
        private static readonly Dictionary<(float, float), Sprite> roundedFrameSprites = new Dictionary<(float, float), Sprite>();
        private static readonly Dictionary<(Color, Color), Sprite> gradientSprites = new Dictionary<(Color, Color), Sprite>();
        private static readonly Dictionary<(Color, Color, float, bool, bool), Sprite> roundedGradientSprites = new Dictionary<(Color, Color, float, bool, bool), Sprite>();

        // ------------------------------------------------------------------ scene

        /// <summary>Ensure an EventSystem exists so pointer/drag events work.</summary>
        public static EventSystem EnsureEventSystem()
        {
            var existing = EventSystem.current;
            if (existing != null) return existing;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return go.GetComponent<EventSystem>();
        }

        /// <summary>Create a screen-space overlay canvas with a scaler.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            EnsureEventSystem();
            return canvas;
        }

        // ------------------------------------------------------------------ rects

        /// <summary>Create a RectTransform with explicit anchors + offsets.</summary>
        public static RectTransform CreateRect(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>Create a RectTransform stretched to fill its parent.</summary>
        public static RectTransform CreateStretch(Transform parent, string name)
        {
            return CreateRect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        /// <summary>Anchor a rect to a point of its parent (pivot = that point, zero offsets).</summary>
        public static RectTransform CreateAnchored(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 sizeDelta)
        {
            var rt = CreateRect(parent, name, anchor, anchor, pivot, Vector2.zero, Vector2.zero);
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        // ------------------------------------------------------------------ widgets

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var rt = CreateStretch(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = GetRoundedFillSprite(10f);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var rt = CreateStretch(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string text, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, bool raycastTarget = false)
        {
            var rt = CreateStretch(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Fonts.Default;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.fontStyle = style;
            t.raycastTarget = raycastTarget;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick,
            RarityPalette palette = null, int fontSize = 22)
        {
            var rt = CreateStretch(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = GetRoundedFillSprite(6f);
            img.type = Image.Type.Sliced;
            img.color = palette != null ? palette.buttonNormal : new Color(0.23f, 0.26f, 0.32f, 1f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ApplyButtonColors(btn, palette);

            var text = CreateText(rt, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        public static void ApplyButtonColors(Button btn, RarityPalette palette)
        {
            var cb = btn.colors;
            if (palette != null)
            {
                cb.normalColor = palette.buttonNormal;
                cb.highlightedColor = palette.buttonHighlight;
                cb.pressedColor = palette.buttonPressed;
                cb.disabledColor = palette.buttonDisabled;
            }
            else
            {
                cb.normalColor = new Color(0.23f, 0.26f, 0.32f, 1f);
                cb.highlightedColor = new Color(0.30f, 0.34f, 0.42f, 1f);
                cb.pressedColor = new Color(0.16f, 0.18f, 0.22f, 1f);
                cb.disabledColor = new Color(0.18f, 0.19f, 0.22f, 0.6f);
            }
            cb.fadeDuration = 0.05f;
            btn.colors = cb;
        }

        public static InputField CreateInputField(Transform parent, string name, string placeholderText,
            int fontSize, Action<string> onValueChanged, Color backgroundColor = default(Color))
        {
            var rt = CreateStretch(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = GetRoundedFillSprite(6f);
            img.type = Image.Type.Sliced;
            img.color = backgroundColor.a == 0f ? new Color(0.08f, 0.09f, 0.12f, 1f) : backgroundColor;

            var field = rt.gameObject.AddComponent<InputField>();
            field.targetGraphic = img;

            var text = CreateText(rt, "Text", "", fontSize, Color.white, TextAnchor.MiddleLeft);
            field.textComponent = text;

            var placeholder = CreateText(rt, "Placeholder", placeholderText, fontSize,
                new Color(0.5f, 0.52f, 0.55f, 1f), TextAnchor.MiddleLeft);
            field.placeholder = placeholder;

            field.caretColor = Color.white;
            field.selectionColor = new Color(0.25f, 0.55f, 1f, 0.5f);
            field.onValueChanged.AddListener(s => onValueChanged?.Invoke(s));
            return field;
        }

        /// <summary>Scrollable vertical list. Returns the ScrollRect; content is <c>scroll.content</c>.</summary>
        public static ScrollRect CreateScrollView(Transform parent, string name, float spacing = 6f)
        {
            var rt = CreateStretch(parent, name);
            var scroll = rt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.elasticity = 0.1f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;

            var viewport = CreateStretch(rt, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            var content = CreateRect(viewport, "Content",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(0f, 0f));
            scroll.content = content;

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return scroll;
        }

        /// <summary>Progress bar: returns the fill rect; drive it with <see cref="SetProgress"/>.</summary>
        public static RectTransform CreateProgressBar(Transform parent, string name, Color fillColor, float height = 14f)
        {
            var bar = CreateRect(parent, name, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, height));
            var bg = bar.gameObject.AddComponent<Image>();
            bg.sprite = GetSolidSprite(Color.white);
            bg.color = new Color(0.05f, 0.06f, 0.08f, 1f);

            var fill = CreateRect(bar, "Fill", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = GetSolidSprite(Color.white);
            fillImg.color = fillColor;
            return fill;
        }

        public static void SetProgress(RectTransform fill, float progress)
        {
            if (fill == null) return;
            float p = Mathf.Clamp01(progress);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(p, 1f);
        }

        /// <summary>Force a preferred height on a layout child (use with VerticalLayoutGroup).</summary>
        public static LayoutElement SetHeight(RectTransform rt, float height)
        {
            var le = rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleHeight = 0f;
            return le;
        }

        // ------------------------------------------------------------------ sprites

        /// <summary>Cached 4×4 white sprite tinted per color. Used for panels/slots.</summary>
        public static Sprite GetSolidSprite(Color color)
        {
            if (solidSprites.TryGetValue(color, out var sprite) && sprite != null) return sprite;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.name = "Solid" + ColorUtility.ToHtmlStringRGBA(color);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;

            sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sprite.name = tex.name;
            solidSprites[color] = sprite;
            return sprite;
        }

        // -------------------------------------------------- rounded / gradient sprites

        /// <summary>9-sliced white rounded-rect sprite (tint via Image.color).</summary>
        public static Sprite GetRoundedFillSprite(float radius = 8f)
        {
            if (roundedFillSprites.TryGetValue(radius, out var s) && s != null) return s;
            var s2 = MakeRounded(radius, true, true, false, 0f, null);
            roundedFillSprites[radius] = s2;
            return s2;
        }

        /// <summary>9-sliced white sprite with only the top corners rounded (panel headers).</summary>
        public static Sprite GetRoundedTopSprite(float radius = 8f)
        {
            if (roundedTopSprites.TryGetValue(radius, out var s) && s != null) return s;
            var s2 = MakeRounded(radius, true, false, false, 0f, null);
            roundedTopSprites[radius] = s2;
            return s2;
        }

        /// <summary>9-sliced white sprite with only the bottom corners rounded.</summary>
        public static Sprite GetRoundedBottomSprite(float radius = 8f)
        {
            if (roundedBottomSprites.TryGetValue(radius, out var s) && s != null) return s;
            var s2 = MakeRounded(radius, false, true, false, 0f, null);
            roundedBottomSprites[radius] = s2;
            return s2;
        }

        /// <summary>9-sliced white ring sprite (transparent center, tint via Image.color).</summary>
        public static Sprite GetRoundedFrameSprite(float radius = 8f, float thickness = 2f)
        {
            if (roundedFrameSprites.TryGetValue((radius, thickness), out var s) && s != null) return s;
            var s2 = MakeRounded(radius, true, true, true, thickness, null);
            roundedFrameSprites[(radius, thickness)] = s2;
            return s2;
        }

        /// <summary>Two-row vertical gradient sprite (top color at the top).</summary>
        public static Sprite GetGradientSprite(Color top, Color bottom)
        {
            if (gradientSprites.TryGetValue((top, bottom), out var s) && s != null) return s;
            var tex = new Texture2D(2, 1, TextureFormat.RGBA32, false);
            tex.name = "Grad" + ColorUtility.ToHtmlStringRGBA(top) + ColorUtility.ToHtmlStringRGBA(bottom);
            tex.SetPixels(new[] { bottom, top }); // row 0 = bottom of the rendered sprite
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            var s2 = Sprite.Create(tex, new Rect(0, 0, 2, 1), new Vector2(0.5f, 0.5f), 100f);
            s2.name = tex.name;
            gradientSprites[(top, bottom)] = s2;
            return s2;
        }

        /// <summary>9-sliced rounded sprite with a vertical gradient baked in (corners optional).</summary>
        public static Sprite GetRoundedGradientSprite(Color top, Color bottom, float radius = 8f,
            bool roundTop = true, bool roundBottom = true)
        {
            if (roundedGradientSprites.TryGetValue((top, bottom, radius, roundTop, roundBottom), out var s) && s != null) return s;
            var s2 = MakeRounded(radius, roundTop, roundBottom, false, 0f, (texSize, y, x) =>
                Color.Lerp(bottom, top, y / (float)texSize));
            roundedGradientSprites[(top, bottom, radius, roundTop, roundBottom)] = s2;
            return s2;
        }

        private static Sprite MakeRounded(float radius, bool roundTop, bool roundBottom, bool frame, float thickness,
            Func<int, int, int, Color> colorAt)
        {
            int texSize = Mathf.CeilToInt(radius * 2f) + 6;
            float half = texSize / 2f;
            float hw = half - radius - 1f;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.name = "Rounded" + radius + (roundTop ? "T" : "") + (roundBottom ? "B" : "") + (frame ? "F" : "");
            var px = new Color[texSize * texSize];
            for (int y = 0; y < texSize; y++)
            {
                bool isTop = y >= half;
                float r = isTop ? (roundTop ? radius : 0f) : (roundBottom ? radius : 0f);
                for (int x = 0; x < texSize; x++)
                {
                    float dx = Mathf.Abs(x - half) - (hw - r);
                    float dy = Mathf.Abs(y - half) - (hw - r);
                    float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
                    float sdf = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
                    float a;
                    if (frame)
                        a = Mathf.Clamp01(0.5f - sdf) * (1f - Mathf.Clamp01(0.5f - (sdf + thickness)));
                    else
                        a = Mathf.Clamp01(0.5f - sdf);
                    Color c = colorAt != null ? colorAt(texSize, y, x) : Color.white;
                    px[y * texSize + x] = new Color(c.r, c.g, c.b, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            float b = radius + 1f;
            var sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sprite.name = tex.name;
            return sprite;
        }

        /// <summary>Simple placeholder icon: solid color with a darker border. Used when an item
        /// has no icon assigned, so the grid still reads well.</summary>
        public static Sprite CreateIconSprite(Color fill, int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Icon" + ColorUtility.ToHtmlStringRGBA(fill);
            var colors = new Color[size * size];
            Color dark = Color.Lerp(fill, Color.black, 0.45f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    bool inner = !border && ((x / 8 + y / 8) % 2 == 0);
                    colors[y * size + x] = border ? dark : inner ? fill : Color.Lerp(fill, Color.white, 0.08f);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = tex.name;
            return sprite;
        }

        // ------------------------------------------------------------------ select popup

        /// <summary>
        /// Dropdown replacement that avoids uGUI's template font pitfalls: clicking the
        /// button opens a popup list of options; clicking an option (or anywhere else)
        /// closes it. Works identically on 2022 LTS and Unity 6.
        /// </summary>
        public static SelectControl CreateSelect(Transform parent, string name, IList<string> options,
            int initialIndex, Action<int> onSelected, RarityPalette palette = null, int fontSize = 20)
        {
            var control = new SelectControl();
            var rt = CreateStretch(parent, name);
            control.Root = rt;

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = GetRoundedFillSprite(6f);
            img.type = Image.Type.Sliced;
            img.color = palette != null ? palette.buttonNormal : new Color(0.23f, 0.26f, 0.32f, 1f);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ApplyButtonColors(btn, palette);

            control.Label = CreateText(rt, "Value", "", fontSize, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal);
            var caret = CreateText(rt, "Caret", "▾", fontSize, new Color(0.7f, 0.72f, 0.75f, 1f), TextAnchor.MiddleRight);

            control.SetOptions(options, initialIndex, onSelected);
            btn.onClick.AddListener(() => control.Open(parent));
            return control;
        }

        /// <summary>State + behaviour for the select popup.</summary>
        public class SelectControl
        {
            public RectTransform Root;
            public Text Label;
            private IList<string> options = new List<string>();
            private int index;
            private Action<int> onSelected;
            private GameObject popup;

            public int Index => index;
            public string Value => index >= 0 && index < options.Count ? options[index] : "";

            public void SetOptions(IList<string> newOptions, int newIndex, Action<int> handler)
            {
                options = newOptions ?? new List<string>();
                index = Mathf.Clamp(newIndex, 0, Mathf.Max(0, options.Count - 1));
                onSelected = handler;
                UpdateLabel();
            }

            private void UpdateLabel()
            {
                if (Label != null) Label.text = Value;
            }

            public void Open(Transform canvasRoot)
            {
                Close();
                if (options.Count == 0) return;

                // full-screen clear blocker closes the popup when clicked outside
                var blocker = new GameObject("SelectBlocker", typeof(RectTransform), typeof(Image));
                var blockerRt = (RectTransform)blocker.transform;
                blockerRt.SetParent(canvasRoot, false);
                blockerRt.anchorMin = Vector2.zero;
                blockerRt.anchorMax = Vector2.one;
                blockerRt.offsetMin = Vector2.zero;
                blockerRt.offsetMax = Vector2.zero;
                blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
                var blockerBtn = blocker.AddComponent<Button>();
                blockerBtn.transition = Selectable.Transition.None;
                blockerBtn.onClick.AddListener(Close);

                // popup panel anchored to the selector's world position
                popup = new GameObject("SelectPopup", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                var popupRt = (RectTransform)popup.transform;
                popupRt.SetParent(canvasRoot, false);
                popupRt.SetAsLastSibling();
                popupRt.pivot = new Vector2(0.5f, 1f);

                // position above/below the selector using its screen rect
                var rootScreen = RectTransformUtility.WorldToScreenPoint(null, Root.position);
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvasRoot, rootScreen, null, out local);
                float openUp = options.Count * 34f > 320f ? 320f : options.Count * 34f;
                popupRt.anchoredPosition = new Vector2(local.x, local.y + openUp * 0.5f + 4f);

                var img = popup.GetComponent<Image>();
                img.sprite = GetSolidSprite(Color.white);
                img.color = new Color(0.09f, 0.10f, 0.13f, 1f);

                var vlg = popup.GetComponent<VerticalLayoutGroup>();
                vlg.spacing = 1f;
                vlg.padding = new RectOffset(2, 2, 2, 2);
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;

                var csf = popup.GetComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                popupRt.sizeDelta = new Vector2(200f, 0f);

                for (int i = 0; i < options.Count; i++)
                {
                    int capture = i;
                    var row = CreateRect(popup.transform, "Option", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    SetHeight(row, 32f);
                    var rowImg = row.gameObject.AddComponent<Image>();
                    rowImg.sprite = GetSolidSprite(Color.white);
                    rowImg.color = i == index ? new Color(0.25f, 0.42f, 0.75f, 1f) : new Color(0.14f, 0.16f, 0.20f, 1f);
                    var rowBtn = row.gameObject.AddComponent<Button>();
                    rowBtn.targetGraphic = rowImg;
                    rowBtn.colors = new ColorBlock
                    {
                        normalColor = Color.white,
                        highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f),
                        pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                        disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f),
                        colorMultiplier = 1f,
                        fadeDuration = 0.05f
                    };
                    var label = CreateText(row.transform, "Label", options[i], 20,
                        i == index ? Color.white : new Color(0.85f, 0.87f, 0.9f, 1f), TextAnchor.MiddleCenter);
                    rowBtn.onClick.AddListener(() =>
                    {
                        index = capture;
                        UpdateLabel();
                        onSelected?.Invoke(index);
                        Close();
                    });
                }
            }

            public void Close()
            {
                if (popup != null)
                {
                    UnityEngine.Object.Destroy(popup);
                    popup = null;
                }
            }
        }
    }
}
