using Game.AI;
using Game.Characters;

namespace Game.AI.States
{
    /// <summary>
    /// Terminal state — the owner (EnemyController/NpcController) subscribes to
    /// HealthComponent.Died and calls StateMachine.ChangeState(deadState)
    /// itself (see ai-state-machine.md); DeadState never transitions on its
    /// own. Reused as-is by Companion NPCs.
    /// </summary>
    public class DeadState : IAiState
    {
        public void Enter(AiContext context)
        {
            context.CurrentTarget = null;
            context.Sensor.GetComponent<CharacterMotor>()?.Stop();
            context.Sensor.enabled = false;
        }

        public void Tick(AiContext context, float deltaTime)
        {
        }

        public void Exit(AiContext context)
        {
        }
    }
}
