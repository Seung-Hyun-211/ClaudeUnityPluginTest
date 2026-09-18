using Game.AI;
using Game.Combat;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// One phase of a multi-phase boss fight, treating the phase itself as a
    /// single IAiState that wraps its own nested AiStateMachine (see
    /// ai-state-machine.md's "보스 확장": "Phase1State/Phase2State는 각각
    /// IAiState를 구현하면서 내부적으로 자신만의 작은 AiStateMachine을 하나 더
    /// 들고 있는 복합 상태"). One reusable class parametrized per phase
    /// (nested graph, escalation threshold, next phase) rather than a
    /// Phase1State/Phase2State pair, per the doc's own framing of what
    /// changes between bosses: "BossEnemyBrain이 어떤 PhaseState들을 몇 개,
    /// 어떤 임계값으로 연결할지만 다르게 구성하면 된다".
    ///
    /// Boundary rule (see the same doc section): only THIS class ever calls
    /// context.StateMachine.ChangeState (to escalate to nextPhaseState). The
    /// nested Idle/Attack states only ever call nestedMachine.ChangeState -
    /// they never see or touch the outer context.StateMachine - so an inner
    /// state can never accidentally skip a phase.
    /// </summary>
    public class BossPhaseState : IAiState
    {
        private readonly AiStateMachine nestedMachine;
        private readonly IAiState nestedInitialState;
        private readonly float? escalateBelowHealthRatio;
        private readonly IAiState nextPhaseState;

        private AiContext context;
        private HealthComponent health;
        private bool hasEscalated;

        /// <param name="nestedMachine">This phase's own nested AiStateMachine (not yet Initialized) - built by BossEnemyBrain alongside nestedInitialState.</param>
        /// <param name="nestedInitialState">The phase's entry state within nestedMachine (e.g. a PhaseIdleState).</param>
        /// <param name="escalateBelowHealthRatio">Current/Max ratio that triggers escalation to nextPhaseState, or null for the final phase (which never escalates - it only ever leaves via EnemyController's HealthComponent.Died -&gt; DeadState).</param>
        /// <param name="nextPhaseState">State to escalate to, or null when escalateBelowHealthRatio is null.</param>
        public BossPhaseState(AiStateMachine nestedMachine, IAiState nestedInitialState, float? escalateBelowHealthRatio, IAiState nextPhaseState)
        {
            this.nestedMachine = nestedMachine;
            this.nestedInitialState = nestedInitialState;
            this.escalateBelowHealthRatio = escalateBelowHealthRatio;
            this.nextPhaseState = nextPhaseState;
        }

        public void Enter(AiContext ctx)
        {
            context = ctx;
            hasEscalated = false;

            // Tier-specific reach-back to the owner, which is exactly why
            // AiContext.Self exists (see its doc comment) - the phase needs
            // its own HealthComponent to judge the escalation threshold.
            health = context.Self.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.Damaged += HandleDamaged;
            }

            nestedMachine.Initialize(context);
            nestedMachine.ChangeState(nestedInitialState);
        }

        public void Tick(AiContext ctx, float deltaTime)
        {
            nestedMachine.Tick(deltaTime);
        }

        public void Exit(AiContext ctx)
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
            }
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (hasEscalated || escalateBelowHealthRatio == null || health == null || health.Max <= 0f)
            {
                return;
            }

            if (health.Current / health.Max <= escalateBelowHealthRatio.Value)
            {
                hasEscalated = true;
                context.StateMachine.ChangeState(nextPhaseState);
            }
        }
    }
}
