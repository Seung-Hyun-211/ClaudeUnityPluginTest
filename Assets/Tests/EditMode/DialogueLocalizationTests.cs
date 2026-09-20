using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Localization;
using Game.Dialogue;

namespace Game.Tests
{
    public class DialogueLocalizationTests : TestBase
    {
        // Stands in for the string tables: wraps whatever the node authored so the tests can see what was resolved.
        private sealed class TaggingResolver : IDialogueTextResolver
        {
            public int Calls;

            public string Resolve(LocalizedString localized, string authored)
            {
                Calls++;
                return authored == null ? null : "[" + authored + "]";
            }
        }

        private sealed class Flags : IDialogueFlags
        {
            public bool GetFlag(string name) => false;
            public void SetFlag(string name, bool value) { }
        }

        private DialogueSequence Sequence(params DialogueNode[] nodes)
        {
            var sequence = NewAsset<DialogueSequence>();
            Set(sequence, "sequenceId", "loc");
            Set(sequence, "skippable", true);
            Set(sequence, "entryNodeId", nodes[0].id);
            Set(sequence, "nodes", nodes);
            return sequence;
        }

        [Test]
        public void Runner_ResolvesSpeakerTextAndLog_ThroughTheResolver()
        {
            var runner = new DialogueRunner(new Flags(), new TaggingResolver());
            var shown = new List<DialogueLine>();
            runner.LineShown += shown.Add;

            runner.Start(Sequence(new DialogueNode { id = "a", type = DialogueNodeType.Line, speakerName = "촌장", text = "안녕" }), null);

            Assert.AreEqual("[촌장]", shown[0].Speaker);
            Assert.AreEqual("[안녕]", shown[0].Text);
            Assert.AreEqual("[안녕]", runner.Log[0].Text);
            Assert.AreEqual("[촌장]", runner.Log[0].Speaker);
        }

        [Test]
        public void Runner_ResolvesChoiceLabels_ThroughTheResolver()
        {
            var runner = new DialogueRunner(new Flags(), new TaggingResolver());
            IReadOnlyList<string> labels = null;
            runner.ChoicesShown += l => labels = l;

            runner.Start(Sequence(new DialogueNode
            {
                id = "c",
                type = DialogueNodeType.Choice,
                choices = new[]
                {
                    new DialogueChoiceOption { text = "예", nextNodeId = null },
                    new DialogueChoiceOption { text = "아니오", nextNodeId = null },
                },
            }), null);

            CollectionAssert.AreEqual(new[] { "[예]", "[아니오]" }, labels);
        }

        [Test]
        public void Runner_WithoutAResolver_ShowsTheAuthoredText()
        {
            var runner = new DialogueRunner(new Flags());
            var shown = new List<DialogueLine>();
            runner.LineShown += shown.Add;

            runner.Start(Sequence(new DialogueNode { id = "a", type = DialogueNodeType.Line, speakerName = "촌장", text = "<wave>안녕</wave>" }), null);

            Assert.AreEqual("촌장", shown[0].Speaker);
            Assert.AreEqual("<wave>안녕</wave>", shown[0].Text, "the runner passes markup through untouched; the view parses it");
        }

        [Test]
        public void LocalizationResolver_WithoutAReference_FallsBackToTheAuthoredText()
        {
            var resolver = new LocalizationTextResolver();

            Assert.AreEqual("원문", resolver.Resolve(null, "원문"));
            Assert.AreEqual("원문", resolver.Resolve(new LocalizedString(), "원문"));
        }

        [Test]
        public void Validator_TranslationWithTheSameTags_IsClean_EvenWhenTheOrderDiffers()
        {
            var problems = DialogueMarkupValidator.Compare(
                "<color=#4aa3ff>그리움</color>과 <wave>호기심</wave>을 안고",
                "Carrying <wave>curiosity</wave> and <color=#4aa3ff>longing</color>",
                TextEffectRegistry.CreateDefault());

            Assert.IsEmpty(problems);
        }

        [Test]
        public void Validator_ReportsALostTag_AnInventedTag_AndADifferentArgument()
        {
            var registry = TextEffectRegistry.CreateDefault();

            Assert.IsNotEmpty(DialogueMarkupValidator.Compare("<wave>a</wave>", "a", registry), "lost tag");
            Assert.IsNotEmpty(DialogueMarkupValidator.Compare("a", "<wave>a</wave>", registry), "invented tag");
            Assert.IsNotEmpty(DialogueMarkupValidator.Compare("<color=red>a</color>", "<color=blue>a</color>", registry), "argument changed");
        }

        [Test]
        public void Validator_ReportsPauseCountDifferences_AndBrokenMarkupInTheTranslation()
        {
            var registry = TextEffectRegistry.CreateDefault();

            Assert.IsNotEmpty(DialogueMarkupValidator.Compare("a<pause=1>b", "ab", registry));
            Assert.IsNotEmpty(DialogueMarkupValidator.Compare("<wave>a</wave>", "<wave>a", registry), "unclosed tag in the translation");
        }

        [Test]
        public void Validator_Check_ReportsWarningsOfASingleText()
        {
            Assert.IsEmpty(DialogueMarkupValidator.Check("<wave>a</wave>", TextEffectRegistry.CreateDefault()));
            Assert.IsNotEmpty(DialogueMarkupValidator.Check("<wave>a", TextEffectRegistry.CreateDefault()));
        }

        [Test]
        public void SpanTag_IsTheNormalisedOpeningTag()
        {
            var parsed = DialogueMarkup.Parse("<COLOR=#4aa3ff>a</color><shake amp=2 >b</shake>", TextEffectRegistry.CreateDefault());

            Assert.AreEqual("color=#4aa3ff", parsed.Spans[0].Tag);
            Assert.AreEqual("shake amp=2", parsed.Spans[1].Tag);
        }
    }
}
