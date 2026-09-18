namespace Game.Persistence
{
    /// <summary>
    /// Implemented by any subsystem that owns data which must survive
    /// restarting the game (see scene-and-persistence-system.md §4-1).
    /// SaveGameService never knows about concrete subsystems, only this
    /// interface (interface segregation + open-closed) — new save-worthy
    /// systems register an implementation without touching SaveGameService.
    ///
    /// Note on <see cref="CaptureState"/>/<see cref="RestoreState"/>: the
    /// object exchanged here is expected to be a plain, provider-owned POCO
    /// marked [Serializable]. SaveGameService serializes it with
    /// JsonUtility.ToJson(object) (which resolves the concrete type at
    /// runtime, so this works without SaveGameService knowing the type) and
    /// treats the result as an opaque blob. On load, the value handed back
    /// to RestoreState is the raw JSON string produced for this SaveKey —
    /// the provider is the only party that knows its own state type, so it
    /// is responsible for JsonUtility.FromJson&lt;TState&gt;((string)state)
    /// itself.
    /// </summary>
    public interface ISaveDataProvider
    {
        /// <summary>Stable identifier for this provider's slice of the save file, e.g. "player.vitals".</summary>
        string SaveKey { get; }

        /// <summary>Returns a serializable snapshot of this provider's current state.</summary>
        object CaptureState();

        /// <summary>Applies a previously captured snapshot (see class remarks for the actual runtime type).</summary>
        void RestoreState(object state);
    }
}
