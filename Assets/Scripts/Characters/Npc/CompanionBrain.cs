using System;
using UnityEngine;
using Game.AI;
using Game.AI.States;
using Game.Combat;

namespace Game.Characters.Npc
{
    /// <summary>
    /// IAiBrain for CombatHelper NPCs. Builds the Follow/Hold/AssistCombat
    /// graph from npc-roles.md's stateDiagram:
    ///
    ///   [*] -> Follow
    ///   Follow -> AssistCombat : sensor sees a hostile & Order != Hold
    ///   AssistCombat -> Follow : target lost/killed
    ///   Follow -> Hold : Order = Hold
    ///   Hold -> Follow : Order = Follow
    ///
    /// AssistCombat is NOT a new class — it's Game.AI.States.ChaseState/
    /// AttackState reused verbatim, exactly as npc-roles.md specifies, since
    /// both only ever read context.CurrentTarget and never care who is
    /// chasing. Dead is not wired in here either, for the same reason
    /// ai-state-machine.md gives for Enemy: the owner subscribes to
    /// HealthComponent.Died and calls StateMachine.ChangeState directly
    /// instead of every state polling health each tick.
    ///
    /// FollowState/HoldState (Game.AI.States) never reference
    /// CompanionOrderReceiver/CompanionOrder — this brain reads the receiver
    /// once here and exposes only what those states need as plain delegates
    /// (Func&lt;bool&gt;, Func&lt;IDamageable&gt;). That keeps the states
    /// themselves NPC-agnostic (dependency inversion) while still letting
    /// them react to orders.
    /// </summary>
    public class CompanionBrain : IAiBrain
    {
        private readonly Transform followTarget;
        private readonly float followDistance;
        private readonly float attackRange;
        private readonly float attackCooldown;
        private readonly float attackDamage;

        /// <param name="followTarget">
        /// Transform to keep station near (the player, in practice). Not
        /// covered explicitly by npc-roles.md — there's no player singleton
        /// in this codebase, so whoever spawns/configures this Companion NPC
        /// supplies it, the same way EnemyData/tuning values are handed to
        /// other IAiState/IAiBrain constructors rather than looked up
        /// globally.
        /// </param>
        public CompanionBrain(
            Transform followTarget,
            float followDistance = 3f,
            float attackRange = 2f,
            float attackCooldown = 1.5f,
            float attackDamage = 10f)
        {
            this.followTarget = followTarget;
            this.followDistance = followDistance;
            this.attackRange = attackRange;
            this.attackCooldown = attackCooldown;
            this.attackDamage = attackDamage;
        }

        public IAiState BuildInitialState(AiContext context)
        {
            var receiver = context.Self.GetComponent<CompanionOrderReceiver>();

            Func<bool> isHoldRequested = receiver != null
                ? () => receiver.CurrentOrder == CompanionOrder.Hold
                : (Func<bool>)null;

            Func<IDamageable> resolveForcedTarget = receiver != null
                ? () => receiver.CurrentOrder == CompanionOrder.AttackTarget ? receiver.AttackTarget : null
                : (Func<IDamageable>)null;

            // Follow <-> Hold and Follow <-> AssistCombat(Chase) are mutually
            // referential; ForwardingAiState breaks the construction cycle
            // (see its doc comment) without needing to mutate ChaseState/
            // AttackState/FollowState/HoldState.
            var followProxy = new ForwardingAiState();
            var combatProxy = new ForwardingAiState();

            var holdState = new HoldState(followProxy, isHoldRequested);
            var followState = new FollowState(followTarget, combatProxy, holdState, isHoldRequested, resolveForcedTarget, followDistance);
            followProxy.Target = followState;

            var attackState = new AttackState(combatProxy, attackRange, attackCooldown, attackDamage);
            var chaseState = new ChaseState(attackState, followState, attackRange);
            combatProxy.Target = chaseState;

            return followState;
        }
    }
}
