using Game.AI;
using Game.AI.States;

namespace Game.Characters.Npc
{
    /// <summary>
    /// IAiBrain for Village NPCs that wander instead of standing in place.
    /// Reuses the exact same IdleState/PatrolState Enemy will use — see
    /// npc-roles.md: "배회가 필요하면 Enemy와 동일한 IdleState/PatrolState를
    /// 그대로 붙인 WanderBrain을 하나 추가하면 된다".
    ///
    /// ChaseState/AttackState are wired in too (mirroring the standard
    /// Idle-Patrol-Chase-Attack graph from ai-state-machine.md) rather than
    /// special-cased out, purely so this brain doesn't need to invent a
    /// placeholder "no combat" state just to satisfy IdleState/PatrolState's
    /// required chase-state constructor argument. In practice a Village NPC
    /// is Faction.Neutral, and FactionUtility.IsHostileTo returns false
    /// whenever either side is Neutral, so AiSensor never reports a target
    /// and the Chase/Attack branches simply never trigger for the documented
    /// Village use case — but they behave exactly as specified if a
    /// wandering NPC's faction is ever changed.
    ///
    /// Dead is not wired in here either, for the same reason as
    /// CompanionBrain: the owner subscribes to HealthComponent.Died and
    /// calls StateMachine.ChangeState directly (see ai-state-machine.md).
    /// </summary>
    public class WanderBrain : IAiBrain
    {
        private readonly float patrolRadius;
        private readonly float idleDuration;
        private readonly float attackRange;
        private readonly float attackCooldown;
        private readonly float attackDamage;

        public WanderBrain(
            float patrolRadius = 8f,
            float idleDuration = 2f,
            float attackRange = 2f,
            float attackCooldown = 1.5f,
            float attackDamage = 10f)
        {
            this.patrolRadius = patrolRadius;
            this.idleDuration = idleDuration;
            this.attackRange = attackRange;
            this.attackCooldown = attackCooldown;
            this.attackDamage = attackDamage;
        }

        public IAiState BuildInitialState(AiContext context)
        {
            // Chase<->Attack is mutually referential; ForwardingAiState
            // breaks the construction cycle (see its doc comment).
            var chaseProxy = new ForwardingAiState();

            var patrolState = new PatrolState(chaseProxy, patrolRadius);
            var idleState = new IdleState(patrolState, chaseProxy, idleDuration);

            var attackState = new AttackState(chaseProxy, attackRange, attackCooldown, attackDamage);
            var chaseState = new ChaseState(attackState, patrolState, attackRange);
            chaseProxy.Target = chaseState;

            return idleState;
        }
    }
}
