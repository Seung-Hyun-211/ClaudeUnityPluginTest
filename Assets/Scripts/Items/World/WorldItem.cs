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

        /// <summary>Configures a freshly instantiated world item (see WorldItemFactory).</summary>
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
                leftover = equipment.AddToContainers(item, quantity);
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

    }
}
