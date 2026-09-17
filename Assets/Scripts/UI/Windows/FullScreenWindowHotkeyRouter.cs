using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI.Windows
{
    /// <summary>
    /// Reads WindowManager's full-screen catalog and toggles whichever entry's
    /// hotkey was pressed. Registering a new kind with a Hotkey set in the
    /// catalog is enough to bind a key to it — this class never changes.
    /// </summary>
    public class FullScreenWindowHotkeyRouter : MonoBehaviour
    {
        [SerializeField] private WindowManager windowManager;

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            foreach (var entry in windowManager.FullScreenWindows)
            {
                if (entry.Hotkey != Key.None && Keyboard.current[entry.Hotkey].wasPressedThisFrame)
                {
                    windowManager.ToggleFullScreen(entry.Id);
                }
            }
        }
    }
}
