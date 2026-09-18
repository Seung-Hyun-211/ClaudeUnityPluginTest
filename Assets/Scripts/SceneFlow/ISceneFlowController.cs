namespace Game.SceneFlow
{
    /// <summary>
    /// Narrow contract for requesting a scene transition. Callers (UI
    /// buttons, gameplay triggers, save/load flow) depend on this interface
    /// rather than the concrete <see cref="SceneFlowController"/> MonoBehaviour
    /// (interface segregation) and never call SceneManager directly, which
    /// keeps "every transition goes through Loading" enforceable in one place
    /// (see scene-and-persistence-system.md §2).
    /// </summary>
    public interface ISceneFlowController
    {
        void RequestTransition(SceneKind target);
    }
}
