using UnityEngine;
using Game.Items.Equipment;

namespace Game.Items
{
    /// <summary>
    /// Puts a container into the world as a ContainerPickup carrying its
    /// contents (WorldSpawnRequest.State as ContainerContents; none = empty).
    /// Without this a dropped backpack became a plain WorldItem and everything
    /// in it was lost or scattered. Registered with the factory at startup.
    /// </summary>
    public class ContainerWorldSpawner : IWorldItemSpawner
    {
        public bool CanSpawn(in WorldSpawnRequest request) => request.Item is ContainerItemData;

        public GameObject Spawn(in WorldSpawnRequest request, Vector3 position)
        {
            var container = (ContainerItemData)request.Item;
            var contents = request.State as ContainerContents ?? ContainerContents.Empty;

            GameObject go;
            if (container.WorldPrefab != null)
            {
                go = Object.Instantiate(container.WorldPrefab, position, Quaternion.identity);
            }
            else
            {
                go = DefaultWorldVisual.Create(container.DisplayName);
                go.transform.position = position;
            }

            if (!go.TryGetComponent(out ContainerPickup pickup))
            {
                DefaultWorldVisual.EnsureCollider(go);
                pickup = go.AddComponent<ContainerPickup>();
            }

            pickup.Initialize(container, contents);
            return go;
        }
    }

    internal static class ContainerWorldSpawnerRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => WorldItemFactory.RegisterGlobalSpawner(new ContainerWorldSpawner());
    }
}
