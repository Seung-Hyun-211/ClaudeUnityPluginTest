using System;
using Game.AI;
using Game.Characters;

namespace Game.AI.States
{
    /// <summary>
    /// Stands in place and ignores Sensor entirely, per npc-roles.md's state
    /// table ("제자리 대기, Sensor 결과를 무시"), until told to resume
    /// following. Like FollowState, it takes an isHoldRequested delegate
    /// instead of referencing an NPC-specific order type directly, so it
    /// stays a generic, reusable Game.AI.States class.
    /// </summary>
    public class HoldState : IAiState
    {
        private readonly IAiState followState;
        private readonly Func<bool> isHoldRequested;

        public HoldState(IAiState followState, Func<bool> isHoldRequested = null)
        {
            this.followState = followState;
            this.isHoldRequested = isHoldRequested;
        }

        public void Enter(AiContext context)
        {
            context.Sensor.GetComponent<CharacterMotor>()?.Stop();
        }

        public void Tick(AiContext context, float deltaTime)
        {
            if (isHoldRequested != null && !isHoldRequested())
            {
                context.StateMachine.ChangeState(followState);
            }
        }

        public void Exit(AiContext context)
        {
        }
    }
}
