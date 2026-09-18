using UnityEngine;
using Game.AI;
using Game.AI.States;
using Game.Combat;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// The attack half of one boss phase's small Idle&lt;-&gt;Attack loop (see
    /// ai-state-machine.md's "보스 확장": "Phase1Attack --&gt; Phase1Idle"). Like
    /// PhaseIdleState, it only ever changes the phase's own nested
    /// AiStateMachine, never context.StateMachine. A boss is assumed to
    /// already be engaged (no patrol/chase within a phase - see the boss
    /// stateDiagram, which has no such states), so this simply hits whatever
    /// context.CurrentTarget is while in range and goes back to idle either
    /// way. Reused for every phase with different range/damage.
    /// </summary>
    public class PhaseAttackState : IAiState
    {
        private readonly AiStateMachine nestedMachine;
        private readonly IAiState idleState;
        private readonly float attackRange;
        private readonly float damageAmount;

        public PhaseAttackState(AiStateMachine nestedMachine, IAiState idleState, float attackRange, float damageAmount)
        {
            this.nestedMachine = nestedMachine;
            this.idleState = idleState;
            this.attackRange = attackRange;
            this.damageAmount = damageAmount;
        }

        public void Enter(AiContext context)
        {
        }

        public void Tick(AiContext context, float deltaTime)
        {
            var target = context.CurrentTarget ?? context.Sensor.DetectedTarget;
            if (target == null || !target.IsAlive || !ChaseState.TryGetTargetPosition(target, out var targetPosition) ||
                Vector3.Distance(context.Sensor.transform.position, targetPosition) > attackRange)
            {
                nestedMachine.ChangeState(idleState);
                return;
            }

            target.TakeDamage(new DamageInfo(damageAmount, DamageType.Physical, context.Self, targetPosition));
            nestedMachine.ChangeState(idleState);
        }

        public void Exit(AiContext context)
        {
        }
    }
}
