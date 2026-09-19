using System;
using UnityEngine;

namespace Game.Dialogue
{
    public sealed class DialogueEventContext
    {
        public IDialogueFlags Flags { get; }
        public GameObject Interactor { get; }

        public DialogueEventContext(IDialogueFlags flags, GameObject interactor)
        {
            Flags = flags;
            Interactor = interactor;
        }
    }

    /// <summary>
    /// Executes one <see cref="DialogueEventType"/>. New event kinds (quest
    /// offer, shop, ...) are new handlers registered on the runner - the
    /// runner never changes (open-closed). A quiet event calls onCompleted
    /// before returning; a modal one (quest window, shop) calls it later once
    /// the window closes. The bool is "accepted" and picks the next node for
    /// modal events (see DialogueNode.nextNodeIfDeclinedId); quiet events pass true.
    /// </summary>
    public interface IDialogueEventHandler
    {
        DialogueEventType EventType { get; }
        void Execute(DialogueNode node, DialogueEventContext context, Action<bool> onCompleted);
    }
}
