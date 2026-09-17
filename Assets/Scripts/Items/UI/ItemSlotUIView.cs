using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Items.UI
{
    public class ItemSlotUIView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Text quantityText;

        public int SlotIndex { get; private set; }
        public event Action<int> Clicked;

        public void Bind(int slotIndex, InventorySlot slot)
        {
            SlotIndex = slotIndex;
            Refresh(slot);
        }

        public void Refresh(InventorySlot slot)
        {
            bool hasItem = slot != null && !slot.IsEmpty;
            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? slot.Stack.Item.Icon : null;
            quantityText.text = hasItem && slot.Stack.Quantity > 1
                ? slot.Stack.Quantity.ToString()
                : string.Empty;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(SlotIndex);
        }
    }
}
