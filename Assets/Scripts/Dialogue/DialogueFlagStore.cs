using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Persistence;

namespace Game.Dialogue
{
    /// <summary>
    /// The story flags plus their save adapter (key "dialogue.flags"). Only
    /// flags that are true are stored - unset and false are the same thing.
    /// </summary>
    public class DialogueFlagStore : MonoBehaviour, IDialogueFlags, ISaveDataProvider
    {
        private readonly HashSet<string> setFlags = new();

        public string SaveKey => "dialogue.flags";
        public IReadOnlyCollection<string> SetFlags => setFlags;

        public event Action Changed;

        // Registered in Start rather than OnEnable: this can live on the
        // boot object next to SaveDataRegistry, whose Awake (setting
        // Instance) is not guaranteed to have run before our OnEnable.
        private void Start() => SaveDataRegistry.Instance?.Register(this);
        private void OnDisable() => SaveDataRegistry.Instance?.Unregister(this);

        public bool GetFlag(string name) => setFlags.Contains(name);

        public void SetFlag(string name, bool value)
        {
            bool changed = value ? setFlags.Add(name) : setFlags.Remove(name);
            if (changed)
            {
                Changed?.Invoke();
            }
        }

        public void Clear()
        {
            if (setFlags.Count == 0)
            {
                return;
            }

            setFlags.Clear();
            Changed?.Invoke();
        }

        public object CaptureState()
        {
            var state = new State { flags = new List<string>(setFlags) };
            return state;
        }

        public void RestoreState(object state)
        {
            var loaded = JsonUtility.FromJson<State>((string)state);
            setFlags.Clear();
            if (loaded.flags != null)
            {
                setFlags.UnionWith(loaded.flags);
            }
            Changed?.Invoke();
        }

        [Serializable]
        private struct State
        {
            public List<string> flags;
        }
    }
}
