using UnityEngine;
using UnityEngine.UI;
using Game.Weapons;

namespace Game.HUD
{
    /// <summary>
    /// Top-left weapon readout bound to WeaponLoadout.ActiveWeapon. Checks for
    /// IWeaponInfo to show ammo (firearms only); melee weapons only implement
    /// the smaller IWeaponDisplay, so the ammo readout hides itself
    /// (documents/hud-system.md).
    /// </summary>
    public class WeaponInfoUIView : MonoBehaviour
    {
        [SerializeField] private WeaponLoadout weaponLoadout;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameLabel;
        [SerializeField] private GameObject ammoRoot;
        [SerializeField] private Text ammoLabel;

        private IWeaponInfo activeWeaponInfo;

        private void OnEnable()
        {
            weaponLoadout.Changed += HandleLoadoutChanged;
            HandleLoadoutChanged();
        }

        private void OnDisable()
        {
            weaponLoadout.Changed -= HandleLoadoutChanged;
            if (activeWeaponInfo != null)
            {
                activeWeaponInfo.Changed -= Refresh;
            }
        }

        private void HandleLoadoutChanged()
        {
            if (activeWeaponInfo != null)
            {
                activeWeaponInfo.Changed -= Refresh;
            }

            activeWeaponInfo = weaponLoadout.ActiveWeapon as IWeaponInfo;
            if (activeWeaponInfo != null)
            {
                activeWeaponInfo.Changed += Refresh;
            }

            Refresh();
        }

        private void Refresh()
        {
            var display = weaponLoadout.ActiveWeapon as IWeaponDisplay;
            bool hasWeapon = display != null;
            iconImage.enabled = hasWeapon;
            iconImage.sprite = hasWeapon ? display.Icon : null;
            nameLabel.text = hasWeapon ? display.DisplayName : string.Empty;

            bool hasAmmo = activeWeaponInfo != null;
            ammoRoot.SetActive(hasAmmo);
            if (hasAmmo)
            {
                ammoLabel.text = $"{activeWeaponInfo.CurrentAmmo}/{activeWeaponInfo.MagazineSize}";
            }
        }
    }
}
