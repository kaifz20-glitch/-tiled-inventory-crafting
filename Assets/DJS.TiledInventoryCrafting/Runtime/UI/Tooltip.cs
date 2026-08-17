using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// Singleton tooltip panel (created by <see cref="InventoryCraftingUI"/>). Hover any
    /// slot to see item name (rarity-colored), description, stats and stack rules.
    /// Follows the mouse and stays inside the screen.
    /// </summary>
    public class Tooltip : MonoBehaviour
    {
        public static Tooltip Instance { get; private set; }

        private RectTransform panel;
        private Image background;
        private Text titleText;
        private Text bodyText;
        private RectTransform canvasRoot;
        private RarityPalette palette = new RarityPalette();
        private bool visible;

        public RarityPalette Palette
        {
            get => palette;
            set => palette = value ?? new RarityPalette();
        }

        private void Awake()
        {
            Instance = this;
            Build();
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Build()
        {
            canvasRoot = (RectTransform)transform;

            panel = UIFactory.CreateAnchored(transform, "Tooltip", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(360f, 120f));
            background = panel.gameObject.AddComponent<Image>();
            background.sprite = UIFactory.GetRoundedFillSprite(8f);
            background.type = Image.Type.Sliced;
            background.color = new Color(0.05f, 0.06f, 0.08f, 0.97f);
            background.raycastTarget = false;

            var border = UIFactory.CreateStretch(panel, "Border");
            var borderImg = border.gameObject.AddComponent<Image>();
            borderImg.sprite = UIFactory.GetRoundedFrameSprite(8f, 2f);
            borderImg.type = Image.Type.Sliced;
            borderImg.color = new Color(0.32f, 0.36f, 0.44f, 0.9f);
            borderImg.raycastTarget = false;
            border.offsetMin = new Vector2(-2f, -2f);
            border.offsetMax = new Vector2(2f, 2f);

            titleText = UIFactory.CreateText(panel, "Title", "", 22, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.offsetMin = new Vector2(12f, -34f);
            titleText.rectTransform.offsetMax = new Vector2(-12f, -8f);

            bodyText = UIFactory.CreateText(panel, "Body", "", 18, Color.white, TextAnchor.UpperLeft);
            bodyText.rectTransform.offsetMin = new Vector2(12f, 8f);
            bodyText.rectTransform.offsetMax = new Vector2(-12f, -36f);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public void Show(ItemDefinition item, string extra = null)
        {
            if (item == null) { Hide(); return; }
            var sb = new StringBuilder();
            sb.AppendLine(item.DisplayName);
            sb.AppendLine();
            if (!string.IsNullOrEmpty(item.Description)) sb.AppendLine(item.Description);
            if (item.CanEquip)
                sb.AppendLine($"Equips to: {item.EquippableSlot}");
            for (int i = 0; i < item.Stats.Count; i++)
                sb.AppendLine($"{item.Stats[i].stat}: {item.Stats[i].value}");
            sb.AppendLine($"{item.Rarity} · {item.Category} · Max stack {item.MaxStack}");
            if (extra != null) sb.AppendLine(extra);
            Show(item.DisplayName, sb.ToString().TrimEnd(), palette.Get(item.Rarity));
        }

        public void Show(string title, string body, Color titleColor)
        {
            titleText.text = title;
            titleText.color = titleColor;
            bodyText.text = body ?? "";
            visible = true;
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            PositionAt(Input.mousePosition);
        }

        public void Hide()
        {
            visible = false;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public bool IsVisible => visible;

        private void Update()
        {
            if (!visible || panel == null) return;
            PositionAt(Input.mousePosition);
        }

        private void PositionAt(Vector3 screenPos)
        {
            if (canvasRoot == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPos, null, out Vector2 local);
            Vector2 size = canvasRoot.rect.size;
            Vector2 offset = new Vector2(16f, -16f);
            Vector2 pos = local + offset;
            pos.x = Mathf.Clamp(pos.x, 8f, size.x - panel.rect.width - 8f);
            pos.y = Mathf.Clamp(pos.y, panel.rect.height + 8f, size.y - 8f);
            panel.anchoredPosition = pos;
        }
    }
}
