using UnityEngine;
using UnityEngine.InputSystem;
using Game.ActionMode;
using Game.UI.Windows;

namespace Game.Building
{
    /// <summary>
    /// Reads the building keys (polling, like every other input handler here):
    /// T toggles building; while building, keys 1-5 pick the category of the
    /// same number (BuildCategory values), left click places, right click
    /// demolishes and the mouse wheel turns a door swing. Windows and dialogues
    /// take priority: opening one ends building. Categories without data
    /// (stairs and ladder in stage 1) simply cannot be selected.
    /// </summary>
    public class BuildInputHandler : MonoBehaviour
    {
        // Key i selects BuildCategory value i + 1.
        private static readonly Key[] CategoryKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

        [Tooltip("Must implement IPlayerActionMode and IPlayerActionModeSetter (PlayerActionModeSwitch).")]
        [SerializeField] private MonoBehaviour modeSource;

        [SerializeField] private BuildModeController controller;
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private Key toggleKey = Key.T;

        private IPlayerActionMode mode;
        private IPlayerActionModeSetter modeSetter;

        private void Awake()
        {
            mode = modeSource as IPlayerActionMode;
            modeSetter = modeSource as IPlayerActionModeSetter;

            if (mode == null || modeSetter == null)
            {
                Debug.LogError($"{nameof(modeSource)} must implement IPlayerActionMode and IPlayerActionModeSetter.", this);
            }
        }

        private void Update()
        {
            if (Keyboard.current == null || modeSetter == null)
            {
                return;
            }

            if (windowManager != null && windowManager.IsAnyWindowOpen)
            {
                modeSetter.Set(PlayerActionMode.Combat);
                return;
            }

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                modeSetter.Toggle();
                return;
            }

            if (mode.IsCombat())
            {
                return;
            }

            for (int i = 0; i < CategoryKeys.Length; i++)
            {
                if (Keyboard.current[CategoryKeys[i]].wasPressedThisFrame)
                {
                    controller.SelectCategory((BuildCategory)(i + 1));
                }
            }

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
    }
}
