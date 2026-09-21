namespace Game.Building
{
    /// <summary>
    /// Implemented by a piece component that has state of its own (a door: open
    /// or closed, which way it swings). StructureManager only talks to this
    /// interface when a piece is placed, saved or restored, so a new kind of
    /// piece with state needs no change there.
    /// </summary>
    public interface IBuildPieceState
    {
        /// <summary>A freshly placed piece; <paramref name="flipped"/> is the player choice of variant (a door swing side).</summary>
        void Initialize(bool flipped);

        void Capture(PieceRecord record);

        void Restore(PieceRecord record);
    }
}
