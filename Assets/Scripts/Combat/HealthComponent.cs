using System;
using UnityEngine;
using Game.Player;

namespace Game.Combat
{
    /// <summary>
    /// Standard IDamageable implementation. Also implements IReadOnlyStat so
    /// HUD's StatBarUIView can bind to it directly (see documents/hud-system.md)
    /// — Damaged/Died are for gameplay reactions, Changed is for UI binding.
    /// </summary>
    public class HealthComponent : MonoBehaviour, IDamageable, IReadOnlyStat
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        private float currentHealth;
        private bool isAlive = true;

        public bool IsAlive => isAlive;
        public float Current => currentHealth;
        public float Max => maxHealth;

        public event Action<DamageInfo> Damaged;
        public event Action Died;
        public event Action Changed;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!isAlive)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damageInfo.Amount);
            Damaged?.Invoke(damageInfo);
            Changed?.Invoke();

            if (currentHealth <= 0f)
            {
                isAlive = false;
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!isAlive)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Changed?.Invoke();
        }
    }
}
