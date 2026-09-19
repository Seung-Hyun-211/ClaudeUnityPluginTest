using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Dialogue
{
    /// <summary>
    /// Reads the dialogue keys and forwards them to DialoguePlayer, following
    /// the project's Keyboard.current polling idiom. Cancel uses the Tap/Hold
    /// split from Docs/기획문서_대화시네마틱구조설계.md 4.2: a short tap opens the
    /// log, holding skips the sequence.
    /// </summary>
    public class DialogueInputHandler : MonoBehaviour
    {
        [SerializeField] private DialoguePlayer player;
        [SerializeField] private Key[] submitKeys = { Key.F, Key.Enter };
        [SerializeField] private Key[] upKeys = { Key.W, Key.UpArrow };
        [SerializeField] private Key[] downKeys = { Key.S, Key.DownArrow };
        [SerializeField] private Key cancelKey = Key.Escape;
        [SerializeField, Min(0f)] private float tapMaxSeconds = 0.2f;
        [SerializeField, Min(0f)] private float holdSeconds = 0.5f;

        private float cancelHeldTime;
        private bool cancelTracking;
        private bool cancelHoldFired;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !player.IsPlaying || Time.frameCount == player.StartFrame)
            {
                cancelTracking = false;
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

            HandleCancel(keyboard[cancelKey]);
        }

        private void HandleCancel(UnityEngine.InputSystem.Controls.KeyControl cancel)
        {
            if (cancel.wasPressedThisFrame)
            {
                cancelTracking = true;
                cancelHoldFired = false;
                cancelHeldTime = 0f;
            }

            if (!cancelTracking)
            {
                return;
            }

            if (cancel.isPressed)
            {
                cancelHeldTime += Time.unscaledDeltaTime;
                if (!cancelHoldFired && cancelHeldTime >= holdSeconds)
                {
                    cancelHoldFired = true;
                    player.OnCancelHold();
                }
            }

            if (cancel.wasReleasedThisFrame)
            {
                if (!cancelHoldFired && cancelHeldTime <= tapMaxSeconds)
                {
                    player.OnCancelTap();
                }
                cancelTracking = false;
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
