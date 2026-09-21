using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Puts items into the world at a position (what does not fit in the
    /// player's bags, for instance). A small interface so callers do not reach
    /// for the WorldItemFactory singleton themselves.
    /// </summary>
    public interface IItemDropper
    {
        void Drop(ItemData item, int quantity, Vector3 position);
    }

    /// <summary>Drops through WorldItemFactory.Instance; does nothing when the scene has none.</summary>
    public sealed class WorldItemDropper : IItemDropper
    {
        public void Drop(ItemData item, int quantity, Vector3 position)
        {
            if (item == null || quantity <= 0)
            {
                return;
            }

            WorldItemFactory.Instance?.SpawnAll(new[] { new ItemStack(item, quantity) }, position);
        }
    }
}
