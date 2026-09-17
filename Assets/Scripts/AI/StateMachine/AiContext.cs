using UnityEngine;
using Game.Combat;

namespace Game.AI
{
    /// <summary>
    /// Blackboard shared by every IAiState so state classes never need to cast
    /// or look up MonoBehaviours themselves (see ai-state-machine.md). Common
    /// states only ever read Sensor/CurrentTarget/StateMachine — Self exists
    /// for tier-specific states (Elite/Boss-only behaviour) that need to reach
    /// back to their owner. Typed as GameObject (matching DamageInfo.Source,
    /// WeaponUseContext.Wielder) rather than a specific controller type so the
    /// same AiContext/AiStateMachine works for Enemy AND companion NPCs
    /// (see npc-roles.md) — the owner can GetComponent whatever it needs.
    /// </summary>
    public class AiContext
    {
        public GameObject Self { get; }
        public AiSensor Sensor { get; }
        public IDamageable CurrentTarget { get; set; }
        public AiStateMachine StateMachine { get; }

        public AiContext(GameObject self, AiSensor sensor, AiStateMachine stateMachine)
        {
            Self = self;
            Sensor = sensor;
            StateMachine = stateMachine;
        }
    }
}
