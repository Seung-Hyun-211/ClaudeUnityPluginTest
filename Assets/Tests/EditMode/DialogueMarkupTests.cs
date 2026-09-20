using System.Linq;
using NUnit.Framework;
using Game.Dialogue;

namespace Game.Tests
{
    public class DialogueMarkupTests
    {
        private static ParsedText Parse(string source) => DialogueMarkup.Parse(source, TextEffectRegistry.CreateDefault());

        [Test]
        public void PlainText_HasNoSpansAndIsUnchanged()
        {
            var parsed = Parse("그냥 대사입니다.");

            Assert.AreEqual("그냥 대사입니다.", parsed.Plain);
            Assert.IsFalse(parsed.HasEffects);
            Assert.IsEmpty(parsed.Warnings);
        }

        [Test]
        public void NullSource_IsEmpty()
        {
            Assert.AreEqual(string.Empty, Parse(null).Plain);
        }

        [Test]
        public void Tag_IsStrippedAndSpanIndicesPointIntoPlainText()
        {
            var parsed = Parse("어른이 된 뒤, <wave>태어난 곳</wave>을 떠났다.");

            Assert.AreEqual("어른이 된 뒤, 태어난 곳을 떠났다.", parsed.Plain);
            Assert.AreEqual(1, parsed.Spans.Count);
            var span = parsed.Spans[0];
            Assert.AreEqual("태어난 곳", parsed.Plain.Substring(span.Start, span.Length));
            Assert.IsInstanceOf<WaveEffect>(span.Effect);
        }

        [Test]
        public void Nested_TagsProduceOverlappingSpans_InOpeningOrder()
        {
            var parsed = Parse("<color=#ff0000>가<sway>나</sway>다</color>");

            Assert.AreEqual("가나다", parsed.Plain);
            Assert.AreEqual(2, parsed.Spans.Count);
            Assert.IsInstanceOf<ColorEffect>(parsed.Spans[0].Effect);
            Assert.AreEqual((0, 3), (parsed.Spans[0].Start, parsed.Spans[0].Length));
            Assert.IsInstanceOf<SwayEffect>(parsed.Spans[1].Effect);
            Assert.AreEqual((1, 1), (parsed.Spans[1].Start, parsed.Spans[1].Length));
        }

        [Test]
        public void ColorTag_ParsesHexAndNames()
        {
            var hex = (ColorEffect)Parse("<color=#4aa3ff>x</color>").Spans[0].Effect;
            var named = (ColorEffect)Parse("<color=red>x</color>").Spans[0].Effect;

            Assert.AreEqual(((byte)0x4a, (byte)0xa3, (byte)0xff), (hex.Color.r, hex.Color.g, hex.Color.b));
            Assert.AreEqual(((byte)255, (byte)0, (byte)0), (named.Color.r, named.Color.g, named.Color.b));
        }

        [Test]
        public void ColorTag_WithBadValue_StaysLiteralWithAWarning()
        {
            var parsed = Parse("<color=nope>x</color>");

            Assert.AreEqual("<color=nope>x", parsed.Plain);
            Assert.IsFalse(parsed.HasEffects);
            Assert.IsNotEmpty(parsed.Warnings);
        }

        [Test]
        public void UnknownTags_AndComparisons_StayLiteral()
        {
            var parsed = Parse("a < b, <blue>c</blue> <unknown=1>d");

            Assert.AreEqual("a < b, <blue>c</blue> <unknown=1>d", parsed.Plain);
            Assert.IsFalse(parsed.HasEffects);
            Assert.IsEmpty(parsed.Warnings);
        }

        [Test]
        public void NameMustEndAtTheTag_SoLongerWordsAreNotTags()
        {
            // "<waves>" must not be read as "<wave>" + "s".
            Assert.AreEqual("<waves>x", Parse("<waves>x").Plain);
        }

        [Test]
        public void Backslash_EscapesAKnownTag()
        {
            var parsed = Parse(@"\<wave>x</wave>");

            Assert.AreEqual("<wave>x", parsed.Plain);
            Assert.IsFalse(parsed.HasEffects);
        }

        [Test]
        public void NamedArguments_AreReadWithDefaultsForTheRest()
        {
            var parsed = Parse("<shake amp=4>x</shake><rainbow>y</rainbow>");

            Assert.AreEqual(2, parsed.Spans.Count);
            Assert.IsInstanceOf<ShakeEffect>(parsed.Spans[0].Effect);
            Assert.IsInstanceOf<RainbowEffect>(parsed.Spans[1].Effect);
        }

        [Test]
        public void UnclosedTag_RunsToTheEndWithAWarning()
        {
            var parsed = Parse("가<wave>나다");

            Assert.AreEqual("가나다", parsed.Plain);
            Assert.AreEqual((1, 2), (parsed.Spans[0].Start, parsed.Spans[0].Length));
            Assert.IsNotEmpty(parsed.Warnings);
        }

        [Test]
        public void UnmatchedClosingTag_IsIgnoredWithAWarning()
        {
            var parsed = Parse("가</wave>나");

            Assert.AreEqual("가나", parsed.Plain);
            Assert.IsFalse(parsed.HasEffects);
            Assert.IsNotEmpty(parsed.Warnings);
        }

        [Test]
        public void ClosingAnOuterTag_ClosesTheInnerOneToo()
        {
            var parsed = Parse("<color=red>가<wave>나</color>다");

            Assert.AreEqual("가나다", parsed.Plain);
            var wave = parsed.Spans.Single(s => s.Effect is WaveEffect);
            Assert.AreEqual((1, 1), (wave.Start, wave.Length));
            Assert.IsNotEmpty(parsed.Warnings);
        }

        [Test]
        public void EmptySpans_AreDropped()
        {
            Assert.IsFalse(Parse("가<wave></wave>나").HasEffects);
        }

        [Test]
        public void Pause_IsRecordedAtThePlainIndex_AndLeavesNoText()
        {
            var parsed = Parse("가나<pause=0.5>다라<pause>마");

            Assert.AreEqual("가나다라마", parsed.Plain);
            Assert.AreEqual(2, parsed.Pauses.Count);
            Assert.AreEqual((2, 0.5f), (parsed.Pauses[0].Index, parsed.Pauses[0].Seconds));
            Assert.AreEqual(4, parsed.Pauses[1].Index);
            Assert.Greater(parsed.Pauses[1].Seconds, 0f);
        }

        [Test]
        public void Pause_WithBadNumber_FallsBackToTheDefaultWithAWarning()
        {
            var parsed = Parse("가<pause=abc>나");

            Assert.AreEqual(1, parsed.Pauses.Count);
            Assert.Greater(parsed.Pauses[0].Seconds, 0f);
            Assert.IsNotEmpty(parsed.Warnings);
        }

        [Test]
        public void TagNames_AreCaseInsensitive()
        {
            Assert.AreEqual(1, Parse("<WAVE>x</Wave>").Spans.Count);
        }

        [Test]
        public void Registry_AllowsNewEffects_WithoutChangingTheParser()
        {
            var registry = TextEffectRegistry.CreateDefault();
            registry.Register("zoom", _ => new ShakeEffect(0f, 1f));

            var parsed = DialogueMarkup.Parse("<zoom>x</zoom>", registry);

            Assert.AreEqual(1, parsed.Spans.Count);
            Assert.IsFalse(DialogueMarkup.Parse("<zoom>x</zoom>", TextEffectRegistry.CreateDefault()).HasEffects);
        }

        [Test]
        public void StaticMarkup_KeepsOnlyColours_InnermostWins()
        {
            var parsed = Parse("가<color=#ff0000>나<sway>다</sway><color=#00ff00>라</color></color>마");

            Assert.AreEqual("가<color=#FF0000FF>나다</color><color=#00FF00FF>라</color>마", parsed.ToStaticMarkup());
        }

        [Test]
        public void StaticMarkup_EscapesLiteralAngleBrackets()
        {
            Assert.AreEqual("a<noparse><</noparse> b", Parse("a< b").ToStaticMarkup());
        }
    }
}
