using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Base stats shared by Player/NPC/Enemy. EnemyData extends on top of this
    /// with combat-specific values (see character-system.md).
    /// </summary>
    [CreateAssetMenu(menuName = "Characters/Character Stats Data", fileName = "New Character Stats")]
    public class CharacterStatsData : ScriptableObject
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;

        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
    }
}
