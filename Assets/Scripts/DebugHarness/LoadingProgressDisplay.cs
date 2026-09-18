using UnityEngine;
using Game.SceneFlow;

namespace Game.DebugHarness
{
    /// <summary>
    /// Lives in the Loading scene. Finds the persistent (DontDestroyOnLoad)
    /// SceneFlowController and draws whatever it reports via
    /// LoadingProgressChanged - proving the Loading scene genuinely knows
    /// nothing about what it is loading (scene-and-persistence-system.md §2),
    /// it only listens to the one event. OnGUI only; never shipped in a real
    /// build.
    /// </summary>
    public class LoadingProgressDisplay : MonoBehaviour
    {
        private float progress;

        private void OnEnable()
        {
            var flow = FindFirstObjectByType<SceneFlowController>();
            if (flow != null)
            {
                flow.LoadingProgressChanged += p => progress = p;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 80), GUI.skin.box);
            GUILayout.Label("Loading...");
            GUILayout.Label($"{progress * 100f:0}%");
            GUILayout.EndArea();
        }
    }
}
