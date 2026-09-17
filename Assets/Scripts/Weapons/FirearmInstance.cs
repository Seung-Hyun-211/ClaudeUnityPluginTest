using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat;

namespace Game.Weapons
{
    /// <summary>
    /// Runtime firearm: shared FirearmData + per-instance attached parts and
    /// loaded magazine. Effective stats are base value combined with attached
    /// parts' modifiers — the same additive-then-percent formula AttributeSet
    /// uses for character attributes (see player-attributes.md), applied here
    /// to weapon stats instead.
    /// </summary>
    public class FirearmInstance : IWeapon, IWeaponInfo
    {
        private readonly Dictionary<WeaponPartSlot, WeaponPartData> attachedParts = new();
        private float cooldownRemaining;

        public FirearmData Data { get; }
        public WeaponItemData Item => Data;
        public MagazineInstance LoadedMagazine { get; private set; }

        public Sprite Icon => Data.Icon;
        public string DisplayName => Data.DisplayName;
        public int CurrentAmmo => LoadedMagazine?.LoadedRounds ?? 0;
        public int MagazineSize => LoadedMagazine?.Data.Capacity ?? 0;
        public event Action Changed;

        public FirearmInstance(FirearmData data)
        {
            Data = data;
        }

        public float GetEffectiveStat(WeaponStatType stat)
        {
            float baseValue = Data.GetBaseStat(stat);
            float flatSum = 0f;
            float percentSum = 0f;

            foreach (var part in attachedParts.Values)
            {
                foreach (var modifier in part.Modifiers)
                {
                    if (modifier.Stat != stat)
                    {
                        continue;
                    }

                    flatSum += modifier.FlatBonus;
                    percentSum += modifier.PercentBonus;
                }
            }

            return (baseValue + flatSum) * (1f + percentSum);
        }

        /// <returns>The part that previously occupied that slot, if any.</returns>
        public WeaponPartData AttachPart(WeaponPartSlot slot, WeaponPartData part)
        {
            attachedParts.TryGetValue(slot, out var previous);
            attachedParts[slot] = part;
            Changed?.Invoke();
            return previous;
        }

        public WeaponPartData DetachPart(WeaponPartSlot slot)
        {
            if (!attachedParts.TryGetValue(slot, out var part))
            {
                return null;
            }

            attachedParts.Remove(slot);
            Changed?.Invoke();
            return part;
        }

        /// <returns>False if the magazine's ammo type doesn't fit this firearm — nothing changes in that case.</returns>
        public bool TryInsertMagazine(MagazineInstance magazine, out MagazineInstance previous)
        {
            previous = null;

            if (magazine != null && magazine.Data.CompatibleAmmoType != Data.CompatibleAmmoType)
            {
                return false;
            }

            previous = LoadedMagazine;
            LoadedMagazine = magazine;
            Changed?.Invoke();
            return true;
        }

        public bool CanUse(WeaponUseContext context)
        {
            return cooldownRemaining <= 0f && LoadedMagazine != null && !LoadedMagazine.IsEmpty;
        }

        public void Tick(float deltaTime)
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
        }

        public void Use(WeaponUseContext context)
        {
            if (!CanUse(context))
            {
                return;
            }

            LoadedMagazine.TryConsumeRound();
            cooldownRemaining = 1f / GetEffectiveStat(WeaponStatType.FireRate);
            Changed?.Invoke();

            if (Physics.Raycast(context.AimOrigin, context.AimDirection, out var hit, Data.Range) &&
                hit.collider.TryGetComponent(out IDamageable target))
            {
                target.TakeDamage(new DamageInfo(GetEffectiveStat(WeaponStatType.Damage), DamageType.Physical, context.Wielder, hit.point));
            }
        }
    }
}
