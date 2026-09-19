using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    /// <summary>The single entry point for putting items into the world.</summary>
    public interface IWorldItemFactory
    {
        /// <summary>Later registrations are asked first; registering the same spawner type again replaces the old one.</summary>
        void Register(IWorldItemSpawner spawner);

        GameObject Spawn(in WorldSpawnRequest request, Vector3 position);

        /// <summary>Spawns every stack around a centre, spread out so they do not overlap.</summary>
        void SpawnAll(IEnumerable<ItemStack> stacks, Vector3 center);
    }
}
