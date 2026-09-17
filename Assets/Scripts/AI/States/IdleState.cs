using Game.AI;

namespace Game.AI.States
{
    /// <summary>
    /// Waits in place; hands off to PatrolState after idleDuration, or straight
    /// to ChaseState if the sensor already sees a target. Reused as-is by
    /// Village NPCs' WanderBrain (see npc-roles.md) — reads only Sensor.
    /// </summary>
    public class IdleState : IAiState
    {
        private readonly IAiState patrolState;
        private readonly IAiState chaseState;
        private readonly float idleDuration;

        private float elapsed;

        public IdleState(IAiState patrolState, IAiState chaseState, float idleDuration = 2f)
        {
            this.patrolState = patrolState;
            this.chaseState = chaseState;
            this.idleDuration = idleDuration;
        }

        public void Enter(AiContext context)
        {
            elapsed = 0f;
        }

        public void Tick(AiContext context, float deltaTime)
        {
            if (context.Sensor.DetectedTarget != null)
            {
                context.CurrentTarget = context.Sensor.DetectedTarget;
                context.StateMachine.ChangeState(chaseState);
                return;
            }

            elapsed += deltaTime;
            if (elapsed >= idleDuration)
            {
                context.StateMachine.ChangeState(patrolState);
            }
        }

        public void Exit(AiContext context)
        {
        }
    }
}
