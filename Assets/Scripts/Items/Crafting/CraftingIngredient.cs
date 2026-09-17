using System;

namespace Game.Items
{
    [Serializable]
    public class CraftingIngredient
    {
        public ItemData Item;
        public int Quantity = 1;
    }
}
