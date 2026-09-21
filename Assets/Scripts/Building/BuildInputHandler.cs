using UnityEngine;
using UnityEngine.InputSystem;
using Game.ActionMode;
using Game.UI.Windows;

namespace Game.Building
{
    /// <summary>
    /// Reads the building keys (polling, like every other input handler here):
    /// T toggles building; while building, 1/2/5 pick wall/floor/door, left
    /// click places, right click demolishes and the mouse wheel turns a door's
    /// swing. Windows and dialogues take priority: opening one ends building.
    /// Keys 3 and 4 (stairs, ladder) are reserved for stage 2.
    /// </summary>
    public class BuildInputHandler : MonoBehaviour
    {
        [SerializeField] private PlayerActionModeSwitch modeSwitch;
        [SerializeField] private BuildModeController controller;
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private Key toggleKey = Key.T;

        private void Update()
        {
            if (Keyboard.current == null || modeSwitch == null)
            {
                return;
            }

            if (windowManager != null && windowManager.IsAnyWindowOpen)
            {
                modeSwitch.Set(PlayerActionMode.Combat);
                return;
            }

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                modeSwitch.Toggle();
                return;
            }

            if (modeSwitch.Current != PlayerActionMode.Build)
            {
                return;
            }

            ReadCategoryKeys();

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                controller.TryPlace();
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                controller.TryDemolish();
            }

            if (Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f)
            {
                controller.ToggleDoorFlip();
            }
        }

        private void ReadCategoryKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard[Key.Digit1].wasPressedThisFrame)
            {
                controller.SelectCategory(BuildCategory.Wall);
            }

            if (keyboard[Key.Digit2].wasPressedThisFrame)
            {
                controller.SelectCategory(BuildCategory.Floor);
            }

            if (keyboard[Key.Digit3].wasPressedThisFrame)
            {
                controller.SelectCategory(BuildCategory.Stairs);
            }

            if (keyboard[Key.Digit4].wasPressedThisFrame)
            {
                controller.SelectCategory(BuildCategory.Ladder);
            }

            if (keyboard[Key.Digit5].wasPressedThisFrame)
            {
                controller.SelectCategory(BuildCategory.Door);
            }
        }
    }
}
