using Game.AI;
using Game.AI.States;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// Idle -&gt; Patrol -&gt; Chase -&gt; Attack using only the shared Game.AI.States
    /// classes, exactly as ai-state-machine.md's "공통 상태 (Normal 티어 기준)"
    /// table specifies. Dead is not part of this graph: EnemyController
    /// subscribes to HealthComponent.Died itself and forces the top-level
    /// AiStateMachine into DeadState directly (see DeadState's own doc
    /// comment) so no state here needs to poll health every frame.
    /// </summary>
    public class NormalEnemyBrain : IAiBrain
    {
        private readonly EnemyData data;

        public NormalEnemyBrain(EnemyData data)
        {
            this.data = data;
        }

        public IAiState BuildInitialState(AiContext context)
        {
            // Chase/Attack/Patrol form a 3-way constructor cycle (Chase needs
            // Attack+Patrol, Attack needs Chase, Patrol needs Chase) - see
            // ForwardingAiState for why a stand-in is required to build this.
            var chaseSlot = new ForwardingAiState();

            var attackState = new AttackState(chaseSlot, data.AttackRange, damageAmount: data.AttackDamage);
            var patrolState = new PatrolState(chaseSlot);
            var chaseState = new ChaseState(attackState, patrolState, data.AttackRange);
            chaseSlot.Target = chaseState;

            return new IdleState(patrolState, chaseState);
        }
    }
}
