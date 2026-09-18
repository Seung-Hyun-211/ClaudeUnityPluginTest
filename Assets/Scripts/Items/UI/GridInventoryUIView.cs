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

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(cellContainer, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            var targetOrigin = new Vector2Int(
                Mathf.FloorToInt(localPoint.x / cellSize),
                Mathf.FloorToInt(-localPoint.y / cellSize));

            GridItemDragMover.Move(draggedView.SourceGrid, draggedView.Placed, grid, targetOrigin);
        }
    }
}
