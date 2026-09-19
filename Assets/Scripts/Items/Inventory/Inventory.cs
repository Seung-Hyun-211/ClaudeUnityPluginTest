using System;
using UnityEngine;

namespace Game.Items
{
    public class Inventory : MonoBehaviour, IInventory
    {
        [SerializeField, Min(1)] private int slotCount = 20;

        private InventorySlot[] slots;

        public int SlotCount => slots.Length;
        public event Action InventoryChanged;

        private void Awake()
        {
            slots = new InventorySlot[slotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new InventorySlot();
            }
        }

        public InventorySlot GetSlot(int index) => slots[index];

        /// <summary>Overwrites one slot directly (null empties it) - for restoring saved contents.</summary>
        public void SetSlotStack(int index, ItemStack stack)
        {
            slots[index].Stack = stack;
            InventoryChanged?.Invoke();
        }

        public int AddItem(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return quantity;
            }

            if (item.IsStackable)
            {
                foreach (var slot in slots)
                {
                    if (quantity <= 0)
                    {
                        break;
                    }

                    if (!slot.IsEmpty && slot.Stack.Item == item && !slot.Stack.IsFull)
                    {
                        quantity = slot.Stack.Add(quantity);
                    }
                }
            }

            while (quantity > 0)
            {
                var emptySlot = FindEmptySlot();
                if (emptySlot == null)
                {
                    break;
                }

                int amountToPlace = item.IsStackable ? Math.Min(quantity, item.MaxStackSize) : 1;
                emptySlot.Stack = new ItemStack(item, amountToPlace);
                quantity -= amountToPlace;
            }

            InventoryChanged?.Invoke();
            return quantity;
        }

        public bool RemoveItem(ItemData item, int quantity)
        {
            if (!HasItem(item, quantity))
            {
                return false;
            }

            int remaining = quantity;
            foreach (var slot in slots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (slot.IsEmpty || slot.Stack.Item != item)
                {
                    continue;
                }

                remaining -= slot.Stack.Remove(remaining);
                if (slot.Stack.Quantity <= 0)
                {
                    slot.Stack = null;
                }
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public int GetQuantity(ItemData item)
        {
            int total = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.Stack.Item == item)
                {
                    total += slot.Stack.Quantity;
                }
            }
            return total;
        }

        public bool HasItem(ItemData item, int quantity) => GetQuantity(item) >= quantity;

        private InventorySlot FindEmptySlot()
        {
            foreach (var slot in slots)
            {
                if (slot.IsEmpty)
                {
                    return slot;
                }
            }
            return null;
        }
    }
}
