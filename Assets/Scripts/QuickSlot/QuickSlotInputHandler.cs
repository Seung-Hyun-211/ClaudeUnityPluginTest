using UnityEngine;
using UnityEngine.InputSystem;
using Game.ActionMode;
using Game.UI.Windows;

namespace Game.QuickSlot
{
    /// <summary>
    /// Maps keys 4-9,0 to slot indices 0-6 and the backtick(`) key to bank
    /// swap. Keys 1/2/3 are deliberately excluded — those are permanently
    /// reserved for the weapon loadout (Game.Weapons.WeaponLoadoutInputHandler)
    /// and never swap with the bank, which is how the quickslot/weapon-slot
    /// key conflict was resolved (see documents/design-conflict-review.md #1).
    /// Kept separate from QuickSlotController so rebinding input never touches
    /// slot/bank logic. Requires the Input System package's backend to be
    /// active in Player Settings.
    /// </summary>
    public class QuickSlotInputHandler : MonoBehaviour
    {
        private static readonly Key[] SlotKeys =
        {
            Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
        };

        [SerializeField] private MonoBehaviour quickSlotControllerSource;
        [SerializeField] private GameObject user;
        [SerializeField] private WindowManager windowManager;
        [Tooltip("Optional. Keys 4-0 mean building categories while the player is building.")]
        [SerializeField] private MonoBehaviour actionModeSource;

        private IQuickSlotController controller;
        private IPlayerActionMode actionMode;

        private void Awake()
        {
            controller = quickSlotControllerSource as IQuickSlotController;
            actionMode = actionModeSource as IPlayerActionMode;
            if (controller == null)
            {
                Debug.LogError($"{nameof(quickSlotControllerSource)} must implement {nameof(IQuickSlotController)}.", this);
            }
        }

        private void Update()
        {
            // 인벤토리/설정 등 전체화면 창이나 팝업이 열려 있으면 Player 입력을
            // 죽인다(design-conflict-review.md #3 — 별도 Input Action Map 전환
            // 없이 동일한 효과를 낸다).
            if (controller == null || Keyboard.current == null || windowManager.IsAnyWindowOpen || !actionMode.IsCombat())
            {
                return;
            }

            if (Keyboard.current[Key.Backquote].wasPressedThisFrame)
            {
                controller.SwapBank();
            }

            for (int i = 0; i < SlotKeys.Length; i++)
            {
                if (Keyboard.current[SlotKeys[i]].wasPressedThisFrame)
                {
                    controller.Activate(i, new QuickSlotUseContext(user));
                }
            }
        }
    }
}
