using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Instantiates ItemData.WorldPrefab back into the scene - the other half
    /// of the WorldItem bridge, used when a grid inventory evicts stacks it no
    /// longer has room for (e.g. ContainerEquipmentController.Equip/Unequip
    /// swapping a grid's shape).
    /// </summary>
    public static class WorldItemSpawner
    {
        public static void SpawnAll(IEnumerable<ItemStack> stacks, Vector3 position)
        {
            foreach (var stack in stacks)
            {
                Spawn(stack, position);
            }
        }

        public static void Spawn(ItemStack stack, Vector3 position)
        {
            if (stack == null || stack.Item == null || stack.Quantity <= 0)
            {
                return;
            }

            var prefab = stack.Item.WorldPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"{nameof(WorldItemSpawner)}: {stack.Item.DisplayName} has no WorldPrefab assigned - cannot drop it into the world.");
                return;
            }

            var instance = Object.Instantiate(prefab, position, Quaternion.identity);
            if (instance.TryGetComponent(out WorldItem worldItem))
            {
                worldItem.SetStack(stack.Item, stack.Quantity);
            }
        }
    }
}
