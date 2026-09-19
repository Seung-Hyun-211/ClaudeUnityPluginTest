using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Builds the world object for one kind of item. A new kind (weapons, ...)
    /// is a new spawner registered with the factory; the factory never changes.
    /// </summary>
    public interface IWorldItemSpawner
    {
        bool CanSpawn(in WorldSpawnRequest request);

        /// <returns>The created object, or null if it could not be built.</returns>
        GameObject Spawn(in WorldSpawnRequest request, Vector3 position);
    }
}
