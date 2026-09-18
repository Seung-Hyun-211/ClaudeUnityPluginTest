using UnityEngine;
using Game.Interaction;
using Game.Items.Equipment;

namespace Game.Items
{
    [RequireComponent(typeof(Collider))]
    public class WorldItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int quantity = 1;

        public ItemData Item => item;
        public int Quantity => quantity;

        public string PromptText => $"{item.DisplayName} 줍기";
        public bool CanInteract(GameObject interactor) => true;

        /// <summary>Configures a freshly instantiated world item (see WorldItemSpawner).</summary>
        public void SetStack(ItemData newItem, int newQuantity)
        {
            item = newItem;
            quantity = newQuantity;
        }

        public void Interact(GameObject interactor)
        {
            int leftover;
            if (interactor.TryGetComponent(out ContainerEquipmentController equipment))
            {
                leftover = AddToContainers(equipment, quantity);
            }
            else if (interactor.TryGetComponent(out IInventory inventory))
            {
                leftover = inventory.AddItem(item, quantity);
            }
            else
            {
                return;
            }

            if (leftover <= 0)
            {
                Destroy(gameObject);
            }
            else
            {
                quantity = leftover;
            }
        }

        /// <summary>
        /// Tries pocket, then rig, then backpack. A fixed pickup priority is a
        /// simple default; players with real container-swap needs can move
        /// items between grids afterward from the inventory screen.
        /// </summary>
        private int AddToContainers(ContainerEquipmentController equipment, int amount)
        {
            amount = TryAddToGrid(equipment.GetGrid(ContainerCategory.Pocket), amount);
            if (amount <= 0) return amount;

            amount = TryAddToGrid(equipment.GetGrid(ContainerCategory.Rig), amount);
            if (amount <= 0) return amount;

            return TryAddToGrid(equipment.GetGrid(ContainerCategory.Backpack), amount);
        }

        private int TryAddToGrid(Game.Items.Grid.IGridInventory grid, int amount)
        {
            return grid != null ? grid.TryAddItem(item, amount) : amount;
        }
    }
}
