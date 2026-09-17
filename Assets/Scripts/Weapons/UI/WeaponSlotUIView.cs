using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Weapons.UI
{
    public class WeaponSlotUIView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private Text keyLabel;

        public WeaponLoadoutSlot Slot { get; private set; }
        public event Action<WeaponLoadoutSlot> Clicked;

        public void Bind(WeaponLoadoutSlot slot, string keyHint)
        {
            Slot = slot;
            keyLabel.text = keyHint;
        }

        public void Refresh(IWeapon weapon, bool isActive)
        {
            bool hasWeapon = weapon != null;
            iconImage.enabled = hasWeapon;
            iconImage.sprite = hasWeapon ? weapon.Item.Icon : null;
            selectedIndicator.SetActive(isActive);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(Slot);
        }
    }
}
