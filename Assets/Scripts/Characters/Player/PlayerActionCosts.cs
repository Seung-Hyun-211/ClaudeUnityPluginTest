using System;
using UnityEngine;

namespace Game.Characters.Player
{
    /// <summary>
    /// Stamina cost per player action, as data — adding a new action (e.g.
    /// Dodge) is a new enum entry + one row here, StaminaController never
    /// changes (see player-attributes.md).
    /// </summary>
    [CreateAssetMenu(menuName = "Characters/Player Action Costs", fileName = "New Player Action Costs")]
    public class PlayerActionCosts : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public PlayerActionType Action;
            public float Cost;
            public bool PerSecond;
        }

        [SerializeField] private Entry[] costs;

        public float GetCost(PlayerActionType action)
        {
            foreach (var entry in costs)
            {
                if (entry.Action == action)
                {
                    return entry.Cost;
                }
            }

            return 0f;
        }
    }
}
