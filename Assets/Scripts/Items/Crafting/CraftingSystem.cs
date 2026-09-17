namespace Game.Items
{
    public static class CraftingSystem
    {
        public static bool TryCraft(IInventory inventory, CraftingRecipe recipe)
        {
            if (!recipe.CanCraft(inventory))
            {
                return false;
            }

            foreach (var ingredient in recipe.Ingredients)
            {
                inventory.RemoveItem(ingredient.Item, ingredient.Quantity);
            }

            inventory.AddItem(recipe.ResultItem, recipe.ResultQuantity);
            return true;
        }
    }
}
