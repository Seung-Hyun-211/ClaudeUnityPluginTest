using System.Collections.Generic;
using UnityEngine;
using Game.Persistence;

namespace Game.Building
{
    /// <summary>
    /// Saves what has been built in every scene under one key. It must live
    /// on a persistent object (next to SaveGameService in the Boot scene):
    /// SaveGameService only writes the providers registered at that moment, so
    /// a provider that lived in one scene would wipe that scene's structures
    /// from the file whenever the player saved from another scene
    /// (documents/building-system.md ch. 10). Scenes' StructureManagers attach
    /// to it, receive their saved structures, and hand their current state
    /// back when captured or when the scene closes.
    /// </summary>
    public class StructureRepository : MonoBehaviour, ISaveDataProvider
    {
        private readonly HashSet<StructureManager> attached = new();
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
            if (!attached.Add(manager))
            {
                return;
            }

            var saved = data.Find(manager.SceneId);
            if (saved != null)
            {
                manager.Rebuild(saved);
            }
        }

        public void Detach(StructureManager manager)
        {
            if (attached.Remove(manager))
            {
                data.Set(manager.SceneId, manager.Snapshot().pieces);
            }
        }

        public object CaptureState()
        {
            foreach (var manager in attached)
            {
                data.Set(manager.SceneId, manager.Snapshot().pieces);
            }

            return data;
        }

        public void RestoreState(object state)
        {
            data = JsonUtility.FromJson<StructureSaveData>((string)state) ?? new StructureSaveData();

            foreach (var manager in attached)
            {
                manager.Rebuild(data.Find(manager.SceneId));
            }
        }
    }
}
