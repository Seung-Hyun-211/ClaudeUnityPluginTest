using System;
using UnityEngine;
using Game.Persistence;

namespace Game.Combat
{
    /// <summary>
    /// ISaveDataProvider adapter for a HealthComponent - intended for the
    /// player's HealthComponent specifically (enemy/NPC health isn't
    /// individually persisted by this save system).
    /// </summary>
    public class HealthSaveProvider : MonoBehaviour, ISaveDataProvider
    {
        [SerializeField] private HealthComponent health;

        public string SaveKey => "player.health";

        // Scene-local (lives on the player), so it reaches the boot-resident
        // registry through the singleton rather than a serialized field -
        // same reasoning as PlayerRuntimeContext.Instance.
        private void OnEnable() => SaveDataRegistry.Instance?.Register(this);
        private void OnDisable() => SaveDataRegistry.Instance?.Unregister(this);

        public object CaptureState() => new State { current = health.Current };

        public void RestoreState(object state)
        {
            var loaded = JsonUtility.FromJson<State>((string)state);
            health.RestoreHealth(loaded.current);
        }

        [Serializable]
        private struct State
        {
            public float current;
        }
    }
}
