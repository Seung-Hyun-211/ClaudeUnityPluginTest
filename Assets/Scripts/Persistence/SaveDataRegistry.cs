using System.Collections.Generic;
using UnityEngine;

namespace Game.Persistence
{
    /// <summary>
    /// Boot-scene resident holding the set of currently active
    /// <see cref="ISaveDataProvider"/>s. Providers register themselves on
    /// enable and unregister on disable; the registry itself has no idea
    /// what any of them actually save (see
    /// scene-and-persistence-system.md §4-1).
    /// </summary>
    public class SaveDataRegistry : MonoBehaviour
    {
        public static SaveDataRegistry Instance { get; private set; }

        private readonly List<ISaveDataProvider> providers = new();

        public IReadOnlyList<ISaveDataProvider> Providers => providers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Register(ISaveDataProvider provider)
        {
            if (provider != null && !providers.Contains(provider))
            {
                providers.Add(provider);
            }
        }

        public void Unregister(ISaveDataProvider provider) => providers.Remove(provider);
    }
}
