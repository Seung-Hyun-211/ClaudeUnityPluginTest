using UnityEngine;

namespace Game.Items.UI
{
    /// <summary>
    /// Renders any IInventory (main inventory, hotbar, chest, ...) as a grid of slots.
    /// Reused as-is for the quick slot bar by pointing inventorySource at a separate
    /// small Inventory component.
    /// </summary>
    public class InventoryUIView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inventorySource;
        [SerializeField] private ItemSlotUIView slotPrefab;
        [SerializeField] private Transform slotContainer;

        private IInventory inventory;
        private ItemSlotUIView[] slotViews;

        private void OnEnable()
        {
            inventory = inventorySource as IInventory;
            if (inventory == null)
            {
                Debug.LogError($"{nameof(inventorySource)} must implement {nameof(IInventory)}.", this);
                return;
            }

            BuildSlots();
            inventory.InventoryChanged += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= RefreshAll;
            }
        }

        private void BuildSlots()
        {
            slotViews = new ItemSlotUIView[inventory.SlotCount];
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var view = Instantiate(slotPrefab, slotContainer);
                view.Bind(i, inventory.GetSlot(i));
                slotViews[i] = view;
            }
        }

        private void RefreshAll()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                slotViews[i].Refresh(inventory.GetSlot(i));
            }
        }
    }
}
