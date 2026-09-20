using UnityEngine;
using Game.Items.Equipment;

namespace Game.Items
{
    /// <summary>Drops what taking off / swapping a container displaced, so every caller (inventory UI, pickups, harnesses) treats it the same way.</summary>
    public static class ContainerDropExtensions
    {
        /// <summary>One container with its contents inside it.</summary>
        public static void Drop(this IWorldItemFactory factory, EquippedContainer container, Vector3 position)
        {
            factory.Spawn(new WorldSpawnRequest(container.Item, 1, container.Contents), position);
        }

        /// <summary>The replaced container (with contents) plus any stacks that had no room.</summary>
        public static void Drop(this IWorldItemFactory factory, EquipResult result, Vector3 position)
        {
            if (result.Replaced.HasValue)
            {
                factory.Drop(result.Replaced.Value, position);
            }

            factory.SpawnAll(result.Overflow, position);
        }
    }
}
