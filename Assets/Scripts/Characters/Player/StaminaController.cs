using System;
using UnityEngine;
using Game.Player;

namespace Game.Characters.Player
{
    /// <summary>
    /// Owns "how much stamina is left" and nothing else — it has no idea what
    /// Sprint or Jump are, callers decide what to spend it on (see
    /// player-attributes.md). Reuses the existing PlayerStat/IReadOnlyStat
    /// pair so StatBarUIView can bind to Stamina exactly like Hunger/Thirst.
    ///
    /// Exhausted/Recovered exist purely as hooks for a future debuff system
    /// (design-conflict-review.md #4 — Docs/기획문서_피해디버프시스템설계.md's
    /// Exhaustion debuff) — this class still knows nothing about debuffs, it
    /// only announces the two edges a debuff would react to.
    /// </summary>
    public class StaminaController : MonoBehaviour
    {
        [SerializeField] private PlayerStat stamina = new();
        [SerializeField, Min(0f)] private float regenPerSecond = 10f;
        [SerializeField, Min(0f)] private float regenDelayAfterUse = 1f;
        [SerializeField, Range(0f, 1f)] private float recoveryThresholdRatio = 0.3f;

        private float regenDelayRemaining;
        private bool isExhausted;

        public IReadOnlyStat Stamina => stamina;

        /// <summary>Fired once when stamina reaches 0.</summary>
        public event Action Exhausted;

        /// <summary>Fired once when stamina regenerates back above recoveryThresholdRatio after being exhausted.</summary>
        public event Action Recovered;

        public bool TryConsume(float amount)
        {
            if (amount <= 0f || stamina.Current < amount)
            {
                return false;
            }

            stamina.Add(-amount);
            regenDelayRemaining = regenDelayAfterUse;

            if (!isExhausted && stamina.Current <= 0f)
            {
                isExhausted = true;
                Exhausted?.Invoke();
            }

            return true;
        }

        private void Update()
        {
            if (regenDelayRemaining > 0f)
            {
                regenDelayRemaining -= Time.deltaTime;
                return;
            }

            if (stamina.Current < stamina.Max)
            {
                stamina.Add(regenPerSecond * Time.deltaTime);
            }

            if (isExhausted && stamina.Current >= stamina.Max * recoveryThresholdRatio)
            {
                isExhausted = false;
                Recovered?.Invoke();
            }
        }
    }
}
