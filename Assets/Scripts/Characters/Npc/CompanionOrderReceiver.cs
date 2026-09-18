using UnityEngine;
using Game.Combat;

namespace Game.Characters.Npc
{
    /// <summary>
    /// Holds the order a Companion NPC is currently under, and the specific
    /// target for AttackTarget. Deliberately dumb: it doesn't know how orders
    /// get issued (UI button, shortcut, ...) or how they affect behaviour —
    /// CompanionBrain reads this component when building the state graph and
    /// hands FollowState/HoldState plain delegates, so those states never
    /// reference this type directly (see CompanionBrain, FollowState).
    /// </summary>
    public class CompanionOrderReceiver : MonoBehaviour
    {
        [SerializeField] private CompanionOrder currentOrder = CompanionOrder.Follow;

        public CompanionOrder CurrentOrder => currentOrder;

        /// <summary>Only meaningful while CurrentOrder == AttackTarget.</summary>
        public IDamageable AttackTarget { get; private set; }

        public void SetOrder(CompanionOrder order)
        {
            currentOrder = order;
            if (order != CompanionOrder.AttackTarget)
            {
                AttackTarget = null;
            }
        }

        public void SetAttackTarget(IDamageable target)
        {
            AttackTarget = target;
            currentOrder = CompanionOrder.AttackTarget;
        }
    }
}
