using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UI.Windows
{
    /// <summary>
    /// Escape closes whatever WindowManager currently has on top (popup first,
    /// then full-screen) — the "Cancel" action from
    /// Docs/기획문서_UI조작설계.md, resolved as part of
    /// design-conflict-review.md #3. Kept separate from WindowManager so the
    /// key binding can change without touching stack/priority logic.
    /// </summary>
    public class WindowCloseInputHandler : MonoBehaviour
    {
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private Key closeKey = Key.Escape;

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current[closeKey].wasPressedThisFrame)
            {
                return;
            }

            if (windowManager.IsAnyWindowOpen)
            {
                windowManager.CloseTopMost();
            }
        }
    }
}
