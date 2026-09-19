using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Items.Grid
{
    /// <summary>
    /// A spatial (footprint-based) container, distinct from the flat, one-item-
    /// per-slot IInventory used for the hotbar: pocket/rig/backpack place items
    /// at a cell origin and items can span more than one cell.
    /// </summary>
    public interface IGridInventory
    {
        GridShapeData Shape { get; }
        IReadOnlyList<PlacedItem> PlacedItems { get; }
        event Action GridChanged;

        bool IsCellFree(Vector2Int origin, Vector2Int footprint);
        bool TryPlaceAt(ItemData item, int quantity, Vector2Int origin, out int leftover, bool rotated = false);

        /// <returns>The leftover quantity that did not fit anywhere.</returns>
        int TryAddItem(ItemData item, int quantity);

        PlacedItem GetItemAt(Vector2Int cell);
        bool RemoveItem(PlacedItem placedItem);

        /// <summary>
        /// Swaps the underlying shape (e.g. equipping a different backpack).
        /// Items that no longer fit are evicted and returned so the caller can
        /// decide what happens to them (drop, move to another container, ...).
        /// </summary>
        List<ItemStack> SetShape(GridShapeData newShape);
    }
}
