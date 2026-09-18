namespace Game.AI
{
    /// <summary>
    /// Construction-order helper, not a behaviour. Several of the built-in
    /// Game.AI.States classes (ChaseState/AttackState/PatrolState/FollowState/
    /// HoldState, and any tier-specific state built on the same pattern, e.g.
    /// Game.Characters.Enemy's BossPhaseState graph) take their transition
    /// targets as readonly constructor arguments, which makes a genuinely
    /// circular state graph (A needs B, B needs A) impossible to build in one
    /// pass without mutating those classes.
    ///
    /// This is that indirection: pass a ForwardingAiState wherever the
    /// "not built yet" state is needed, finish building the real graph, then
    /// assign Target once — every brain does this synchronously inside
    /// BuildInitialState, before AiStateMachine can ever tick the proxy, so
    /// Target is never observed null at runtime.
    ///
    /// Lives in Game.AI (not a specific Enemy/Npc namespace) because building
    /// a cyclic state graph is a generic IAiBrain concern, not something tied
    /// to any one actor kind — the Enemy and Companion NPC tracks each
    /// independently needed this exact class before it was consolidated here.
    /// </summary>
    public sealed class ForwardingAiState : IAiState
    {
        public IAiState Target { get; set; }

        public void Enter(AiContext context) => Target.Enter(context);
        public void Tick(AiContext context, float deltaTime) => Target.Tick(context, deltaTime);
        public void Exit(AiContext context) => Target.Exit(context);
    }
}
