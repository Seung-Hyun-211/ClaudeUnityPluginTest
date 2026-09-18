using UnityEngine;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// Per-monster tuning data (see character-system.md's Enemy 절). "Normal
    /// Goblin"/"Elite Goblin"/"Goblin Chieftain(Boss)" are all the same
    /// EnemyController prefab with a different EnemyData asset plugged in -
    /// tier differences live entirely in data + which IAiBrain gets picked
    /// for Tier, never in a new component or subclass.
    /// </summary>
    [CreateAssetMenu(menuName = "Characters/Enemy Data", fileName = "New Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private EnemyTier tier;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0f)] private float attackRange = 2f;
        [SerializeField, Min(0f)] private float detectionRadius = 10f;

        public EnemyTier Tier => tier;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float DetectionRadius => detectionRadius;
    }
}
