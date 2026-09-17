using Game.Characters.Enemy;
using Game.Combat;

namespace Game.AI
{
    /// <summary>
    /// Blackboard shared by every IAiState so state classes never need to cast
    /// or look up MonoBehaviours themselves (see ai-state-machine.md). Common
    /// states only ever read Sensor/CurrentTarget/StateMachine — Self exists
    /// for tier-specific states (Elite/Boss/EnemyController-only behaviour).
    /// </summary>
    public class AiContext
    {
        public EnemyController Self { get; }
        public AiSensor Sensor { get; }
        public IDamageable CurrentTarget { get; set; }
        public AiStateMachine StateMachine { get; }

        public AiContext(EnemyController self, AiSensor sensor, AiStateMachine stateMachine)
        {
            Self = self;
            Sensor = sensor;
            StateMachine = stateMachine;
        }
    }
}
