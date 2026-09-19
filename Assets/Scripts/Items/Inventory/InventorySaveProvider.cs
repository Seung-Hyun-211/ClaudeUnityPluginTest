using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Persistence;

namespace Game.Items
{
    /// <summary>
    /// Saves a flat Inventory's slots (index, item id, quantity). The key is a
    /// field because a scene can hold several flat inventories (hotbar, chest);
    /// each needs its own.
    /// </summary>
    public class InventorySaveProvider : MonoBehaviour, ISaveDataProvider
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private ItemDatabase database;
        [SerializeField] private string saveKey = "inventory.flat";

        public string SaveKey => saveKey;

        private void OnEnable() => SaveDataRegistry.Instance?.Register(this);
        private void OnDisable() => SaveDataRegistry.Instance?.Unregister(this);

        public object CaptureState()
        {
            var state = new State();
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                var slot = inventory.GetSlot(i);
                if (!slot.IsEmpty)
                {
                    state.slots.Add(new SlotState { index = i, itemId = slot.Stack.Item.ItemId, quantity = slot.Stack.Quantity });
                }
            }

            return state;
        }

        public void RestoreState(object state)
        {
            var loaded = JsonUtility.FromJson<State>((string)state);

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (!inventory.GetSlot(i).IsEmpty)
                {
                    inventory.SetSlotStack(i, null);
                }
            }

            foreach (var saved in loaded.slots)
            {
                if (saved.index < 0 || saved.index >= inventory.SlotCount)
                {
                    Debug.LogWarning($"{nameof(InventorySaveProvider)}: slot {saved.index} is out of range ({inventory.SlotCount} slots) - dropped.", this);
                    continue;
                }

                if (!database.TryGet(saved.itemId, out var item))
                {
                    Debug.LogWarning($"{nameof(InventorySaveProvider)}: unknown item id '{saved.itemId}' - dropped.", this);
                    continue;
                }

                inventory.SetSlotStack(saved.index, new ItemStack(item, saved.quantity));
            }
        }

        [Serializable]
        private class SlotState
        {
            public int index;
            public string itemId;
            public int quantity;
        }

        [Serializable]
        private class State
        {
            public List<SlotState> slots = new();
        }
    }
}
