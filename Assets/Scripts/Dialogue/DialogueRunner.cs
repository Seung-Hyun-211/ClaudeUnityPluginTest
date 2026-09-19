using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>
    /// Plays a DialogueSequence: walks the node graph and exposes the
    /// playback state machine (Docs/기획문서_대화시네마틱구조설계.md ch.3). Pure C#
    /// with no UI or input - it raises events and is driven through
    /// Submit/Choose/Skip/Tick, which makes it testable and lets any view
    /// present it.
    /// </summary>
    public class DialogueRunner
    {
        private const int MaxAutoSteps = 1000;

        private readonly IDialogueFlags flags;
        private readonly Dictionary<DialogueEventType, IDialogueEventHandler> handlers = new();
        private readonly List<DialogueLogEntry> log = new();
        private readonly List<DialogueChoiceOption> visibleChoices = new();

        private GameObject interactor;
        private DialogueNode currentNode;
        private float waitRemaining;
        private int session;

        public DialogueRunner(IDialogueFlags flags)
        {
            this.flags = flags;
        }

        public DialogueState State { get; private set; }
        public DialogueSequence Sequence { get; private set; }
        public IReadOnlyList<DialogueLogEntry> Log => log;

        public event Action<DialogueNode> LineShown;
        public event Action<IReadOnlyList<DialogueChoiceOption>> ChoicesShown;
        public event Action Ended;

        public void RegisterHandler(IDialogueEventHandler handler) => handlers[handler.EventType] = handler;

        public bool Start(DialogueSequence sequence, GameObject interactingObject)
        {
            if (State != DialogueState.Idle || sequence == null)
            {
                return false;
            }

            Sequence = sequence;
            interactor = interactingObject;
            log.Clear();
            Enter(sequence.EntryNodeId);
            return true;
        }

        /// <summary>Line fully typed out: Playing -> WaitingForAdvance.</summary>
        public void NotifyLineFullyShown()
        {
            if (State == DialogueState.Playing)
            {
                State = DialogueState.WaitingForAdvance;
            }
        }

        /// <summary>Advance past a finished line.</summary>
        public void Submit()
        {
            if (State == DialogueState.WaitingForAdvance)
            {
                Enter(currentNode.nextNodeId);
            }
        }

        /// <param name="visibleIndex">Index into the options passed to ChoicesShown.</param>
        public void Choose(int visibleIndex)
        {
            if (State != DialogueState.ChoicePending || visibleIndex < 0 || visibleIndex >= visibleChoices.Count)
            {
                return;
            }

            Enter(visibleChoices[visibleIndex].nextNodeId);
        }

        /// <summary>Jumps straight to the end; ignored for non-skippable sequences.</summary>
        public void Skip()
        {
            if (State != DialogueState.Idle && Sequence.Skippable)
            {
                Finish();
            }
        }

        public void Tick(float deltaTime)
        {
            if (State != DialogueState.Waiting)
            {
                return;
            }

            waitRemaining -= deltaTime;
            if (waitRemaining <= 0f)
            {
                Enter(currentNode.nextNodeId);
            }
        }

        private void Enter(string nodeId)
        {
            int steps = 0;
            while (true)
            {
                if (string.IsNullOrEmpty(nodeId))
                {
                    Finish();
                    return;
                }

                if (++steps > MaxAutoSteps)
                {
                    Debug.LogError($"Dialogue [{Sequence.SequenceId}] looped through {MaxAutoSteps} nodes without stopping at a line/choice - ending it.");
                    Finish();
                    return;
                }

                if (!Sequence.TryGetNode(nodeId, out var node))
                {
                    Debug.LogWarning($"Dialogue [{Sequence.SequenceId}] has no node [{nodeId}] - ending it.");
                    Finish();
                    return;
                }

                currentNode = node;

                switch (node.type)
                {
                    case DialogueNodeType.Line:
                        State = DialogueState.Playing;
                        log.Add(new DialogueLogEntry(node.speakerName, node.text));
                        LineShown?.Invoke(node);
                        return;

                    case DialogueNodeType.Choice:
                        if (!ShowChoices(node))
                        {
                            Finish();
                        }
                        return;

                    case DialogueNodeType.Branch:
                        nodeId = node.condition.IsMet(flags) ? node.nextNodeId : node.nextNodeIfFalseId;
                        continue;

                    case DialogueNodeType.Wait:
                        if (node.duration <= 0f)
                        {
                            nodeId = node.nextNodeId;
                            continue;
                        }
                        waitRemaining = node.duration;
                        State = DialogueState.Waiting;
                        return;

                    case DialogueNodeType.Event:
                        if (!TryRunEvent(node, out nodeId))
                        {
                            return;
                        }
                        continue;

                    case DialogueNodeType.End:
                    default:
                        Finish();
                        return;
                }
            }
        }

        private bool ShowChoices(DialogueNode node)
        {
            visibleChoices.Clear();
            if (node.choices != null)
            {
                foreach (var option in node.choices)
                {
                    if (option.condition.IsMet(flags))
                    {
                        visibleChoices.Add(option);
                    }
                }
            }

            if (visibleChoices.Count == 0)
            {
                Debug.LogWarning($"Dialogue [{Sequence.SequenceId}] choice node [{node.id}] has no visible options - ending it.");
                return false;
            }

            State = DialogueState.ChoicePending;
            ChoicesShown?.Invoke(visibleChoices);
            return true;
        }

        /// <returns>True with the next node id when the event finished synchronously; false while a modal handler is still pending (State = ModalPending).</returns>
        private bool TryRunEvent(DialogueNode node, out string nextNodeId)
        {
            if (!handlers.TryGetValue(node.eventType, out var handler))
            {
                Debug.LogWarning($"Dialogue [{Sequence.SequenceId}]: no handler registered for event {node.eventType} (node [{node.id}]) - skipping it.");
                nextNodeId = node.nextNodeId;
                return true;
            }

            State = DialogueState.ModalPending;
            int token = session;
            bool executing = true;
            bool? syncResult = null;

            handler.Execute(node, new DialogueEventContext(flags, interactor), accepted =>
            {
                if (token != session)
                {
                    return; // sequence ended/skipped while the modal was open
                }

                if (executing)
                {
                    syncResult = accepted;
                    return;
                }

                Enter(NextAfterEvent(node, accepted));
            });

            executing = false;

            if (syncResult.HasValue)
            {
                nextNodeId = NextAfterEvent(node, syncResult.Value);
                return true;
            }

            nextNodeId = null;
            return false;
        }

        private static string NextAfterEvent(DialogueNode node, bool accepted)
        {
            return !accepted && !string.IsNullOrEmpty(node.nextNodeIfDeclinedId)
                ? node.nextNodeIfDeclinedId
                : node.nextNodeId;
        }

        private void Finish()
        {
            session++;
            State = DialogueState.Idle;
            currentNode = null;
            Ended?.Invoke();
        }
    }
}
