using System.Collections.Generic;
using UnityEngine;
using Game.Items.Grid;

namespace Game.Items.UI
{
    /// <summary>
    /// Renders one IGridInventory (pocket, rig or backpack — same view for all
    /// three) as cell backgrounds following the shape mask, plus item icons
    /// spanning their footprint.
    /// </summary>
    public class GridInventoryUIView : MonoBehaviour
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

        private void OnEnable()
        {
            grid = gridSource as IGridInventory;
            if (grid == null)
            {
                Debug.LogError($"{nameof(gridSource)} must implement {nameof(IGridInventory)}.", this);
                return;
            }

            grid.GridChanged += Refresh;
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
                view.Bind(placed);
                view.RectTransform.anchoredPosition = new Vector2(placed.Origin.x * cellSize, -placed.Origin.y * cellSize);
                view.RectTransform.sizeDelta = new Vector2(placed.FootprintSize.x * cellSize, placed.FootprintSize.y * cellSize);
                spawnedItemViews.Add(view);
            }
        }
    }
}
