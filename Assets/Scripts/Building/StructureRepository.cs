using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Persistence;

namespace Game.Building
{
    /// <summary>
    /// Saves what has been built in every scene under one key. It must live
    /// on a persistent object (next to SaveGameService in the Boot scene):
    /// SaveGameService only writes the providers registered at that moment, so
    /// a provider that lived in one scene would wipe that scene structures
    /// from the file whenever the player saved from another scene
    /// (documents/building-system.md ch. 10). Scenes StructureManagers attach
    /// to it, receive their saved structures, and hand their current state
    /// back whenever it changes, when captured, and when the scene closes.
    /// A snapshot is only taken from a manager whose objects are all still
    /// alive; during teardown the last good one is kept instead.
    /// </summary>
    public class StructureRepository : MonoBehaviour, ISaveDataProvider
    {
        private readonly Dictionary<StructureManager, Action> attached = new();
        private StructureSaveData data = new();

        public static StructureRepository Instance { get; private set; }

        public string SaveKey => "building.structures";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one StructureRepository - the latest one wins.", this);
            }

            Instance = this;
        }

        // Registered in Start: it can share an object with SaveDataRegistry, whose Awake must run first.
        private void Start() => SaveDataRegistry.Instance?.Register(this);

        private void OnDestroy()
        {
            SaveDataRegistry.Instance?.Unregister(this);
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Attach(StructureManager manager)
        {
            if (attached.ContainsKey(manager))
            {
                return;
            }

            var saved = data.Find(manager.SceneId);
            if (saved != null)
            {
                manager.Rebuild(saved);
            }

            Action onChanged = () => Store(manager);
            attached[manager] = onChanged;
            manager.Changed += onChanged;
        }

        public void Detach(StructureManager manager)
        {
            if (attached.Remove(manager, out var onChanged))
            {
                manager.Changed -= onChanged;
                Store(manager);
            }
        }

        public object CaptureState()
        {
            foreach (var manager in attached.Keys)
            {
                Store(manager);
            }

            return data;
        }

        public void RestoreState(object state)
        {
            data = JsonUtility.FromJson<StructureSaveData>((string)state) ?? new StructureSaveData();

            foreach (var manager in attached.Keys)
            {
                manager.Rebuild(data.Find(manager.SceneId));
            }
        }

        private void Store(StructureManager manager)
        {
            if (manager.CanSnapshotCompletely)
            {
                data.Set(manager.SceneId, manager.Snapshot().pieces);
            }
        }
    }
}
