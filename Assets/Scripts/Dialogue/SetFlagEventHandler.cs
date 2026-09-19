using System;

namespace Game.Dialogue
{
    public class SetFlagEventHandler : IDialogueEventHandler
    {
        public DialogueEventType EventType => DialogueEventType.SetFlag;

        public void Execute(DialogueNode node, DialogueEventContext context, Action<bool> onCompleted)
        {
            context.Flags.SetFlag(node.eventParam, node.eventFlagValue);
            onCompleted(true);
        }
    }
}
