using Game.AI;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// Passed to ChaseState as its "attackState" instead of a plain
    /// AttackState, so the moment the target comes into range ChaseState
    /// (unmodified) hands off here, and this decides - once, on Enter - which
    /// concrete attack to actually run (see ai-state-machine.md's "Elite
    /// 확장": "Chase --&gt; SpecialAttack: 쿨다운 완료 &amp; 사거리 내"). It never
    /// itself becomes the "current" state from the state machine's point of
    /// view for more than the instant it takes to redirect: ChangeState's
    /// Exit-&gt;swap-&gt;Enter sequence means calling ChangeState again from inside
    /// Enter simply swaps again before any Tick runs on this gate.
    /// </summary>
    internal sealed class SpecialAttackGate : IAiState
    {
        private readonly IAiState normalAttackState;
        private readonly SpecialAttackState specialAttackState;

        public SpecialAttackGate(IAiState normalAttackState, SpecialAttackState specialAttackState)
        {
            this.normalAttackState = normalAttackState;
            this.specialAttackState = specialAttackState;
        }

        public void Enter(AiContext context)
        {
            IAiState next = specialAttackState.IsReady ? specialAttackState : normalAttackState;
            context.StateMachine.ChangeState(next);
        }

        public void Tick(AiContext context, float deltaTime)
        {
        }

        public void Exit(AiContext context)
        {
        }
    }
}
