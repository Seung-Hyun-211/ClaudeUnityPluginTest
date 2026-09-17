using UnityEngine;
using Game.AI;
using Game.Characters;
using Game.Combat;

namespace Game.AI.States
{
    /// <summary>
    /// Moves toward AiContext.CurrentTarget; switches to AttackState in range
    /// or back to PatrolState if the target is lost. Reused verbatim by
    /// Companion NPCs (npc-roles.md's AssistCombatState) — it only ever reads
    /// CurrentTarget/Sensor and never assumes the target or the owner's
    /// faction, so it works the same whether the chaser is hostile or friendly.
    /// </summary>
    public class ChaseState : IAiState
    {
        private readonly IAiState attackState;
        private readonly IAiState patrolState;
        private readonly float attackRange;

        public ChaseState(IAiState attackState, IAiState patrolState, float attackRange = 2f)
        {
            this.attackState = attackState;
            this.patrolState = patrolState;
            this.attackRange = attackRange;
        }

        public void Enter(AiContext context)
        {
        }

        public void Tick(AiContext context, float deltaTime)
        {
            var target = context.CurrentTarget;
            if (target == null || !target.IsAlive || !TryGetTargetPosition(target, out var targetPosition))
            {
                context.CurrentTarget = null;
                context.StateMachine.ChangeState(patrolState);
                return;
            }

            var selfPosition = context.Sensor.transform.position;
            if (Vector3.Distance(selfPosition, targetPosition) <= attackRange)
            {
                context.StateMachine.ChangeState(attackState);
                return;
            }

            context.Sensor.GetComponent<CharacterMotor>()?.MoveTo(targetPosition);
        }

        public void Exit(AiContext context)
        {
        }

        /// <summary>Every current IDamageable is a MonoBehaviour (HealthComponent); this cast is how states read a target's position without IDamageable itself depending on Transform.</summary>
        internal static bool TryGetTargetPosition(IDamageable target, out Vector3 position)
        {
            if (target is Component component)
            {
                position = component.transform.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
