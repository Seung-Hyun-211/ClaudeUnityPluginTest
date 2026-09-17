using UnityEngine;
using Game.Interaction;

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

        public void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out IInventory inventory))
            {
                return;
            }

            int leftover = inventory.AddItem(item, quantity);
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
