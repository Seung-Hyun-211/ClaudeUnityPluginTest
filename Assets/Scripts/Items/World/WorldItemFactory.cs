using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Puts items into the world. Every path that drops something (equipment
    /// swaps, dialogue rewards, discarding, ...) goes through here, so drop
    /// policy lives in one place. Kind-specific spawners (weapons) register
    /// themselves; the item spawner is the fallback.
    ///
    /// Place one in a scene to tune the settings; if there is none, the first
    /// use creates one with the defaults, so a drop never silently vanishes
    /// because a scene forgot to wire it.
    /// </summary>
    public class WorldItemFactory : MonoBehaviour, IWorldItemFactory
    {
        private static readonly List<IWorldItemSpawner> globalSpawners = new();
        private static WorldItemFactory instance;
        private static bool quitting;

        [SerializeField, Min(0f)] private float spacing = 0.45f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0f)] private float groundProbeHeight = 1f;
        [SerializeField, Min(0f)] private float groundProbeDistance = 5f;

        private readonly List<IWorldItemSpawner> spawners = new();
        private readonly IWorldItemSpawner fallback = new ItemWorldSpawner();

        public static WorldItemFactory Instance
        {
            get
            {
                if (instance == null && !quitting)
                {
                    instance = FindAnyObjectByType<WorldItemFactory>();
                    if (instance == null)
                    {
                        instance = new GameObject(nameof(WorldItemFactory)).AddComponent<WorldItemFactory>();
                    }
                }

                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            globalSpawners.Clear();
            instance = null;
            quitting = false;
        }

        /// <summary>
        /// For modules that add a kind of their own (Weapons): registers a
        /// spawner for every factory, existing or created later, without any
        /// scene wiring.
        /// </summary>
        public static void RegisterGlobalSpawner(IWorldItemSpawner spawner)
        {
            AddOrReplace(globalSpawners, spawner);
            instance?.Register(spawner);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("More than one WorldItemFactory in the scene - the latest one wins.", this);
            }
            instance = this;

            foreach (var spawner in globalSpawners)
            {
                Register(spawner);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnApplicationQuit() => quitting = true;

        public void Register(IWorldItemSpawner spawner) => AddOrReplace(spawners, spawner);

        public GameObject Spawn(in WorldSpawnRequest request, Vector3 position)
        {
            var spawner = FindSpawner(request);
            if (spawner == null)
            {
                Debug.LogWarning("WorldItemFactory: nothing can spawn this request (missing item or quantity) - dropped.", this);
                return null;
            }

            var go = spawner.Spawn(request, position);
            if (go != null && DropPlacement.TryFindGround(position, groundProbeHeight, groundProbeDistance, groundMask, out float groundY))
            {
                DropPlacement.RestOnGround(go, groundY);
            }

            return go;
        }

        public void SpawnAll(IEnumerable<ItemStack> stacks, Vector3 center)
        {
            var list = new List<ItemStack>();
            foreach (var stack in stacks)
            {
                if (stack != null && stack.Item != null && stack.Quantity > 0)
                {
                    list.Add(stack);
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                var offset = DropPlacement.ScatterOffset(i, list.Count, spacing);
                Spawn(WorldSpawnRequest.FromStack(list[i]), center + new Vector3(offset.x, 0f, offset.y));
            }
        }

        private IWorldItemSpawner FindSpawner(in WorldSpawnRequest request)
        {
            for (int i = spawners.Count - 1; i >= 0; i--)
            {
                if (spawners[i].CanSpawn(request))
                {
                    return spawners[i];
                }
            }

            return fallback.CanSpawn(request) ? fallback : null;
        }

        private static void AddOrReplace(List<IWorldItemSpawner> list, IWorldItemSpawner spawner)
        {
            int existing = list.FindIndex(s => s.GetType() == spawner.GetType());
            if (existing >= 0)
            {
                list.RemoveAt(existing);
            }

            list.Add(spawner);
        }
    }
}
