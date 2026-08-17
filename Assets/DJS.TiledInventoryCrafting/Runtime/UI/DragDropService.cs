using UnityEngine;
using UnityEngine.UI;

namespace TiledInventory
{
    /// <summary>Everything needed to complete a drag: where it started and what it holds.</summary>
    public class DragData
    {
        public InventoryGrid sourceGrid;
        public int sourceIndex;
        public ItemStack stack;
    }

    /// <summary>
    /// Global drag-and-drop state for the inventory UI. Slot views start a drag,
    /// this service renders a ghost icon that follows the mouse, and drops are routed
    /// into <see cref="InventoryGrid.Move"/> (which enforces restrictions, stacking and
    /// swapping). One instance lives on the UI canvas.
    /// </summary>
    public class DragDropService : MonoBehaviour
    {
        public static DragDropService Instance { get; private set; }

        private Image ghost;
        private RectTransform canvasRoot;
        private float ghostSize = 64f;

        public DragData Current { get; private set; }
        public bool IsDragging => Current != null;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void BeginDrag(InventoryGrid sourceGrid, int sourceIndex, ItemStack stack, RectTransform canvasRoot)
        {
            if (stack.IsEmpty) return;
            Current = new DragData { sourceGrid = sourceGrid, sourceIndex = sourceIndex, stack = stack };
            this.canvasRoot = canvasRoot;
            CreateGhost();
            MoveGhost(Input.mousePosition);
        }

        private void CreateGhost()
        {
            var item = Current.stack.item;
            ghost = UIFactory.CreateImage(canvasRoot, "DragGhost", UIFactory.GetSolidSprite(Color.white),
                new Color(0.9f, 0.9f, 0.9f, 0.85f));
            var rt = (RectTransform)ghost.transform;
            rt.sizeDelta = new Vector2(ghostSize, ghostSize);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            ghost.raycastTarget = false;

            Sprite icon = item != null && item.Icon != null ? item.Icon : UIFactory.GetSolidSprite(item != null ? RarityColors.Get(item.Rarity) : Color.white);
            var iconImg = UIFactory.CreateImage(rt, "Icon", icon, Color.white);
            iconImg.rectTransform.offsetMin = new Vector2(6f, 6f);
            iconImg.rectTransform.offsetMax = new Vector2(-6f, -6f);

            var countText = UIFactory.CreateText(rt, "Count", Current.stack.count > 1 ? Current.stack.count.ToString() : "",
                20, Color.white, TextAnchor.LowerRight, FontStyle.Bold);
            countText.rectTransform.offsetMin = new Vector2(0f, 0f);
            countText.rectTransform.offsetMax = new Vector2(-4f, 4f);
            ghost.transform.SetAsLastSibling();
        }

        private void MoveGhost(Vector3 screenPos)
        {
            if (ghost == null || canvasRoot == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPos, null, out Vector2 local);
            ((RectTransform)ghost.transform).anchoredPosition = local;
        }

        /// <summary>Attempt a drop onto a slot of a grid. Returns true when the move happened.</summary>
        public bool DropOn(InventoryGrid targetGrid, int targetIndex)
        {
            if (Current == null) return false;
            var data = Current;
            EndDrag();

            if (data.sourceGrid == targetGrid && data.sourceIndex == targetIndex) return false;
            if (targetGrid == null || !targetGrid.IsValidIndex(targetIndex)) return false;

            int count = data.stack.count;
            // Hold Control while dragging to move a single item instead of the whole stack.
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) count = 1;

            return data.sourceGrid.Move(data.sourceIndex, targetIndex, count);
        }

        /// <summary>Discard the dragged stack from its source grid ("drop item").
        /// Removes the whole stack, or a single item while Control is held — the same
        /// partial-move semantics as <see cref="DropOn"/>. Returns true when items were removed.</summary>
        public bool DropItem()
        {
            if (Current == null) return false;
            var data = Current;
            EndDrag();

            int count = data.stack.count;
            // Hold Control while dragging to drop a single item instead of the whole stack.
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) count = 1;

            return data.sourceGrid.Remove(data.sourceIndex, count) > 0;
        }

        public void EndDrag()
        {
            Current = null;
            if (ghost != null)
            {
                Destroy(ghost.gameObject);
                ghost = null;
            }
        }

        private void Update()
        {
            if (IsDragging)
            {
                MoveGhost(Input.mousePosition);
                if (!Input.GetMouseButton(0)) EndDrag();
            }
        }
    }
}
