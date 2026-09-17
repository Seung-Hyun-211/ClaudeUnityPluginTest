using UnityEngine;
using Game.Interaction;

namespace Game.Weapons
{
    /// <summary>
    /// A weapon lying in the world holding a LIVE instance (attached parts,
    /// loaded magazine and its remaining rounds) — unlike a generic WorldItem,
    /// which only tracks an ItemData + a count, this preserves runtime state
    /// so a dropped, half-modded rifle stays that way when picked up. Any
    /// actor with a WeaponLoadout can pick it up and use it immediately —
    /// enemies included, since Interact only looks for that component.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponPickup dropPrefab;

        private IWeapon weapon;

        public void Initialize(IWeapon weaponInstance)
        {
            weapon = weaponInstance;
        }

        public string PromptText => $"{weapon.Item.DisplayName} 장착";
        public bool CanInteract(GameObject interactor) => interactor.GetComponent<WeaponLoadout>() != null;

        public void Interact(GameObject interactor)
        {
            var loadout = interactor.GetComponent<WeaponLoadout>();
            if (loadout == null)
            {
                return;
            }

            IWeapon replaced = weapon switch
            {
                FirearmInstance firearm => loadout.EquipFirearm(firearm, ChooseFirearmSlot(loadout)),
                MeleeWeaponInstance melee => loadout.EquipMelee(melee),
                _ => null
            };

            if (replaced != null && dropPrefab != null)
            {
                var dropped = Instantiate(dropPrefab, transform.position, transform.rotation);
                dropped.Initialize(replaced);
            }

            Destroy(gameObject);
        }

        private static WeaponLoadoutSlot ChooseFirearmSlot(WeaponLoadout loadout)
        {
            if (loadout.Primary == null)
            {
                return WeaponLoadoutSlot.Primary;
            }

            return loadout.Secondary == null ? WeaponLoadoutSlot.Secondary : WeaponLoadoutSlot.Primary;
        }
    }
}
