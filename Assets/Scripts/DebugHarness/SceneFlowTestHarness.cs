using UnityEngine;
using UnityEngine.SceneManagement;
using Game.SceneFlow;
using Game.Persistence;

namespace Game.DebugHarness
{
    /// <summary>
    /// Manual test harness for scene-and-persistence-system.md. Lives on the
    /// same persistent Boot GameObject as SceneFlowController/
    /// PlayerRuntimeContext/SaveDataRegistry/SaveGameService (all
    /// DontDestroyOnLoad), so this OnGUI overlay stays visible across every
    /// scene the flow loads - proving transitions actually route through
    /// Loading and that saves land only once the Loading screen is up.
    /// OnGUI only; never shipped in a real build.
    /// </summary>
    public class SceneFlowTestHarness : MonoBehaviour
    {
        [SerializeField] private SceneFlowController sceneFlow;
        [SerializeField] private SaveGameService saveGame;

        private string eventLog = "";

        private void OnEnable()
        {
            sceneFlow.TransitionStarted += k => Append($"TransitionStarted -> {k}");
            sceneFlow.LoadingScreenEntered += () => Append("LoadingScreenEntered (save flushes here if pending)");
            sceneFlow.TransitionCompleted += k => Append($"TransitionCompleted -> {k}");
        }

        private void Append(string line)
        {
            eventLog = $"{Time.time:0.00}s {line}\n{eventLog}";
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 480, 480), GUI.skin.box);
            GUILayout.Label("SceneFlow + Persistence Test Harness");
            GUILayout.Label($"Active scene: {SceneManager.GetActiveScene().name}");
            GUILayout.Label($"PlayerRuntimeContext.ActivePlayer: {(PlayerRuntimeContext.Instance.ActivePlayer != null ? PlayerRuntimeContext.Instance.ActivePlayer.name : "(none bound yet)")}");

            GUILayout.Space(8);
            GUILayout.Label("Transitions (always route through Loading):");
            if (GUILayout.Button("Go to Title")) sceneFlow.RequestTransition(SceneKind.Title);
            if (GUILayout.Button("Go to Lobby")) sceneFlow.RequestTransition(SceneKind.Lobby);
            if (GUILayout.Button("Go to Combat")) sceneFlow.RequestTransition(SceneKind.Combat);

            GUILayout.Space(8);
            GUILayout.Label("Save (see documents/scene-and-persistence-system.md §4-3):");
            if (GUILayout.Button("Request save (Custom, immediate)")) saveGame.RequestSave(SaveTriggerReason.Custom);
            if (GUILayout.Button("Request save (SceneTransition, deferred)")) saveGame.RequestSave(SaveTriggerReason.SceneTransition);
            if (GUILayout.Button("Load from disk"))
            {
                if (saveGame.TryLoadFromDisk(out var snapshot))
                {
                    saveGame.ApplyLoadedState(snapshot);
                    Append($"Loaded save.json ({snapshot.Count} entries).");
                }
                else
                {
                    Append("No save.json found yet.");
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("Event log:");
            GUILayout.Label(eventLog);
            GUILayout.EndArea();
        }
    }
}
