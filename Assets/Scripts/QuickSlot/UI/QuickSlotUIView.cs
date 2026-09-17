using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.QuickSlot.UI
{
    public class QuickSlotUIView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text keyLabel;

        public int SlotIndex { get; private set; }
        public event Action<int> Clicked;

        public void Bind(int slotIndex, string keyHint, IQuickSlottable entry)
        {
            SlotIndex = slotIndex;
            keyLabel.text = keyHint;
            Refresh(entry);
        }

        public void Refresh(IQuickSlottable entry)
        {
            bool hasEntry = entry != null;
            iconImage.enabled = hasEntry;
            iconImage.sprite = hasEntry ? entry.Icon : null;
            canvasGroup.alpha = hasEntry && !entry.IsUsable ? 0.4f : 1f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(SlotIndex);
        }
    }
}
