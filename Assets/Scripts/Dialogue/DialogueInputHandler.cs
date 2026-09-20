using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Dialogue
{
    /// <summary>
    /// Reads the dialogue keys and forwards them to DialoguePlayer, following
    /// the project's Keyboard.current polling idiom.
    /// F/Enter next line, W/S choose, Esc cancel the dialogue (talking again
    /// starts over), Tab skip ahead, L history log.
    /// </summary>
    public class DialogueInputHandler : MonoBehaviour
    {
        [SerializeField] private DialoguePlayer player;
        [SerializeField] private Key[] submitKeys = { Key.F, Key.Enter };
        [SerializeField] private Key[] upKeys = { Key.W, Key.UpArrow };
        [SerializeField] private Key[] downKeys = { Key.S, Key.DownArrow };
        [SerializeField] private Key cancelKey = Key.Escape;
        [SerializeField] private Key skipKey = Key.Tab;
        [SerializeField] private Key logKey = Key.L;

        private void Update()
        {
            var keyboard = Keyboard.current;

            // The frame a dialogue starts, the key that started it (F) is still
            // "pressed this frame" - ignore it so it cannot also advance the
            // first line. While a real window is open on top of the dialogue
            // (Esc must close that, not the dialogue) it owns the keys.
            if (keyboard == null || !player.IsPlaying || Time.frameCount == player.StartFrame || player.IsModalWindowOpen)
            {
                return;
            }

            if (keyboard[cancelKey].wasPressedThisFrame)
            {
                player.OnCancel();
                return;
            }

            if (WasPressed(keyboard, submitKeys))
            {
                player.OnSubmit();
            }

            if (WasPressed(keyboard, upKeys))
            {
                player.OnNavigate(-1);
            }
            else if (WasPressed(keyboard, downKeys))
            {
                player.OnNavigate(1);
            }

            if (keyboard[skipKey].wasPressedThisFrame)
            {
                player.OnSkip();
            }

            if (keyboard[logKey].wasPressedThisFrame)
            {
                player.OnToggleLog();
            }
        }

        private static bool WasPressed(Keyboard keyboard, Key[] keys)
        {
            foreach (var key in keys)
            {
                if (keyboard[key].wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
