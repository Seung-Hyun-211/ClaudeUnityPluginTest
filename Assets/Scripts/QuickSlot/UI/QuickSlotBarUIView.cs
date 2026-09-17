using UnityEngine;

namespace Game.QuickSlot.UI
{
    /// <summary>
    /// Always-on HUD bar — separate from the toggled inventory screen, since
    /// number-key activation must work whether or not the inventory is open.
    /// Shows the active bank's slots and rebinds them whenever the bank swaps.
    /// Covers keys 4-9,0 only; 1/2/3 are the fixed weapon slots rendered by
    /// Game.Weapons.UI.WeaponLoadoutBarUIView instead.
    /// </summary>
    public class QuickSlotBarUIView : MonoBehaviour
    {
        private static readonly string[] KeyHints = { "4", "5", "6", "7", "8", "9", "0" };

        [SerializeField] private MonoBehaviour quickSlotControllerSource;
        [SerializeField] private QuickSlotUIView[] slotViews;

        private IQuickSlotController controller;

        private void OnEnable()
        {
            controller = quickSlotControllerSource as IQuickSlotController;
            if (controller == null)
            {
                Debug.LogError($"{nameof(quickSlotControllerSource)} must implement {nameof(IQuickSlotController)}.", this);
                return;
            }

            controller.Changed += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Changed -= RefreshAll;
            }
        }

        private void RefreshAll()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                slotViews[i].Bind(i, KeyHints[i], controller.GetSlot(i));
            }
        }
    }
}
