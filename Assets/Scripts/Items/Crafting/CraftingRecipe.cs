using UnityEngine;

namespace Game.Items
{
    [CreateAssetMenu(menuName = "Items/Crafting Recipe", fileName = "New Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [SerializeField] private CraftingIngredient[] ingredients;
        [SerializeField] private ItemData resultItem;
        [SerializeField, Min(1)] private int resultQuantity = 1;

        public CraftingIngredient[] Ingredients => ingredients;
        public ItemData ResultItem => resultItem;
        public int ResultQuantity => resultQuantity;

        public bool CanCraft(IInventory inventory)
        {
            foreach (var ingredient in ingredients)
            {
                if (!inventory.HasItem(ingredient.Item, ingredient.Quantity))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
