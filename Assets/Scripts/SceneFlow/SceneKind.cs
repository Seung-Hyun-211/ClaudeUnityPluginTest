namespace Game.SceneFlow
{
    /// <summary>
    /// The set of "screens" the game can be showing. Boot is not a screen —
    /// it is an additive container that is always loaded (see
    /// scene-and-persistence-system.md §1) — the other four are mutually
    /// exclusive and loaded Single.
    /// </summary>
    public enum SceneKind
    {
        Boot,
        Title,
        Loading,
        Lobby,
        Combat
    }
}
