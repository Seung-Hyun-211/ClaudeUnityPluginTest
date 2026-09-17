using System;
using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Two firearm slots + one melee slot, with one active at a time. Attach
    /// to any actor (player or enemy) — nothing here assumes who owns it,
    /// which is what lets enemies pick up and use weapons too.
    /// </summary>
    public class WeaponLoadout : MonoBehaviour
    {
        public FirearmInstance Primary { get; private set; }
        public FirearmInstance Secondary { get; private set; }
        public MeleeWeaponInstance Melee { get; private set; }
        public WeaponLoadoutSlot ActiveSlot { get; private set; } = WeaponLoadoutSlot.Primary;

        public event Action Changed;

        public IWeapon ActiveWeapon => ActiveSlot switch
        {
            WeaponLoadoutSlot.Primary => Primary,
            WeaponLoadoutSlot.Secondary => Secondary,
            WeaponLoadoutSlot.Melee => Melee,
            _ => null
        };

        private void Update()
        {
            Primary?.Tick(Time.deltaTime);
            Secondary?.Tick(Time.deltaTime);
            Melee?.Tick(Time.deltaTime);
        }

        /// <returns>The firearm previously in that slot, if any.</returns>
        public FirearmInstance EquipFirearm(FirearmInstance firearm, WeaponLoadoutSlot slot)
        {
            FirearmInstance previous;

            if (slot == WeaponLoadoutSlot.Primary)
            {
                previous = Primary;
                Primary = firearm;
            }
            else if (slot == WeaponLoadoutSlot.Secondary)
            {
                previous = Secondary;
                Secondary = firearm;
            }
            else
            {
                throw new ArgumentException("Firearms can only be equipped into Primary or Secondary.", nameof(slot));
            }

            Changed?.Invoke();
            return previous;
        }

        /// <returns>The melee weapon previously equipped, if any.</returns>
        public MeleeWeaponInstance EquipMelee(MeleeWeaponInstance melee)
        {
            var previous = Melee;
            Melee = melee;
            Changed?.Invoke();
            return previous;
        }

        public void SwitchTo(WeaponLoadoutSlot slot)
        {
            if (ActiveSlot == slot)
            {
                return;
            }

            ActiveSlot = slot;
            Changed?.Invoke();
        }

        public bool TryUseActive(WeaponUseContext context)
        {
            var weapon = ActiveWeapon;
            if (weapon == null || !weapon.CanUse(context))
            {
                return false;
            }

            weapon.Use(context);
            return true;
        }
    }
}
