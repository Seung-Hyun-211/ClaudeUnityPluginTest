namespace Game.Persistence
{
    /// <summary>
    /// Tags "why" a save was requested. Used for logging and for the
    /// SceneTransition/immediate-write policy split in SaveGameService (see
    /// scene-and-persistence-system.md §4-1/§4-2). New trigger kinds are
    /// added here without touching SaveGameService (open-closed).
    /// </summary>
    public enum SaveTriggerReason
    {
        /// <summary>Automatic save attached to a Lobby/Combat scene transition; the actual disk write happens once the Loading scene is up.</summary>
        SceneTransition,

        /// <summary>Player interacted with an in-world save point.</summary>
        ManualSavePoint,

        /// <summary>A quest or event completed.</summary>
        QuestCompleted,

        /// <summary>The application is quitting.</summary>
        AppQuit,

        /// <summary>Any other designer/script-driven save request.</summary>
        Custom
    }
}
