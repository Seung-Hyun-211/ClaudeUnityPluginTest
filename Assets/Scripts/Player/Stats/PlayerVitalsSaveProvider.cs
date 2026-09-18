using System;
using UnityEngine;
using Game.Persistence;

namespace Game.Player
{
    /// <summary>
    /// ISaveDataProvider adapter for PlayerVitals - follows the same shape as
    /// Game.DebugHarness.DummyCounterSaveProvider (that class documents the
    /// pattern; this is the first real subsystem to use it).
    /// </summary>
    public class PlayerVitalsSaveProvider : MonoBehaviour, ISaveDataProvider
    {
        [SerializeField] private PlayerVitals vitals;

        public string SaveKey => "player.vitals";

        // This lives on the player, which is scene-local and loads in a
        // different scene than the boot-resident SaveDataRegistry, so it
        // can't hold a serialized reference to it (same reasoning as
        // PlayerRuntimeContext.Instance in PlayerController/PlayerStandIn).
        private void OnEnable() => SaveDataRegistry.Instance?.Register(this);
        private void OnDisable() => SaveDataRegistry.Instance?.Unregister(this);

        public object CaptureState() => new State
        {
            hunger = vitals.Hunger.Current,
            thirst = vitals.Thirst.Current
        };

        public void RestoreState(object state)
        {
            var loaded = JsonUtility.FromJson<State>((string)state);
            vitals.RestoreVitals(loaded.hunger, loaded.thirst);
        }

        [Serializable]
        private struct State
        {
            public float hunger;
            public float thirst;
        }
    }
}
