using Game.AI;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// Two-phase boss built from BossPhaseState (see its doc comment and
    /// ai-state-machine.md's "보스 확장"). Phase 2 hits harder/faster and
    /// never escalates further - it leaves the fight only through
    /// EnemyController's normal HealthComponent.Died -&gt; DeadState wiring,
    /// same as every other tier. Adding a third phase, or a different
    /// escalation threshold, only ever means changing the BuildPhase calls
    /// here - AiStateMachine/IAiState (Game.AI) stay untouched.
    /// </summary>
    public class BossEnemyBrain : IAiBrain
    {
        private const float Phase2HealthRatio = 0.5f;
        private const float Phase1IdleDuration = 1.5f;
        private const float Phase2IdleDuration = 0.75f;
        private const float Phase2DamageMultiplier = 1.5f;

        private readonly EnemyData data;

        public BossEnemyBrain(EnemyData data)
        {
            this.data = data;
        }

        public IAiState BuildInitialState(AiContext context)
        {
            var phase2 = BuildPhase(
                idleDuration: Phase2IdleDuration,
                attackRange: data.AttackRange,
                damageAmount: data.AttackDamage * Phase2DamageMultiplier,
                escalateBelowHealthRatio: null,
                nextPhaseState: null);

            var phase1 = BuildPhase(
                idleDuration: Phase1IdleDuration,
                attackRange: data.AttackRange,
                damageAmount: data.AttackDamage,
                escalateBelowHealthRatio: Phase2HealthRatio,
                nextPhaseState: phase2);

            return phase1;
        }

        private static BossPhaseState BuildPhase(float idleDuration, float attackRange, float damageAmount, float? escalateBelowHealthRatio, IAiState nextPhaseState)
        {
            var nestedMachine = new AiStateMachine();

            // PhaseIdleState/PhaseAttackState need each other's instance at
            // construction (same readonly-field cycle problem NormalEnemyBrain
            // solves for Idle/Patrol/Chase/Attack) - see DeferredAiState.
            var idleSlot = new DeferredAiState();
            var attackState = new PhaseAttackState(nestedMachine, idleSlot, attackRange, damageAmount);
            var idleState = new PhaseIdleState(nestedMachine, attackState, idleDuration);
            idleSlot.Target = idleState;

            return new BossPhaseState(nestedMachine, idleState, escalateBelowHealthRatio, nextPhaseState);
        }
    }
}
