using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// The default spawner: the item's WorldPrefab (or the default visual)
    /// plus a WorldItem holding the stack. The factory keeps it as the last
    /// resort, so any request that no kind-specific spawner claims ends here.
    /// </summary>
    public class ItemWorldSpawner : IWorldItemSpawner
    {
        public bool CanSpawn(in WorldSpawnRequest request) => request.Item != null && request.Quantity > 0;

        public GameObject Spawn(in WorldSpawnRequest request, Vector3 position)
        {
            var item = request.Item;
            var prefab = item.WorldPrefab;

            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                go = DefaultWorldVisual.Create(item.DisplayName);
                go.transform.position = position;
            }

            if (!go.TryGetComponent(out WorldItem worldItem))
            {
                DefaultWorldVisual.EnsureCollider(go);
                worldItem = go.AddComponent<WorldItem>();
            }

            worldItem.SetStack(item, request.Quantity);
            return go;
        }
    }
}
