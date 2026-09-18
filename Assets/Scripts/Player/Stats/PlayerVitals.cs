using UnityEngine;

namespace Game.Player
{
    public class PlayerVitals : MonoBehaviour
    {
        [SerializeField] private PlayerStat hunger = new();
        [SerializeField] private PlayerStat thirst = new();
        [SerializeField] private float hungerDecayPerSecond = 0.05f;
        [SerializeField] private float thirstDecayPerSecond = 0.08f;

        public IReadOnlyStat Hunger => hunger;
        public IReadOnlyStat Thirst => thirst;

        /// <summary>Raw restore for save/load - bypasses decay, matches PlayerStat.SetCurrent's clamping.</summary>
        public void RestoreVitals(float hungerCurrent, float thirstCurrent)
        {
            hunger.SetCurrent(hungerCurrent);
            thirst.SetCurrent(thirstCurrent);
        }

        private void Update()
        {
            hunger.Add(-hungerDecayPerSecond * Time.deltaTime);
            thirst.Add(-thirstDecayPerSecond * Time.deltaTime);
        }
    }
}
