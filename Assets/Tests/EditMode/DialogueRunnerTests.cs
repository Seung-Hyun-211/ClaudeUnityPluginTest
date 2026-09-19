using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Game.Dialogue;

namespace Game.Tests
{
    public class DialogueRunnerTests : TestBase
    {
        private class Flags : IDialogueFlags
        {
            private readonly HashSet<string> set = new();
            public bool GetFlag(string name) => set.Contains(name);
            public void SetFlag(string name, bool value) { if (value) set.Add(name); else set.Remove(name); }
        }

        private class ModalHandler : IDialogueEventHandler
        {
            public DialogueEventType EventType => DialogueEventType.OpenShop;
            public Action<bool> Complete;
            public int Executions;

            public void Execute(DialogueNode node, DialogueEventContext context, Action<bool> onCompleted)
            {
                Executions++;
                Complete = onCompleted;
            }
        }

        private static DialogueNode Line(string id, string text, string next) =>
            new() { id = id, type = DialogueNodeType.Line, speakerName = "npc", text = text, nextNodeId = next };

        private static DialogueNode Choice(string id, params DialogueChoiceOption[] options) =>
            new() { id = id, type = DialogueNodeType.Choice, choices = options };

        private static DialogueChoiceOption Option(string text, string next, string requiredFlag = null) =>
            new() { text = text, nextNodeId = next, condition = new DialogueCondition { flag = requiredFlag, expected = true } };

        private static DialogueNode Branch(string id, string flag, string ifTrue, string ifFalse) =>
            new() { id = id, type = DialogueNodeType.Branch, condition = new DialogueCondition { flag = flag, expected = true }, nextNodeId = ifTrue, nextNodeIfFalseId = ifFalse };

        private static DialogueNode SetFlag(string id, string flag, string next) =>
            new() { id = id, type = DialogueNodeType.Event, eventType = DialogueEventType.SetFlag, eventParam = flag, eventFlagValue = true, nextNodeId = next };

        private static DialogueNode Shop(string id, string next, string declined = null) =>
            new() { id = id, type = DialogueNodeType.Event, eventType = DialogueEventType.OpenShop, nextNodeId = next, nextNodeIfDeclinedId = declined };

        private static DialogueNode Wait(string id, float seconds, string next) =>
            new() { id = id, type = DialogueNodeType.Wait, duration = seconds, nextNodeId = next };

        private DialogueSequence Sequence(string entry, bool skippable, params DialogueNode[] nodes)
        {
            var sequence = NewAsset<DialogueSequence>();
            Set(sequence, "sequenceId", "test");
            Set(sequence, "skippable", skippable);
            Set(sequence, "entryNodeId", entry);
            Set(sequence, "nodes", nodes);
            return sequence;
        }

        private class Run
        {
            public readonly Flags Flags = new();
            public readonly DialogueRunner Runner;
            public readonly List<string> Lines = new();
            public IReadOnlyList<DialogueChoiceOption> Choices;
            public int Ended;

            public Run()
            {
                Runner = new DialogueRunner(Flags);
                Runner.RegisterHandler(new SetFlagEventHandler());
                Runner.LineShown += node => Lines.Add(node.text);
                Runner.ChoicesShown += options => Choices = options;
                Runner.Ended += () => Ended++;
            }

            public void Advance()
            {
                Runner.NotifyLineFullyShown();
                Runner.Submit();
            }
        }

        [Test]
        public void Lines_PlayInOrderAndEndTheSequence()
        {
            var run = new Run();
            run.Runner.Start(Sequence("a", true, Line("a", "first", "b"), Line("b", "second", null)), null);

            Assert.AreEqual(DialogueState.Playing, run.Runner.State);
            run.Advance();
            Assert.AreEqual("second", run.Lines[1]);
            run.Advance();

            Assert.AreEqual(DialogueState.Idle, run.Runner.State);
            Assert.AreEqual(1, run.Ended);
            Assert.AreEqual(2, run.Runner.Log.Count);
        }

        [Test]
        public void Submit_WhileStillTyping_DoesNotAdvance()
        {
            var run = new Run();
            run.Runner.Start(Sequence("a", true, Line("a", "first", "b"), Line("b", "second", null)), null);

            run.Runner.Submit();

            Assert.AreEqual(DialogueState.Playing, run.Runner.State);
            Assert.AreEqual(1, run.Lines.Count);

            run.Runner.NotifyLineFullyShown();
            Assert.AreEqual(DialogueState.WaitingForAdvance, run.Runner.State);
        }

        [Test]
        public void Start_WhileRunningOrWithoutSequence_IsRejected()
        {
            var run = new Run();
            var sequence = Sequence("a", true, Line("a", "x", null));

            Assert.IsFalse(run.Runner.Start(null, null));
            Assert.IsTrue(run.Runner.Start(sequence, null));
            Assert.IsFalse(run.Runner.Start(sequence, null));
        }

        [Test]
        public void Choice_HidesOptionsWhoseConditionIsNotMet_AndChooseUsesVisibleIndices()
        {
            var run = new Run();
            run.Runner.Start(Sequence("c", true,
                Choice("c", Option("always", "x"), Option("secret", "y", "knows_secret"), Option("also always", "z")),
                Line("x", "X", null), Line("y", "Y", null), Line("z", "Z", null)), null);

            Assert.AreEqual(DialogueState.ChoicePending, run.Runner.State);
            Assert.AreEqual(2, run.Choices.Count);

            run.Runner.Choose(1);   // the second VISIBLE option, i.e. "also always"

            Assert.AreEqual("Z", run.Lines[0]);
        }

        [Test]
        public void Choice_ShowsAConditionalOptionOnceItsFlagIsSet()
        {
            var run = new Run();
            run.Flags.SetFlag("knows_secret", true);
            run.Runner.Start(Sequence("c", true, Choice("c", Option("always", "x"), Option("secret", "x", "knows_secret")), Line("x", "X", null)), null);

            Assert.AreEqual(2, run.Choices.Count);
        }

        [Test]
        public void Choose_OutOfRangeOrWrongState_IsIgnored()
        {
            var run = new Run();
            run.Runner.Start(Sequence("c", true, Choice("c", Option("only", "x")), Line("x", "X", null)), null);

            run.Runner.Choose(5);
            run.Runner.Choose(-1);
            Assert.AreEqual(DialogueState.ChoicePending, run.Runner.State);

            run.Runner.Choose(0);
            run.Runner.Choose(0);   // no longer a choice state
            Assert.AreEqual(1, run.Lines.Count);
        }

        [Test]
        public void Branch_FollowsTheFlag()
        {
            var sequence = Sequence("b", true, Branch("b", "met", "again", "first"), Line("first", "hello", null), Line("again", "welcome back", null));

            var fresh = new Run();
            fresh.Runner.Start(sequence, null);
            Assert.AreEqual("hello", fresh.Lines[0]);

            var returning = new Run();
            returning.Flags.SetFlag("met", true);
            returning.Runner.Start(sequence, null);
            Assert.AreEqual("welcome back", returning.Lines[0]);
        }

        [Test]
        public void SetFlagEvent_ChangesFlagsAndContinuesWithoutStopping()
        {
            var run = new Run();
            run.Runner.Start(Sequence("e", true, SetFlag("e", "done", "l"), Line("l", "after", null)), null);

            Assert.IsTrue(run.Flags.GetFlag("done"));
            Assert.AreEqual("after", run.Lines[0]);
        }

        [Test]
        public void EventWithoutARegisteredHandler_IsSkippedWithAWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("no handler registered"));
            var run = new Run();

            run.Runner.Start(Sequence("e", true, Shop("e", "l"), Line("l", "after", null)), null);

            Assert.AreEqual("after", run.Lines[0]);
        }

        [Test]
        public void ModalEvent_PausesUntilTheHandlerCompletes_ThenFollowsAcceptedOrDeclined()
        {
            var nodes = new[] { Shop("shop", "bought", "declined"), Line("bought", "thanks", null), Line("declined", "maybe later", null) };

            var accepted = new Run();
            var handlerA = new ModalHandler();
            accepted.Runner.RegisterHandler(handlerA);
            accepted.Runner.Start(Sequence("shop", true, nodes), null);
            Assert.AreEqual(DialogueState.ModalPending, accepted.Runner.State);
            Assert.AreEqual(0, accepted.Lines.Count);
            handlerA.Complete(true);
            Assert.AreEqual("thanks", accepted.Lines[0]);

            var declined = new Run();
            var handlerB = new ModalHandler();
            declined.Runner.RegisterHandler(handlerB);
            declined.Runner.Start(Sequence("shop", true, nodes), null);
            handlerB.Complete(false);
            Assert.AreEqual("maybe later", declined.Lines[0]);
        }

        [Test]
        public void ModalEvent_CompletingAfterTheSequenceWasSkipped_IsIgnored()
        {
            var run = new Run();
            var handler = new ModalHandler();
            run.Runner.RegisterHandler(handler);
            run.Runner.Start(Sequence("shop", true, Shop("shop", "l"), Line("l", "late", null)), null);

            run.Runner.Skip();
            handler.Complete(true);

            Assert.AreEqual(0, run.Lines.Count);
            Assert.AreEqual(DialogueState.Idle, run.Runner.State);
        }

        [Test]
        public void Skip_EndsASkippableSequence_ButIsIgnoredWhenNotSkippable()
        {
            var skippable = new Run();
            skippable.Runner.Start(Sequence("a", true, Line("a", "x", "b"), Line("b", "y", null)), null);
            skippable.Runner.Skip();
            Assert.AreEqual(DialogueState.Idle, skippable.Runner.State);
            Assert.AreEqual(1, skippable.Ended);

            var locked = new Run();
            locked.Runner.Start(Sequence("a", false, Line("a", "x", "b"), Line("b", "y", null)), null);
            locked.Runner.Skip();
            Assert.AreEqual(DialogueState.Playing, locked.Runner.State);
            Assert.AreEqual(0, locked.Ended);
        }

        [Test]
        public void Wait_HoldsUntilEnoughTimeHasPassed()
        {
            var run = new Run();
            run.Runner.Start(Sequence("w", true, Wait("w", 1f, "l"), Line("l", "after", null)), null);

            Assert.AreEqual(DialogueState.Waiting, run.Runner.State);
            run.Runner.Tick(0.6f);
            Assert.AreEqual(DialogueState.Waiting, run.Runner.State);
            run.Runner.Tick(0.6f);
            Assert.AreEqual("after", run.Lines[0]);
        }

        [Test]
        public void MissingNode_EndsTheSequenceWithAWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("has no node"));
            var run = new Run();

            run.Runner.Start(Sequence("a", true, Line("a", "x", "does_not_exist")), null);
            run.Advance();

            Assert.AreEqual(DialogueState.Idle, run.Runner.State);
            Assert.AreEqual(1, run.Ended);
        }

        [Test]
        public void EndlessBranchLoop_IsCutOffWithAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("looped through"));
            var run = new Run();

            run.Runner.Start(Sequence("a", true, Branch("a", null, "b", "b"), Branch("b", null, "a", "a")), null);

            Assert.AreEqual(DialogueState.Idle, run.Runner.State);
            Assert.AreEqual(1, run.Ended);
        }

        [Test]
        public void ChoiceWithNoVisibleOptions_EndsWithAWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("no visible options"));
            var run = new Run();

            run.Runner.Start(Sequence("c", true, Choice("c", Option("hidden", "x", "never_set")), Line("x", "X", null)), null);

            Assert.AreEqual(DialogueState.Idle, run.Runner.State);
        }
    }

    public class DialogueFlagStoreTests : TestBase
    {
        [Test]
        public void SetFlag_GetFlag_AndChangedEvent()
        {
            var store = AddComponent<DialogueFlagStore>();
            int changes = 0;
            store.Changed += () => changes++;

            store.SetFlag("met", true);
            store.SetFlag("met", true);   // no change
            Assert.IsTrue(store.GetFlag("met"));
            Assert.IsFalse(store.GetFlag("other"));
            Assert.AreEqual(1, changes);

            store.SetFlag("met", false);
            Assert.IsFalse(store.GetFlag("met"));
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void SaveState_RoundTripsThroughJson()
        {
            var original = AddComponent<DialogueFlagStore>();
            original.SetFlag("met_elder", true);
            original.SetFlag("got_reward", true);
            string json = JsonUtility.ToJson(original.CaptureState());

            var restored = AddComponent<DialogueFlagStore>();
            restored.SetFlag("stale", true);
            restored.RestoreState(json);

            Assert.IsTrue(restored.GetFlag("met_elder"));
            Assert.IsTrue(restored.GetFlag("got_reward"));
            Assert.IsFalse(restored.GetFlag("stale"), "restoring replaces the current flags");
        }

        [Test]
        public void Clear_RemovesAllFlags()
        {
            var store = AddComponent<DialogueFlagStore>();
            store.SetFlag("a", true);

            store.Clear();

            Assert.AreEqual(0, store.SetFlags.Count);
        }
    }
}
