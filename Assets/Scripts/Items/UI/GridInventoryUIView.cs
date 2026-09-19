using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Items.Grid;

namespace Game.Items.UI
{
    /// <summary>
    /// Renders one IGridInventory (pocket, rig or backpack — same view for all
    /// three) as cell backgrounds following the shape mask, plus item icons
    /// spanning their footprint. Also the drop target for grid drag-and-drop:
    /// resolves the dropped screen position to a cell and hands the actual
    /// move off to GridItemDragMover.
    /// </summary>
    public class GridInventoryUIView : MonoBehaviour, IDropHandler
    {
        [SerializeField] private MonoBehaviour gridSource;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private RectTransform cellContainer;
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private GridCellUIView cellPrefab;
        [SerializeField] private GridItemUIView itemPrefab;
        [SerializeField] private float cellSize = 64f;

        private IGridInventory grid;
        private readonly List<GridItemUIView> spawnedItemViews = new();
        private bool started;
        private RectTransform dropPreview;

        private void Awake()
        {
            grid = gridSource as IGridInventory;
            if (grid == null)
            {
                Debug.LogError($"{nameof(gridSource)} must implement {nameof(IGridInventory)}.", this);
            }
        }

        private void OnEnable()
        {
            if (grid == null)
            {
                return;
            }

            grid.GridChanged += Refresh;

            // Skip the very first OnEnable's refresh: gridSource may live on
            // a different GameObject whose own Awake (e.g. GridInventory
            // setting Shape) hasn't necessarily run yet - Start() below is
            // guaranteed to fire after every Awake in the scene. A later
            // re-enable (e.g. reopening an inventory window) still needs to
            // refresh immediately since Start only ever runs once.
            if (started)
            {
                Refresh();
            }
        }

        private void Start()
        {
            started = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (grid != null)
            {
                grid.GridChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            ResizePanel();
            BuildCells();
            BuildItems();
        }

        /// <summary>
        /// Sizes this panel to its shape's bounds so a VerticalLayoutGroup can
        /// stack rig/pocket/backpack panels of different sizes in a scroll view.
        /// </summary>
        private void ResizePanel()
        {
            if (panelRoot == null)
            {
                return;
            }

            var shape = grid.Shape;
            Vector2 size = shape == null
                ? Vector2.zero
                : new Vector2(shape.Width * cellSize, shape.Height * cellSize);
            panelRoot.sizeDelta = size;
        }

        private void BuildCells()
        {
            foreach (Transform child in cellContainer)
            {
                Destroy(child.gameObject);
            }

            var shape = grid.Shape;
            if (shape == null)
            {
                return;
            }

            for (int y = 0; y < shape.Height; y++)
            {
                for (int x = 0; x < shape.Width; x++)
                {
                    if (!shape.IsCellUsable(x, y))
                    {
                        continue;
                    }

                    var cell = Instantiate(cellPrefab, cellContainer);
                    cell.RectTransform.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                    cell.RectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                }
            }
        }

        private void BuildItems()
        {
            foreach (var view in spawnedItemViews)
            {
                Destroy(view.gameObject);
            }
            spawnedItemViews.Clear();

            foreach (var placed in grid.PlacedItems)
            {
                var view = Instantiate(itemPrefab, itemContainer);
                view.Bind(placed, grid);
                view.RectTransform.anchoredPosition = new Vector2(placed.Origin.x * cellSize, -placed.Origin.y * cellSize);
                view.RectTransform.sizeDelta = new Vector2(placed.FootprintSize.x * cellSize, placed.FootprintSize.y * cellSize);
                spawnedItemViews.Add(view);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var draggedView = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<GridItemUIView>() : null;
            if (draggedView == null || draggedView.Placed == null || draggedView.SourceGrid == null)
            {
                return;
            }

            if (!TryResolveOrigin(draggedView.DragTopLeftScreen, out Vector2Int targetOrigin))
            {
                return;
            }

            HideDropPreview();
            GridItemDragMover.Move(draggedView.SourceGrid, draggedView.Placed, grid, targetOrigin, draggedView.DragRotated);
        }

        /// <summary>
        /// Outlines where the dragged item would land: green when the exact
        /// cells are free (the item's own current cells count as free), red
        /// when they aren't - a red drop still falls back to auto-placement.
        /// </summary>
        public void ShowDropPreview(GridItemUIView dragged)
        {
            if (grid == null || !TryResolveOrigin(dragged.DragTopLeftScreen, out Vector2Int origin))
            {
                HideDropPreview();
                return;
            }

            var footprint = PlacedItem.GetFootprint(dragged.Placed.Stack.Item, dragged.DragRotated);
            bool fits = FitsExactly(origin, footprint, dragged.Placed);

            if (dropPreview == null)
            {
                var go = new GameObject("DropPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                dropPreview = (RectTransform)go.transform;
                dropPreview.SetParent(itemContainer, false);
                dropPreview.anchorMin = new Vector2(0f, 1f);
                dropPreview.anchorMax = new Vector2(0f, 1f);
                dropPreview.pivot = new Vector2(0f, 1f);
                go.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            }

            dropPreview.SetAsLastSibling();
            dropPreview.gameObject.SetActive(true);
            dropPreview.anchoredPosition = new Vector2(origin.x * cellSize, -origin.y * cellSize);
            dropPreview.sizeDelta = new Vector2(footprint.x * cellSize, footprint.y * cellSize);
            dropPreview.GetComponent<UnityEngine.UI.Image>().color = fits
                ? new Color(0.3f, 1f, 0.3f, 0.4f)
                : new Color(1f, 0.3f, 0.3f, 0.4f);
        }

        public void HideDropPreview()
        {
            if (dropPreview != null)
            {
                dropPreview.gameObject.SetActive(false);
            }
        }

        // The icon's top-left corner snaps to the NEAREST cell, so the cell a
        // drop lands in is the one the icon visibly overlaps most.
        private bool TryResolveOrigin(Vector2 screenTopLeft, out Vector2Int origin)
        {
            origin = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(cellContainer, screenTopLeft, null, out Vector2 localPoint))
            {
                return false;
            }

            origin = new Vector2Int(
                Mathf.RoundToInt(localPoint.x / cellSize),
                Mathf.RoundToInt(-localPoint.y / cellSize));
            return true;
        }

        private bool FitsExactly(Vector2Int origin, Vector2Int footprint, PlacedItem ignored)
        {
            var shape = grid.Shape;
            if (shape == null)
            {
                return false;
            }

            for (int x = origin.x; x < origin.x + footprint.x; x++)
            {
                for (int y = origin.y; y < origin.y + footprint.y; y++)
                {
                    if (!shape.IsCellUsable(x, y))
                    {
                        return false;
                    }

                    var occupant = grid.GetItemAt(new Vector2Int(x, y));
                    if (occupant != null && occupant != ignored)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
