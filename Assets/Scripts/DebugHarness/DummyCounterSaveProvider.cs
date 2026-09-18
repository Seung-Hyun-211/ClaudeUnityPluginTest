using System;
using UnityEngine;
using Game.Persistence;

namespace Game.DebugHarness
{
    /// <summary>
    /// The first (test-only) ISaveDataProvider implementation - a single int
    /// counter - proving the whole Persistence pipeline round-trips real data
    /// end to end (register -> capture -> JsonUtility envelope -> disk ->
    /// load -> restore). See scene-and-persistence-system.md §4-4: no real
    /// subsystem (PlayerVitals, AttributeSet, ...) has an adapter yet: this is
    /// a template for what one looks like, not a production provider.
    /// </summary>
    public class DummyCounterSaveProvider : MonoBehaviour, ISaveDataProvider
    {
        [SerializeField] private SaveDataRegistry registry;
        [SerializeField] private int counter;

        public string SaveKey => "test.counter";
        public int Counter => counter;

        private void OnEnable() => registry.Register(this);
        private void OnDisable() => registry.Unregister(this);

        public void Increment() => counter++;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 500, 300, 60), GUI.skin.box);
            if (GUILayout.Button($"Increment test counter ({counter})")) Increment();
            GUILayout.EndArea();
        }

        public object CaptureState() => new State { counter = counter };

        public void RestoreState(object state)
        {
            var loaded = JsonUtility.FromJson<State>((string)state);
            counter = loaded.counter;
        }

        [Serializable]
        private struct State
        {
            public int counter;
        }
    }
}
