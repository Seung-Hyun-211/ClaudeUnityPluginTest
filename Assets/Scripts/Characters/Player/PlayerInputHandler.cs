using UnityEngine;
using UnityEngine.InputSystem;
using Game.ActionMode;
using Game.UI.Windows;

namespace Game.Characters.Player
{
    /// <summary>
    /// Reads WASD movement and left-click attack, forwarding straight into
    /// PlayerController.OnMoveInput/OnAttackInput - same idiom as
    /// PlayerInteractionController/WindowCloseInputHandler (poll
    /// Keyboard.current/Mouse.current directly, no InputAction/PlayerInput
    /// indirection). Kept separate from PlayerController so key bindings can
    /// change without touching the composition root. Sprint/Jump are not
    /// handled here - PlayerLocomotion already drives those itself.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private WindowManager windowManager;
        [Tooltip("Optional. While building, left click places pieces instead of attacking; movement is unaffected.")]
        [SerializeField] private MonoBehaviour actionModeSource;
        [SerializeField] private Key moveUpKey = Key.W;
        [SerializeField] private Key moveDownKey = Key.S;
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;

        private IPlayerActionMode actionMode;

        private void Awake() => actionMode = actionModeSource as IPlayerActionMode;

        private void Update()
        {
            if (Keyboard.current == null || windowManager.IsAnyWindowOpen)
            {
                controller.OnMoveInput(Vector2.zero);
                return;
            }

            controller.OnMoveInput(ReadMoveInput());

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && actionMode.IsCombat())
            {
                controller.OnAttackInput();
            }
        }

        private Vector2 ReadMoveInput()
        {
            var keyboard = Keyboard.current;
            float x = (keyboard[moveRightKey].isPressed ? 1f : 0f) - (keyboard[moveLeftKey].isPressed ? 1f : 0f);
            float y = (keyboard[moveUpKey].isPressed ? 1f : 0f) - (keyboard[moveDownKey].isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
    }
}
