using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Base attribute values plus runtime modifiers (equipment/buffs). Final
    /// value is (base + sum of flat bonuses) * (1 + sum of percent bonuses).
    /// </summary>
    public class AttributeSet : MonoBehaviour
    {
        [Serializable]
        public struct BaseValue
        {
            public AttributeType Type;
            public float Value;
        }

        [SerializeField] private BaseValue[] baseValues;

        private readonly List<AttributeModifier> modifiers = new();

        public event Action Changed;

        public float GetValue(AttributeType type)
        {
            float flatSum = 0f;
            float percentSum = 0f;

            foreach (var modifier in modifiers)
            {
                if (modifier.Type != type)
                {
                    continue;
                }

                flatSum += modifier.FlatBonus;
                percentSum += modifier.PercentBonus;
            }

            return (GetBaseValue(type) + flatSum) * (1f + percentSum);
        }

        public void AddModifier(AttributeModifier modifier)
        {
            modifiers.Add(modifier);
            Changed?.Invoke();
        }

        public void RemoveAllFromSource(object source)
        {
            int removedCount = modifiers.RemoveAll(modifier => modifier.Source == source);
            if (removedCount > 0)
            {
                Changed?.Invoke();
            }
        }

        private float GetBaseValue(AttributeType type)
        {
            foreach (var entry in baseValues)
            {
                if (entry.Type == type)
                {
                    return entry.Value;
                }
            }

            return 0f;
        }
    }
}
