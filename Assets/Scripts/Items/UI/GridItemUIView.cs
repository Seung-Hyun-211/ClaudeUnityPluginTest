using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game.Items.Grid;

namespace Game.Items.UI
{
    /// <summary>
    /// An item icon sized to its footprint and positioned at its origin cell.
    /// Also the drag source for grid drag-and-drop: dragging never mutates
    /// the grid itself. A copy of this icon follows the pointer (keeping the
    /// spot it was grabbed at), the grid under the pointer previews where it
    /// would land, and the actual move (GridItemDragMover) only happens once
    /// GridInventoryUIView.OnDrop fires.
    /// </summary>
    public class GridItemUIView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float DimmedSourceAlpha = 0.35f;

        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text quantityText;
        [SerializeField] private Key rotateKey = Key.R;

        private RectTransform dragGhost;
        private PointerEventData dragEventData;
        private Vector2 grabOffset;
        private Color originalIconColor;
        private GridInventoryUIView previewedGrid;

        public RectTransform RectTransform => rectTransform != null ? rectTransform : (RectTransform)transform;
        public PlacedItem Placed { get; private set; }
        public IGridInventory SourceGrid { get; private set; }

        /// <summary>Orientation the item will be dropped in - starts as its current one, toggled by the rotate key mid-drag.</summary>
        public bool DragRotated { get; private set; }

        /// <summary>
        /// Screen position of the dragged icon's top-left corner - the point a
        /// drop snaps to a grid cell, so the cell it lands in matches what the
        /// player sees rather than where the pointer happens to be.
        /// </summary>
        public Vector2 DragTopLeftScreen => dragEventData != null ? dragEventData.position + grabOffset : Vector2.zero;

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

        private void Update()
        {
            if (dragGhost == null || Keyboard.current == null || !Keyboard.current[rotateKey].wasPressedThisFrame)
            {
                return;
            }

            // A 90-degree turn swaps the footprint, so the ghost's on-screen
            // size swaps with it.
            DragRotated = !DragRotated;
            dragGhost.sizeDelta = new Vector2(dragGhost.sizeDelta.y, dragGhost.sizeDelta.x);
            RefreshDrag();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            DragRotated = Placed.IsRotated;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            dragEventData = eventData;

            var corners = new Vector3[4];
            RectTransform.GetWorldCorners(corners);
            grabOffset = (Vector2)corners[1] - eventData.pressPosition;

            // Copy the real icon (sprite + quantity text) rather than drawing
            // a stand-in, then strip the copy's own drag behaviour.
            var ghostGO = Instantiate(gameObject, canvas.rootCanvas.transform);
            ghostGO.name = "DragGhost";
            DestroyImmediate(ghostGO.GetComponent<GridItemUIView>());
            foreach (var graphic in ghostGO.GetComponentsInChildren<Graphic>())
            {
                graphic.raycastTarget = false;
            }
            ghostGO.AddComponent<CanvasGroup>().alpha = 0.9f;

            dragGhost = (RectTransform)ghostGO.transform;
            dragGhost.SetAsLastSibling();
            dragGhost.sizeDelta = RectTransform.rect.size;

            // Dim the original so it reads as "picked up".
            originalIconColor = iconImage.color;
            var dimmed = originalIconColor;
            dimmed.a = DimmedSourceAlpha;
            iconImage.color = dimmed;

            RefreshDrag();
        }

        public void OnDrag(PointerEventData eventData)
        {
            dragEventData = eventData;
            RefreshDrag();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DestroyGhost();
        }

        // Moves the ghost to the pointer and previews the drop on whichever
        // grid is under it.
        private void RefreshDrag()
        {
            if (dragGhost == null || dragEventData == null)
            {
                return;
            }

            dragGhost.position = DragTopLeftScreen;

            var hovered = dragEventData.pointerCurrentRaycast.gameObject != null
                ? dragEventData.pointerCurrentRaycast.gameObject.GetComponentInParent<GridInventoryUIView>()
                : null;

            if (hovered != previewedGrid && previewedGrid != null)
            {
                previewedGrid.HideDropPreview();
            }

            previewedGrid = hovered;
            if (previewedGrid != null)
            {
                previewedGrid.ShowDropPreview(this);
            }
        }

        // A successful drop rebuilds the grid views, which destroys this very
        // view mid-drag - OnEndDrag isn't guaranteed to reach a destroyed/
        // disabled source, so the ghost must also be cleaned up here or it
        // stays behind at the drop position.
        private void OnDisable() => DestroyGhost();
        private void OnDestroy() => DestroyGhost();

        private void DestroyGhost()
        {
            if (previewedGrid != null)
            {
                previewedGrid.HideDropPreview();
                previewedGrid = null;
            }

            if (dragGhost != null)
            {
                Destroy(dragGhost.gameObject);
                dragGhost = null;

                if (iconImage != null)
                {
                    iconImage.color = originalIconColor;
                }
            }

            dragEventData = null;
        }
    }
}
