using System;

namespace Game.Items
{
    [Serializable]
    public class ItemStack
    {
        public ItemData Item { get; }
        public int Quantity { get; private set; }

        public ItemStack(ItemData item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public bool IsFull => Quantity >= Item.MaxStackSize;
        public int SpaceRemaining => Item.MaxStackSize - Quantity;

        /// <returns>The amount that could not be added.</returns>
        public int Add(int amount)
        {
            int addable = Math.Min(amount, SpaceRemaining);
            Quantity += addable;
            return amount - addable;
        }

        /// <returns>The amount actually removed.</returns>
        public int Remove(int amount)
        {
            int removable = Math.Min(amount, Quantity);
            Quantity -= removable;
            return removable;
        }
    }
}
