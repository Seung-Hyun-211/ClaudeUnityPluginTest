using Game.AI;
using Game.AI.States;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// Same graph as NormalEnemyBrain plus one cooldown-based extra attack
    /// (see character-system.md's Enemy 절: "Normal과 동일한 상태 + 쿨다운 기반
    /// SpecialAttackState 하나 추가"). IdleState/PatrolState/ChaseState/
    /// AttackState are reused completely unmodified - only SpecialAttackState
    /// is new, plus the small SpecialAttackGate router needed to let
    /// ChaseState (also unmodified) hand off to either attack depending on
    /// the special attack's cooldown.
    /// </summary>
    public class EliteEnemyBrain : IAiBrain
    {
        private readonly EnemyData data;
        private readonly float specialAttackCooldown;
        private readonly float specialAttackDamageMultiplier;

        public EliteEnemyBrain(EnemyData data, float specialAttackCooldown = 6f, float specialAttackDamageMultiplier = 2f)
        {
            this.data = data;
            this.specialAttackCooldown = specialAttackCooldown;
            this.specialAttackDamageMultiplier = specialAttackDamageMultiplier;
        }

        public IAiState BuildInitialState(AiContext context)
        {
            var chaseSlot = new ForwardingAiState();

            var normalAttackState = new AttackState(chaseSlot, data.AttackRange, damageAmount: data.AttackDamage);
            var specialAttackState = new SpecialAttackState(chaseSlot, specialAttackCooldown, data.AttackDamage * specialAttackDamageMultiplier);
            var attackGate = new SpecialAttackGate(normalAttackState, specialAttackState);

            var patrolState = new PatrolState(chaseSlot);
            var chaseState = new ChaseState(attackGate, patrolState, data.AttackRange);
            chaseSlot.Target = chaseState;

            return new IdleState(patrolState, chaseState);
        }
    }
}
