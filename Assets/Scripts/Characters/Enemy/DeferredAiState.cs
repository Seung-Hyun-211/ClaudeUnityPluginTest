using Game.AI;

namespace Game.Characters.Enemy
{
    /// <summary>
    /// Stands in for an IAiState that does not exist yet at construction time.
    /// Every brain in this namespace has to wire a small cycle of states that
    /// each reference the others through readonly constructor parameters
    /// (e.g. ChaseState needs the AttackState instance and vice versa,
    /// PatrolState needs ChaseState and vice versa) - there is no valid
    /// construction order for a true cycle of immutable objects. This class
    /// breaks the cycle: pass a DeferredAiState wherever the "not built yet"
    /// state is needed, finish building the real graph, then set Target once.
    /// Target is always assigned before AiStateMachine can ever reach this
    /// instance (brains assign it synchronously right after the real state is
    /// constructed, before BuildInitialState returns), so callers never see a
    /// null Target. Purely a construction-time detail internal to how brains
    /// in this file build their graphs - AiStateMachine/IAiState (Game.AI)
    /// never need to know it exists.
    /// </summary>
    internal sealed class DeferredAiState : IAiState
    {
        public IAiState Target { get; set; }

        public void Enter(AiContext context) => Target.Enter(context);
        public void Tick(AiContext context, float deltaTime) => Target.Tick(context, deltaTime);
        public void Exit(AiContext context) => Target.Exit(context);
    }
}
