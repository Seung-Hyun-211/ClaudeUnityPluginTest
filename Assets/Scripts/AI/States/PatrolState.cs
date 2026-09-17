using UnityEngine;
using Game.AI;
using Game.Characters;

namespace Game.AI.States
{
    /// <summary>
    /// Wanders within patrolRadius of wherever it entered this state; hands
    /// off to ChaseState the moment the sensor sees a target. Reused as-is by
    /// Village NPCs' WanderBrain (see npc-roles.md) — reads only Sensor and
    /// moves via CharacterMotor on the sensor's own GameObject, never Self.
    /// </summary>
    public class PatrolState : IAiState
    {
        private const float ArrivalThreshold = 0.5f;

        private readonly IAiState chaseState;
        private readonly float patrolRadius;
        private readonly float waypointCooldown;

        private Vector3 origin;
        private Vector3 currentWaypoint;
        private float waitTimer;

        public PatrolState(IAiState chaseState, float patrolRadius = 8f, float waypointCooldown = 1.5f)
        {
            this.chaseState = chaseState;
            this.patrolRadius = patrolRadius;
            this.waypointCooldown = waypointCooldown;
        }

        public void Enter(AiContext context)
        {
            origin = context.Sensor.transform.position;
            PickNewWaypoint();
        }

        public void Tick(AiContext context, float deltaTime)
        {
            if (context.Sensor.DetectedTarget != null)
            {
                context.CurrentTarget = context.Sensor.DetectedTarget;
                context.StateMachine.ChangeState(chaseState);
                return;
            }

            var motor = context.Sensor.GetComponent<CharacterMotor>();
            var position = context.Sensor.transform.position;

            if (Vector3.Distance(position, currentWaypoint) <= ArrivalThreshold)
            {
                motor?.Stop();
                waitTimer += deltaTime;
                if (waitTimer >= waypointCooldown)
                {
                    PickNewWaypoint();
                }
                return;
            }

            motor?.MoveTo(currentWaypoint);
        }

        public void Exit(AiContext context)
        {
            context.Sensor.GetComponent<CharacterMotor>()?.Stop();
        }

        private void PickNewWaypoint()
        {
            var randomOffset = Random.insideUnitCircle * patrolRadius;
            currentWaypoint = origin + new Vector3(randomOffset.x, 0f, randomOffset.y);
            waitTimer = 0f;
        }
    }
}
