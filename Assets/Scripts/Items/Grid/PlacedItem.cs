using UnityEngine;

namespace Game.Items.Grid
{
    public class PlacedItem
    {
        public ItemStack Stack { get; }
        public Vector2Int Origin { get; internal set; }
        public Vector2Int FootprintSize => Stack.Item.GridSize;

        public PlacedItem(ItemStack stack, Vector2Int origin)
        {
            Stack = stack;
            Origin = origin;
        }

        public bool Occupies(Vector2Int cell)
        {
            return cell.x >= Origin.x && cell.x < Origin.x + FootprintSize.x
                && cell.y >= Origin.y && cell.y < Origin.y + FootprintSize.y;
        }
    }
}
