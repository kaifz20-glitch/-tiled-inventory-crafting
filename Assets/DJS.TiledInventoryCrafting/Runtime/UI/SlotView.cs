using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DJS.TiledInventoryCrafting
{
    /// <summary>
    /// One tiled slot. Renders the item (rarity-tinted glow, icon, count, lock overlay)
    /// and handles hover (tooltip) and drag-and-drop (move/swap/stack via
    /// <see cref="DragDropService"/>). Works for both the main grid and equipment slots —
    /// placement rules come from the grid data, not this view.
    /// </summary>
    public class SlotView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private InventoryGrid grid;
        private int slotIndex;
        private RarityPalette palette = new RarityPalette();

        private Image glow;
        private Image icon;
        private Text countText;
        private Image frame;
        private Image lockedOverlay;
        private Image emptyHint;

        public InventoryGrid Grid => grid;
        public int SlotIndex => slotIndex;

        /// <summary>Optional label shown in the empty slot (e.g. equipment slot names).</summary>
        public Text HintText { get; private set; }

        public void Bind(InventoryGrid grid, int index, RarityPalette palette, string emptyHint = null)
        {
            this.grid = grid;
            slotIndex = index;
            if (palette != null) this.palette = palette;

            var bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }
            bg.sprite = UIFactory.GetRoundedFillSprite(7f);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;

            // rounded frame: tinted by rarity when occupied, subtle outline when empty
            frame = UIFactory.CreateStretch(transform, "Frame").gameObject.AddComponent<Image>();
            frame.sprite = UIFactory.GetRoundedFrameSprite(7f, 2f);
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0f, 0f, 0f, 0f);
            frame.raycastTarget = false;

            glow = UIFactory.CreateStretch(transform, "RarityGlow").gameObject.AddComponent<Image>();
            glow.color = new Color(0f, 0f, 0f, 0f);
            glow.raycastTarget = false;

            icon = UIFactory.CreateStretch(transform, "Icon").gameObject.AddComponent<Image>();
            icon.color = new Color(0f, 0f, 0f, 0f);
            icon.raycastTarget = false;
            icon.rectTransform.offsetMin = new Vector2(6f, 6f);
            icon.rectTransform.offsetMax = new Vector2(-6f, -6f);

            countText = UIFactory.CreateText(transform, "Count", "", 20, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
            countText.rectTransform.offsetMin = new Vector2(0f, 0f);
            countText.rectTransform.offsetMax = new Vector2(-4f, 2f);

            lockedOverlay = UIFactory.CreateStretch(transform, "Locked").gameObject.AddComponent<Image>();
            lockedOverlay.color = new Color(0f, 0f, 0f, 0f);
            lockedOverlay.raycastTarget = false;

            if (!string.IsNullOrEmpty(emptyHint))
            {
                HintText = UIFactory.CreateText(transform, "Hint", emptyHint, 13,
                    new Color(0.55f, 0.58f, 0.62f, 0.9f), TextAnchor.MiddleCenter);
            }

            Refresh();
        }

        /// <summary>Re-render this slot from grid data.</summary>
        public void Refresh()
        {
            if (grid == null) return;
            var slot = grid.GetSlot(slotIndex);
            if (slot == null) return;

            bool isEmpty = slot.IsEmpty;
            var bg = GetComponent<Image>();
            bool special = slot.restriction.equipmentSlotType != EquipmentSlotType.None ||
                           slot.restriction.allowedItems.Count > 0 ||
                           slot.restriction.allowedCategories.Count > 0;
            bg.color = isEmpty ? (special ? palette.slotBackgroundSpecial : palette.slotEmpty) : palette.slotBackground;

            if (isEmpty)
            {
                icon.color = new Color(0f, 0f, 0f, 0f);
                glow.color = new Color(0f, 0f, 0f, 0f);
                countText.text = "";
                frame.color = slot.restriction.locked
                    ? new Color(0.35f, 0.37f, 0.42f, 0.15f)
                    : new Color(0.35f, 0.38f, 0.45f, 0.25f);
                lockedOverlay.color = slot.restriction.locked ? palette.lockedOverlay : new Color(0f, 0f, 0f, 0f);
                if (HintText != null) HintText.gameObject.SetActive(true);
                return;
            }

            var item = slot.stack.item;
            icon.sprite = item.Icon != null ? item.Icon : UIFactory.GetSolidSprite(RarityColors.Get(item.Rarity));
            icon.color = Color.white;
            var rar = palette.Get(item.Rarity);
            glow.color = new Color(rar.r, rar.g, rar.b, 0.22f);
            frame.color = new Color(rar.r, rar.g, rar.b, 0.55f);
            countText.text = slot.stack.count > 1 ? slot.stack.count.ToString() : "";
            lockedOverlay.color = new Color(0f, 0f, 0f, 0f);
            if (HintText != null) HintText.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ tooltip

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (grid == null) return;
            var slot = grid.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty) return;
            var item = slot.stack.item;
            string extra = slot.restriction.equipmentSlotType != EquipmentSlotType.None
                ? $"Equipped in: {slot.restriction.equipmentSlotType}"
                : null;
            Tooltip.Instance?.Show(item, extra);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.Instance?.Hide();
        }

        // ------------------------------------------------------------------ drag

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (grid == null) return;
            var slot = grid.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty) return;
            var canvasRoot = GetComponentInParent<Canvas>() != null ? (RectTransform)GetComponentInParent<Canvas>().transform : null;
            if (canvasRoot == null) return;
            DragDropService.Instance?.BeginDrag(grid, slotIndex, slot.stack, canvasRoot);
        }

        public void OnDrag(PointerEventData eventData) { /* ghost follows via DragDropService.Update */ }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragDropService.Instance?.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (grid == null) return;
            DragDropService.Instance?.DropOn(grid, slotIndex);
        }
    }
}
