using UnityEngine;
using UnityEngine.UI;

namespace Game.Items.UI
{
    public class CraftingUIView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inventorySource;
        [SerializeField] private CraftingRecipe recipe;
        [SerializeField] private Button craftButton;

        private IInventory inventory;

        private void Awake()
        {
            inventory = inventorySource as IInventory;
            if (inventory == null)
            {
                Debug.LogError($"{nameof(inventorySource)} must implement {nameof(IInventory)}.", this);
                return;
            }

            craftButton.onClick.AddListener(OnCraftClicked);
        }

        private void OnCraftClicked()
        {
            CraftingSystem.TryCraft(inventory, recipe);
        }
    }
}
