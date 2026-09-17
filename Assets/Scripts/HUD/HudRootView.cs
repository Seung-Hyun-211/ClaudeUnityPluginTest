using UnityEngine;
using Game.UI.Windows;

namespace Game.HUD
{
    /// <summary>
    /// Hides every HUD widget except the hotbar (HotbarLayoutController
    /// handles its own exemption) whenever a full-screen window is open —
    /// inventory, settings, map, etc. already occupy the whole screen
    /// (documents/hud-system.md).
    /// </summary>
    public class HudRootView : MonoBehaviour
    {
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private GameObject hudRoot;

        private void OnEnable()
        {
            windowManager.FullScreenChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            windowManager.FullScreenChanged -= Refresh;
        }

        private void Refresh()
        {
            hudRoot.SetActive(windowManager.CurrentFullScreenId == null);
        }
    }
}
