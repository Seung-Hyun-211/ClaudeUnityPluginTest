using UnityEngine;
using Game.AI;
using Game.Characters;
using Game.Combat;

namespace Game.AI.States
{
    /// <summary>
    /// Attacks AiContext.CurrentTarget on a cooldown while in range, falling
    /// back to ChaseState otherwise. Reused verbatim by Companion NPCs
    /// (npc-roles.md's AssistCombatState) — only reads CurrentTarget/Sensor,
    /// same reasoning as ChaseState.
    /// </summary>
    public class AttackState : IAiState
    {
        private readonly IAiState chaseState;
        private readonly float attackRange;
        private readonly float attackCooldown;
        private readonly float damageAmount;

        private float cooldownRemaining;

        public AttackState(IAiState chaseState, float attackRange = 2f, float attackCooldown = 1.5f, float damageAmount = 10f)
        {
            this.chaseState = chaseState;
            this.attackRange = attackRange;
            this.attackCooldown = attackCooldown;
            this.damageAmount = damageAmount;
        }

        public void Enter(AiContext context)
        {
            cooldownRemaining = 0f;
            context.Sensor.GetComponent<CharacterMotor>()?.Stop();
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

            if (Vector3.Distance(context.Sensor.transform.position, targetPosition) > attackRange)
            {
                context.StateMachine.ChangeState(chaseState);
                return;
            }

            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f)
            {
                return;
            }

            cooldownRemaining = attackCooldown;
            target.TakeDamage(new DamageInfo(damageAmount, DamageType.Physical, context.Sensor.gameObject, targetPosition));
        }

        public void Exit(AiContext context)
        {
        }
    }
}
