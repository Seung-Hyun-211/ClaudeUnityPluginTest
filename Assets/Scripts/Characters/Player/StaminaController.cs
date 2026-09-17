using UnityEngine;
using Game.Player;

namespace Game.Characters.Player
{
    /// <summary>
    /// Owns "how much stamina is left" and nothing else — it has no idea what
    /// Sprint or Jump are, callers decide what to spend it on (see
    /// player-attributes.md). Reuses the existing PlayerStat/IReadOnlyStat
    /// pair so StatBarUIView can bind to Stamina exactly like Hunger/Thirst.
    /// </summary>
    public class StaminaController : MonoBehaviour
    {
        [SerializeField] private PlayerStat stamina = new();
        [SerializeField, Min(0f)] private float regenPerSecond = 10f;
        [SerializeField, Min(0f)] private float regenDelayAfterUse = 1f;

        private float regenDelayRemaining;

        public IReadOnlyStat Stamina => stamina;

        public bool TryConsume(float amount)
        {
            if (amount <= 0f || stamina.Current < amount)
            {
                return false;
            }

            stamina.Add(-amount);
            regenDelayRemaining = regenDelayAfterUse;
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
        }
    }
}
