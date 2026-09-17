using System;
using UnityEngine;
using Game.Player;

namespace Game.Combat
{
    /// <summary>
    /// Armor value for HUD display, next to HealthComponent (documents/hud-system.md).
    /// Mirrors HealthComponent's IReadOnlyStat shape but is not IDamageable —
    /// armor absorbing damage before health is a combat-system follow-up this
    /// design explicitly leaves open.
    /// </summary>
    public class ArmorComponent : MonoBehaviour, IReadOnlyStat
    {
        [SerializeField, Min(0f)] private float maxArmor = 50f;

        private float currentArmor;

        public float Current => currentArmor;
        public float Max => maxArmor;
        public event Action Changed;

        private void Awake()
        {
            currentArmor = maxArmor;
        }

        public void TakeDamage(float amount)
        {
            currentArmor = Mathf.Max(0f, currentArmor - amount);
            Changed?.Invoke();
        }

        public void Heal(float amount)
        {
            currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
            Changed?.Invoke();
        }
    }
}
