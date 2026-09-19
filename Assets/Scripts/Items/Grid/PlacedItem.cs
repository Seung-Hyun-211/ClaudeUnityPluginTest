using UnityEngine;

namespace Game.Items.Grid
{
    public class PlacedItem
    {
        public ItemStack Stack { get; }
        public Vector2Int Origin { get; internal set; }
        public bool IsRotated { get; }
        public Vector2Int FootprintSize => GetFootprint(Stack.Item, IsRotated);

        public PlacedItem(ItemStack stack, Vector2Int origin, bool isRotated = false)
        {
            Stack = stack;
            Origin = origin;
            IsRotated = isRotated;
        }

        /// <summary>A 90-degree rotation swaps the item's authored width and height.</summary>
        public static Vector2Int GetFootprint(ItemData item, bool rotated)
        {
            var size = item.GridSize;
            return rotated ? new Vector2Int(size.y, size.x) : size;
        }

        public bool Occupies(Vector2Int cell)
        {
            return cell.x >= Origin.x && cell.x < Origin.x + FootprintSize.x
                && cell.y >= Origin.y && cell.y < Origin.y + FootprintSize.y;
        }
    }
}
