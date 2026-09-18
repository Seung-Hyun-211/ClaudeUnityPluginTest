namespace Game.Persistence
{
    /// <summary>
    /// Narrow contract for anything that wants to trigger a save. Trigger
    /// sites (save points, quest completion, app quit) depend only on this
    /// interface, never on SaveGameService itself or how/when it actually
    /// writes to disk (interface segregation) — see
    /// scene-and-persistence-system.md §4-1/§4-3.
    /// </summary>
    public interface ISaveRequestSink
    {
        void RequestSave(SaveTriggerReason reason);
    }
}
