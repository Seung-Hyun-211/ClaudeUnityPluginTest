using System;
using System.Collections.Generic;
using System.IO;
using Game.SceneFlow;
using UnityEngine;

namespace Game.Persistence
{
    /// <summary>
    /// Single point of disk persistence (see
    /// scene-and-persistence-system.md §4-1/§4-2). Subscribes to
    /// SceneFlowController's transition events but SceneFlowController
    /// never references this class back — Persistence depends on SceneFlow,
    /// never the reverse (dependency inversion: the low-level transition
    /// mechanism does not need to know a save policy exists).
    ///
    /// Save envelope format: JsonUtility cannot serialize a polymorphic
    /// Dictionary&lt;string, object&gt; directly, so each provider's own POCO
    /// is serialized individually with JsonUtility.ToJson(object) (this
    /// resolves the concrete runtime type via reflection, so it works
    /// without SaveGameService knowing any provider's type). The resulting
    /// per-provider JSON strings are collected into a
    /// List&lt;SaveEntry&gt; (JsonUtility CAN serialize a list of one concrete
    /// type), and that list is wrapped in a single outer SaveEnvelope class
    /// which is what actually gets serialized to disk.
    /// </summary>
    public class SaveGameService : MonoBehaviour, ISaveRequestSink
    {
        private const string SaveFileName = "save.json";

        [SerializeField] private SaveDataRegistry registry;
        [SerializeField] private SceneFlowController sceneFlow;

        private bool pendingSceneTransitionSave;

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void OnEnable()
        {
            if (sceneFlow != null)
            {
                sceneFlow.LoadingScreenEntered += FlushPendingSceneTransitionSave;
            }

            Application.quitting += HandleApplicationQuitting;
        }

        private void OnDisable()
        {
            if (sceneFlow != null)
            {
                sceneFlow.LoadingScreenEntered -= FlushPendingSceneTransitionSave;
            }

            Application.quitting -= HandleApplicationQuitting;
        }

        /// <summary>
        /// The single entry point for every save trigger, regardless of
        /// source (scene transition, save point, quest, quit). Scene
        /// transitions are deferred until the Loading screen is actually on
        /// screen (see LoadingScreenEntered remarks on SceneFlowController);
        /// every other reason is assumed to fire outside of a scene
        /// transition and is written immediately.
        /// </summary>
        public void RequestSave(SaveTriggerReason reason)
        {
            if (reason == SaveTriggerReason.SceneTransition)
            {
                pendingSceneTransitionSave = true;
                return;
            }

            SaveToDisk();
        }

        private void FlushPendingSceneTransitionSave()
        {
            if (!pendingSceneTransitionSave)
            {
                return;
            }

            SaveToDisk();
            pendingSceneTransitionSave = false;
        }

        private void HandleApplicationQuitting() => RequestSave(SaveTriggerReason.AppQuit);

        private void SaveToDisk()
        {
            if (registry == null)
            {
                Debug.LogError($"{nameof(SaveGameService)} has no {nameof(SaveDataRegistry)} assigned.", this);
                return;
            }

            var envelope = new SaveEnvelope();
            foreach (var provider in registry.Providers)
            {
                envelope.entries.Add(new SaveEntry
                {
                    key = provider.SaveKey,
                    json = JsonUtility.ToJson(provider.CaptureState())
                });
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(envelope));
        }

        /// <summary>
        /// Reads the save file, if any, into a key -&gt; raw-JSON-string
        /// snapshot. Each value is the exact string previously produced by
        /// JsonUtility.ToJson(provider.CaptureState()) for that key; a
        /// provider's RestoreState is expected to deserialize it back into
        /// its own concrete state type.
        /// </summary>
        public bool TryLoadFromDisk(out Dictionary<string, object> snapshot)
        {
            if (!File.Exists(SavePath))
            {
                snapshot = null;
                return false;
            }

            var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(SavePath));
            snapshot = new Dictionary<string, object>();

            if (envelope?.entries != null)
            {
                foreach (var entry in envelope.entries)
                {
                    snapshot[entry.key] = entry.json;
                }
            }

            return true;
        }

        /// <summary>Feeds a snapshot produced by <see cref="TryLoadFromDisk"/> into every registered provider whose key is present.</summary>
        public void ApplyLoadedState(Dictionary<string, object> snapshot)
        {
            if (snapshot == null || registry == null)
            {
                return;
            }

            foreach (var provider in registry.Providers)
            {
                if (snapshot.TryGetValue(provider.SaveKey, out var state))
                {
                    provider.RestoreState(state);
                }
            }
        }

        [Serializable]
        private struct SaveEntry
        {
            public string key;
            public string json;
        }

        [Serializable]
        private class SaveEnvelope
        {
            public List<SaveEntry> entries = new();
        }
    }
}
