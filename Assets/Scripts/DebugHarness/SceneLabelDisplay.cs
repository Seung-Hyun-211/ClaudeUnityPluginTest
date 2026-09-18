using UnityEngine;

namespace Game.DebugHarness
{
    /// <summary>
    /// Just prints a configurable label on screen so it is obvious which
    /// scene is currently loaded while testing SceneFlow transitions.
    /// OnGUI only; never shipped in a real build.
    /// </summary>
    public class SceneLabelDisplay : MonoBehaviour
    {
        [SerializeField] private string label = "SCENE";

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 32 };
            GUI.Label(new Rect(Screen.width / 2f - 150f, 20f, 300f, 60f), label, style);
        }
    }
}
