namespace Game.Building
{
    /// <summary>Why the piece under the crosshair can or cannot be placed (documents/building-system.md 7).</summary>
    public enum PlacementStatus
    {
        Ok,
        NoTarget,
        NotAvailable,
        OutOfZone,
        Occupied,
        Unsupported,
        Blocked,
        NoMaterials,
        TooFar,
    }
}
