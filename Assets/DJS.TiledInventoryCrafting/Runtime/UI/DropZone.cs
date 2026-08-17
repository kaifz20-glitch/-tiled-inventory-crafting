using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TiledInventory
{
    /// <summary>
    /// \"Drop items\" affordance: a small strip under the inventory grid. Dragging an
    /// item (or stack) onto it removes it from its source grid entirely. Highlights
    /// while a drag hovers over it so the player knows the drop will land, and flashes
    /// when an item is actually discarded. Works with any grid (bag or equipment).
    /// </summary>
    public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private RarityPalette palette = new RarityPalette();
        private Image bg;
        private Color idleColor;
        private Color hoverColor;
        private float flashUntil;

        /// <summary>Build the drop zone into <paramref name="parent"/> and return it.</summary>
        public static DropZone Create(RectTransform parent, RarityPalette palette)
        {
            var zone = UIFactory.CreateRect(parent, "DropZone",
                new Vector2(0f, 0f), new Vector2(1f, 0.05f), new Vector2(0.5f, 0.5f),
                new Vector2(12f, 6f), new Vector2(-12f, 0f));
            var drop = zone.gameObject.AddComponent<DropZone>();
            drop.Bind(palette);
            return drop;
        }

        private void Bind(RarityPalette palette)
        {
            if (palette != null) this.palette = palette;

            bg = gameObject.AddComponent<Image>();
            bg.sprite = UIFactory.GetRoundedFillSprite(7f);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;

            idleColor = new Color(palette.danger.r, palette.danger.g, palette.danger.b, 0.10f);
            hoverColor = new Color(palette.danger.r, palette.danger.g, palette.danger.b, 0.30f);
            bg.color = idleColor;

            var icon = UIFactory.CreateText(transform, "Icon", "✕", 18, palette.textSecondary, TextAnchor.MiddleLeft, FontStyle.Bold);
            icon.rectTransform.anchorMin = new Vector2(0f, 0f);
            icon.rectTransform.anchorMax = new Vector2(0.12f, 1f);
            icon.rectTransform.offsetMin = new Vector2(14f, 0f);
            icon.rectTransform.offsetMax = new Vector2(0f, 0f);

            var label = UIFactory.CreateText(transform, "Label", "Drag here to drop item", 14,
                palette.textSecondary, TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = new Vector2(0.12f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(4f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);
        }

        private void Update()
        {
            // brief red flash after a successful drop
            if (Time.unscaledTime < flashUntil)
            {
                float k = 1f - (flashUntil - Time.unscaledTime) / 0.25f;
                bg.color = Color.Lerp(new Color(palette.danger.r, palette.danger.g, palette.danger.b, 0.55f), idleColor, k);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (DragDropService.Instance != null && DragDropService.Instance.IsDragging)
                bg.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Time.unscaledTime >= flashUntil)
                bg.color = idleColor;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var drag = DragDropService.Instance;
            if (drag != null && drag.DropItem())
                flashUntil = Time.unscaledTime + 0.25f;
            bg.color = idleColor;
        }
    }
}
