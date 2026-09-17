using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Items.Equipment;

namespace Game.Items.UI
{
    public class EquipmentSlotUIView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private ContainerCategory category;
        [SerializeField] private Image iconImage;

        public ContainerCategory Category => category;
        public event Action<ContainerCategory> Clicked;

        public void Refresh(ContainerItemData equipped)
        {
            bool hasItem = equipped != null;
            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? equipped.Icon : null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(category);
        }
    }
}
