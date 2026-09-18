using UnityEngine;

namespace Game.Items.Grid
{
    /// <summary>
    /// Moves a placed item from one grid to another (or within the same
    /// grid) for drag-and-drop UI: tries the exact drop cell first, falls
    /// back to TryAddItem's auto-placement search, and silently restores the
    /// item to where it came from if neither works - see
    /// Docs/기획문서_인벤토리아이템시스템설계.md's drag&drop section ("실패시
    /// 조용히 원위치로 스냅백").
    /// </summary>
    public static class GridItemDragMover
    {
        public static void Move(IGridInventory sourceGrid, PlacedItem placedItem, IGridInventory targetGrid, Vector2Int targetOrigin)
        {
            if (!sourceGrid.RemoveItem(placedItem))
            {
                return;
            }

            var item = placedItem.Stack.Item;
            int remaining = placedItem.Stack.Quantity;

            if (targetGrid.TryPlaceAt(item, remaining, targetOrigin, out int leftoverAfterExact))
            {
                remaining = leftoverAfterExact;
            }

            if (remaining > 0)
            {
                remaining = targetGrid.TryAddItem(item, remaining);
            }

            if (remaining <= 0)
            {
                return;
            }

            // Nothing fit anywhere in the target grid - snap back to where
            // it came from, preferring its exact original cell.
            if (sourceGrid.TryPlaceAt(item, remaining, placedItem.Origin, out int leftoverBack))
            {
                remaining = leftoverBack;
            }

            if (remaining > 0)
            {
                sourceGrid.TryAddItem(item, remaining);
            }
        }
    }
}
