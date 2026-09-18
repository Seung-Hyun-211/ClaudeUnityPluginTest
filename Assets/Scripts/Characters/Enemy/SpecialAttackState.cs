using UnityEngine;
using Game.AI;
using Game.AI.States;
using Game.Characters;
using Game.Combat;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// Elite-only cooldown-gated extra attack layered on top of the shared
    /// Chase/Attack loop (see character-system.md's Enemy 절 and
    /// ai-state-machine.md's "Elite 확장"). Not a generally reusable state
    /// like the ones in Game.AI.States - it exists only for the Elite tier -
    /// so it lives here rather than Game.AI.States. IsReady is read by
    /// SpecialAttackGate to decide, once per Chase-&gt;attack transition,
    /// whether to route into this state instead of the plain AttackState;
    /// ChaseState/AttackState themselves are reused completely unmodified.
    /// Uses context.Self.GetComponent&lt;CharacterMotor&gt;() rather than
    /// context.Sensor.GetComponent (as the shared states do) precisely
    /// because AiContext.Self exists for this kind of tier-specific,
    /// reach-back-to-owner behaviour (see AiContext's doc comment).
    /// </summary>
    public class SpecialAttackState : IAiState
    {
        private const float WindupDuration = 0.5f;

        private readonly IAiState chaseState;
        private readonly float cooldownDuration;
        private readonly float damageAmount;

        private float readyAtTime;
        private float windupRemaining;
        private bool hasStruck;

        public SpecialAttackState(IAiState chaseState, float cooldownDuration, float damageAmount)
        {
            this.chaseState = chaseState;
            this.cooldownDuration = cooldownDuration;
            this.damageAmount = damageAmount;
        }

        /// <summary>Ready immediately the first time (readyAtTime defaults to 0), then gated by cooldownDuration after each use.</summary>
        public bool IsReady => Time.time >= readyAtTime;

        public void Enter(AiContext context)
        {
            windupRemaining = WindupDuration;
            hasStruck = false;
            context.Self.GetComponent<CharacterMotor>()?.Stop();
        }

        public void Tick(AiContext context, float deltaTime)
        {
            var target = context.CurrentTarget;
            if (target == null || !target.IsAlive || !ChaseState.TryGetTargetPosition(target, out var targetPosition))
            {
                context.CurrentTarget = null;
                context.StateMachine.ChangeState(chaseState);
                return;
            }

            windupRemaining -= deltaTime;
            if (windupRemaining > 0f)
            {
                return;
            }

            if (!hasStruck)
            {
                hasStruck = true;
                target.TakeDamage(new DamageInfo(damageAmount, DamageType.Physical, context.Self, targetPosition));
                readyAtTime = Time.time + cooldownDuration;
            }

            context.StateMachine.ChangeState(chaseState);
        }

        public void Exit(AiContext context)
        {
        }
    }
}
