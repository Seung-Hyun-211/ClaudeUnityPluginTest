using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Items.Grid
{
    public class GridInventory : MonoBehaviour, IGridInventory
    {
        [SerializeField] private GridShapeData initialShape;

        private readonly List<PlacedItem> placedItems = new();

        public GridShapeData Shape { get; private set; }
        public IReadOnlyList<PlacedItem> PlacedItems => placedItems;
        public event Action GridChanged;

        private void Awake()
        {
            Shape = initialShape;
        }

        public bool IsCellFree(Vector2Int origin, Vector2Int footprint)
        {
            if (Shape == null)
            {
                return false;
            }

            for (int x = origin.x; x < origin.x + footprint.x; x++)
            {
                for (int y = origin.y; y < origin.y + footprint.y; y++)
                {
                    if (!Shape.IsCellUsable(x, y))
                    {
                        return false;
                    }

                    if (GetItemAt(new Vector2Int(x, y)) != null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool TryPlaceAt(ItemData item, int quantity, Vector2Int origin, out int leftover, bool rotated = false)
        {
            leftover = quantity;
            if (item == null || quantity <= 0 || !IsCellFree(origin, PlacedItem.GetFootprint(item, rotated)))
            {
                return false;
            }

            int amount = item.IsStackable ? Math.Min(quantity, item.MaxStackSize) : 1;
            placedItems.Add(new PlacedItem(new ItemStack(item, amount), origin, rotated));
            leftover = quantity - amount;

            GridChanged?.Invoke();
            return true;
        }

        public int TryAddItem(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0 || Shape == null)
            {
                return quantity;
            }

            if (item.IsStackable)
            {
                foreach (var placed in placedItems)
                {
                    if (quantity <= 0)
                    {
                        break;
                    }

                    if (placed.Stack.Item == item && !placed.Stack.IsFull)
                    {
                        quantity = placed.Stack.Add(quantity);
                    }
                }
            }

            while (quantity > 0)
            {
                bool rotated = false;
                Vector2Int? freeOrigin = FindFreeOrigin(PlacedItem.GetFootprint(item, false));
                if (freeOrigin == null)
                {
                    // Auto-placement may turn the item 90 degrees when that
                    // is the only way it fits.
                    rotated = true;
                    freeOrigin = FindFreeOrigin(PlacedItem.GetFootprint(item, true));
                }

                if (freeOrigin == null)
                {
                    break;
                }

                TryPlaceAt(item, quantity, freeOrigin.Value, out int leftover, rotated);
                quantity = leftover;
            }

            return quantity;
        }

        public PlacedItem GetItemAt(Vector2Int cell)
        {
            foreach (var placed in placedItems)
            {
                if (placed.Occupies(cell))
                {
                    return placed;
                }
            }
            return null;
        }

        public bool RemoveItem(PlacedItem placedItem)
        {
            bool removed = placedItems.Remove(placedItem);
            if (removed)
            {
                GridChanged?.Invoke();
            }
            return removed;
        }

        /// <summary>Removes every placed item without returning them - for restoring saved contents over the current ones.</summary>
        public void Clear()
        {
            if (placedItems.Count == 0)
            {
                return;
            }

            placedItems.Clear();
            GridChanged?.Invoke();
        }

        public List<ItemStack> SetShape(GridShapeData newShape)
        {
            Shape = newShape;

            var evicted = new List<ItemStack>();
            var stillFits = new List<PlacedItem>();

            foreach (var placed in placedItems)
            {
                if (newShape != null && FitsShape(placed.Origin, placed.FootprintSize))
                {
                    stillFits.Add(placed);
                }
                else
                {
                    evicted.Add(placed.Stack);
                }
            }

            placedItems.Clear();
            placedItems.AddRange(stillFits);

            GridChanged?.Invoke();
            return evicted;
        }

        private bool FitsShape(Vector2Int origin, Vector2Int footprint)
        {
            for (int x = origin.x; x < origin.x + footprint.x; x++)
            {
                for (int y = origin.y; y < origin.y + footprint.y; y++)
                {
                    if (!Shape.IsCellUsable(x, y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private Vector2Int? FindFreeOrigin(Vector2Int footprint)
        {
            for (int y = 0; y <= Shape.Height - footprint.y; y++)
            {
                for (int x = 0; x <= Shape.Width - footprint.x; x++)
                {
                    var origin = new Vector2Int(x, y);
                    if (IsCellFree(origin, footprint))
                    {
                        return origin;
                    }
                }
            }
            return null;
        }
    }
}
