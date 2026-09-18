using Game.AI;

namespace Game.Characters.Npc
{
    /// <summary>
    /// Construction-order helper, not a behaviour. ChaseState/AttackState and
    /// this track's FollowState/HoldState all take their transition targets as
    /// constructor arguments with no way to change them afterwards, which
    /// makes a genuinely circular graph (Follow needs Chase, Chase needs
    /// Attack, Attack needs Chase, Hold needs Follow, Follow needs Hold)
    /// impossible to build in one pass without either mutating those existing
    /// classes (out of scope — see the implementation report) or a level of
    /// indirection.
    ///
    /// This is that indirection: a placeholder IAiState that forwards every
    /// call to whatever Target gets assigned once the whole graph exists. It
    /// is only ever touched during graph construction (see CompanionBrain and
    /// WanderBrain) — by the time Enter/Tick/Exit are actually invoked at
    /// runtime, every proxy's Target has already been assigned.
    /// </summary>
    internal sealed class ForwardingAiState : IAiState
    {
        public IAiState Target { get; set; }

        public void Enter(AiContext context) => Target.Enter(context);
        public void Tick(AiContext context, float deltaTime) => Target.Tick(context, deltaTime);
        public void Exit(AiContext context) => Target.Exit(context);
    }
}
