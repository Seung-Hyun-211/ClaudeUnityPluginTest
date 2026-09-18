using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Items.Grid;

namespace Game.Items.UI
{
    /// <summary>
    /// An item icon sized to its footprint and positioned at its origin cell.
    /// Also the drag source for grid drag-and-drop: dragging never mutates
    /// the grid itself, it only shows a floating ghost icon - the actual move
    /// (GridItemDragMover) happens once GridInventoryUIView.OnDrop resolves
    /// a target cell.
    /// </summary>
    public class GridItemUIView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text quantityText;

        private RectTransform dragGhost;

        public RectTransform RectTransform => rectTransform != null ? rectTransform : (RectTransform)transform;
        public PlacedItem Placed { get; private set; }
        public IGridInventory SourceGrid { get; private set; }
        public event Action<PlacedItem> Clicked;

        public void Bind(PlacedItem placed, IGridInventory sourceGrid)
        {
            Placed = placed;
            SourceGrid = sourceGrid;
            iconImage.sprite = placed.Stack.Item.Icon;
            quantityText.text = placed.Stack.Quantity > 1 ? placed.Stack.Quantity.ToString() : string.Empty;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(Placed);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var ghostGO = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dragGhost = (RectTransform)ghostGO.transform;
            dragGhost.SetParent(canvas.rootCanvas.transform, worldPositionStays: false);
            dragGhost.SetAsLastSibling();
            dragGhost.sizeDelta = RectTransform.rect.size;
            dragGhost.position = eventData.position;

            var ghostImage = ghostGO.GetComponent<Image>();
            ghostImage.sprite = iconImage.sprite;
            ghostImage.raycastTarget = false;
            ghostGO.GetComponent<CanvasGroup>().alpha = 0.6f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                dragGhost.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost.gameObject);
                dragGhost = null;
            }
        }
    }
}
