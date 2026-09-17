using UnityEngine;
using Game.Combat;

namespace Game.Weapons
{
    /// <summary>No ammo/parts — just a cooldown-gated swing. Kept deliberately simpler than FirearmInstance.</summary>
    public class MeleeWeaponInstance : IWeapon, IWeaponDisplay
    {
        private float cooldownRemaining;

        public MeleeWeaponData Data { get; }
        public WeaponItemData Item => Data;
        public Sprite Icon => Data.Icon;
        public string DisplayName => Data.DisplayName;

        public MeleeWeaponInstance(MeleeWeaponData data)
        {
            Data = data;
        }

        public bool CanUse(WeaponUseContext context) => cooldownRemaining <= 0f;

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

            cooldownRemaining = 1f / Data.AttackRate;

            Vector3 strikeCenter = context.AimOrigin + context.AimDirection * (Data.Range * 0.5f);
            foreach (var hit in Physics.OverlapSphere(strikeCenter, Data.Range * 0.5f))
            {
                if (hit.gameObject == context.Wielder)
                {
                    continue;
                }

                if (hit.TryGetComponent(out IDamageable target))
                {
                    target.TakeDamage(new DamageInfo(Data.Damage, DamageType.Physical, context.Wielder, hit.ClosestPoint(strikeCenter)));
                }
            }
        }
    }
}
