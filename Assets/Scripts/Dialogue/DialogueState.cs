namespace Game.Dialogue
{
    /// <summary>Playback states from Docs/기획문서_대화시네마틱구조설계.md ch.3, plus Waiting for the Wait node.</summary>
    public enum DialogueState
    {
        Idle,
        Playing,
        WaitingForAdvance,
        ChoicePending,
        ModalPending,
        Waiting
    }
}
