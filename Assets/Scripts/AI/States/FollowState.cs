using System;
using UnityEngine;
using Game.AI;
using Game.Characters;
using Game.Combat;

namespace Game.AI.States
{
    /// <summary>
    /// Keeps station near a followed Transform (the player, for a Companion
    /// NPC) via CharacterMotor, per npc-roles.md's state table. Hands off to
    /// a combat state when a target should be engaged and to a hold state
    /// when ordered to stop.
    ///
    /// This class only ever touches context.Sensor/context.Self/
    /// CharacterMotor plus plain delegates — it never references
    /// CompanionOrderReceiver/CompanionOrder (Game.Characters.Npc), so it
    /// stays a generic, reusable Game.AI.States class instead of an
    /// NPC-specific one (matching the convention IdleState/PatrolState/
    /// ChaseState/AttackState already set). Whoever builds the state graph
    /// (Game.Characters.Npc.CompanionBrain) supplies isHoldRequested /
    /// resolveForcedTarget by reading its own order-tracking component.
    /// </summary>
    public class FollowState : IAiState
    {
        private readonly Transform followTarget;
        private readonly IAiState combatState;
        private readonly IAiState holdState;
        private readonly Func<bool> isHoldRequested;
        private readonly Func<IDamageable> resolveForcedTarget;
        private readonly float followDistance;
        private readonly float arrivalTolerance;

        public FollowState(
            Transform followTarget,
            IAiState combatState,
            IAiState holdState,
            Func<bool> isHoldRequested = null,
            Func<IDamageable> resolveForcedTarget = null,
            float followDistance = 3f,
            float arrivalTolerance = 0.5f)
        {
            this.followTarget = followTarget;
            this.combatState = combatState;
            this.holdState = holdState;
            this.isHoldRequested = isHoldRequested;
            this.resolveForcedTarget = resolveForcedTarget;
            this.followDistance = followDistance;
            this.arrivalTolerance = arrivalTolerance;
        }

        public void Enter(AiContext context)
        {
        }

        public void Tick(AiContext context, float deltaTime)
        {
            if (isHoldRequested != null && isHoldRequested())
            {
                context.StateMachine.ChangeState(holdState);
                return;
            }

            var target = ResolveTarget(context);
            if (target != null)
            {
                context.CurrentTarget = target;
                context.StateMachine.ChangeState(combatState);
                return;
            }

            var motor = context.Sensor.GetComponent<CharacterMotor>();
            if (followTarget == null)
            {
                motor?.Stop();
                return;
            }

            float distance = Vector3.Distance(context.Sensor.transform.position, followTarget.position);
            if (distance <= followDistance + arrivalTolerance)
            {
                motor?.Stop();
                return;
            }

            motor?.MoveTo(followTarget.position);
        }

        public void Exit(AiContext context)
        {
            context.Sensor.GetComponent<CharacterMotor>()?.Stop();
        }

        private IDamageable ResolveTarget(AiContext context)
        {
            var forced = resolveForcedTarget?.Invoke();
            if (forced != null && forced.IsAlive)
            {
                return forced;
            }

            var detected = context.Sensor.DetectedTarget;
            return detected != null && detected.IsAlive ? detected : null;
        }
    }
}
