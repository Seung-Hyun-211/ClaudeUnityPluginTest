using UnityEngine;
using Game.Items.Equipment;
using Game.Player;

namespace Game.Items.UI
{
    /// <summary>
    /// Composition root for the inventory screen's placeholder layout: left side
    /// wires equipment slots + vital bars, right side is the grid views (bound
    /// per-panel in the inspector to the pocket/rig/backpack GridInventory).
    /// </summary>
    public class InventoryLayoutView : MonoBehaviour
    {
        [SerializeField] private ContainerEquipmentController equipmentController;
        [SerializeField] private EquipmentSlotUIView[] equipmentSlots;
        [SerializeField] private PlayerVitals playerVitals;
        [SerializeField] private Game.UI.StatBarUIView hungerBar;
        [SerializeField] private Game.UI.StatBarUIView thirstBar;

        private void OnEnable()
        {
            foreach (var slot in equipmentSlots)
            {
                slot.Refresh(equipmentController.GetEquipped(slot.Category));
                slot.Clicked += OnEquipmentSlotClicked;
            }

            equipmentController.EquipmentChanged += RefreshEquipmentSlot;

            hungerBar.Bind(playerVitals.Hunger);
            thirstBar.Bind(playerVitals.Thirst);
        }

        private void OnDisable()
        {
            foreach (var slot in equipmentSlots)
            {
                slot.Clicked -= OnEquipmentSlotClicked;
            }

            equipmentController.EquipmentChanged -= RefreshEquipmentSlot;
        }

        private void OnEquipmentSlotClicked(ContainerCategory category)
        {
            equipmentController.Unequip(category);
        }

        private void RefreshEquipmentSlot(ContainerCategory category)
        {
            foreach (var slot in equipmentSlots)
            {
                if (slot.Category == category)
                {
                    slot.Refresh(equipmentController.GetEquipped(category));
                }
            }
        }
    }
}
