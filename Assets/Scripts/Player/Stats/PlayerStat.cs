using System;
using UnityEngine;

namespace Game.Player
{
    public interface IReadOnlyStat
    {
        float Current { get; }
        float Max { get; }
        event Action Changed;
    }

    [Serializable]
    public class PlayerStat : IReadOnlyStat
    {
        [SerializeField] private float max = 100f;
        [SerializeField] private float current = 100f;

        public float Max => max;
        public float Current => current;
        public event Action Changed;

        public void Add(float amount)
        {
            current = Mathf.Clamp(current + amount, 0f, max);
            Changed?.Invoke();
        }

        public void SetCurrent(float value)
        {
            current = Mathf.Clamp(value, 0f, max);
            Changed?.Invoke();
        }
    }
}
