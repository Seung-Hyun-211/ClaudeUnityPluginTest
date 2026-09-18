using Game.AI;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// The idle half of one boss phase's small Idle&lt;-&gt;Attack loop (see
    /// ai-state-machine.md's "보스 확장": "Phase1Idle --&gt; Phase1Attack"). Waits
    /// idleDuration then hands off - but only within the phase's OWN nested
    /// AiStateMachine (passed in by BossEnemyBrain), never the top-level
    /// context.StateMachine. That boundary is what lets a phase escalation
    /// (handled by BossPhaseState) never be short-circuited by an inner
    /// state accidentally touching the outer machine (see BossPhaseState's
    /// doc comment). Reused for every phase with different timings, so
    /// BossEnemyBrain never needs a Phase1-specific/Phase2-specific subclass.
    /// </summary>
    public class PhaseIdleState : IAiState
    {
        private readonly AiStateMachine nestedMachine;
        private readonly IAiState attackState;
        private readonly float idleDuration;

        private float elapsed;

        public PhaseIdleState(AiStateMachine nestedMachine, IAiState attackState, float idleDuration)
        {
            this.nestedMachine = nestedMachine;
            this.attackState = attackState;
            this.idleDuration = idleDuration;
        }

        public void Enter(AiContext context)
        {
            elapsed = 0f;
        }

        public void Tick(AiContext context, float deltaTime)
        {
            elapsed += deltaTime;
            if (elapsed >= idleDuration)
            {
                nestedMachine.ChangeState(attackState);
            }
        }

        public void Exit(AiContext context)
        {
        }
    }
}
