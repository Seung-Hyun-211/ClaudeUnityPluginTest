using System;

namespace Game.Items
{
    public interface IInventory
    {
        int SlotCount { get; }
        event Action InventoryChanged;

        InventorySlot GetSlot(int index);

        /// <returns>The leftover quantity that did not fit.</returns>
        int AddItem(ItemData item, int quantity);

        bool RemoveItem(ItemData item, int quantity);
        int GetQuantity(ItemData item);
        bool HasItem(ItemData item, int quantity);
    }
}
