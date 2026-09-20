using NUnit.Framework;
using UnityEngine;
using Game.Dialogue;

namespace Game.Tests
{
    public class TextEffectTests
    {
        private static GlyphStyle Styled(ITextEffect effect, int spanIndex, float time, int lineIndex = 0)
        {
            var style = new GlyphStyle { Color = new Color32(255, 255, 255, 255) };
            effect.Apply(new GlyphContext(lineIndex, spanIndex, 10, time), ref style);
            return style;
        }

        [Test]
        public void Sway_MovesOnlyHorizontally_WithinItsAmplitude()
        {
            var sway = new SwayEffect(3f, 1f);
            float widest = 0f;
            for (float t = 0f; t < 2f; t += 0.01f)
            {
                var style = Styled(sway, 0, t);
                Assert.AreEqual(0f, style.Offset.y);
                widest = Mathf.Max(widest, Mathf.Abs(style.Offset.x));
            }

            Assert.AreEqual(3f, widest, 0.05f);
        }

        [Test]
        public void Sway_GlyphsAreOutOfPhase_SoTheSpanRipples()
        {
            var sway = new SwayEffect(3f, 1f);

            Assert.AreNotEqual(Styled(sway, 0, 0.3f).Offset.x, Styled(sway, 1, 0.3f).Offset.x);
        }

        [Test]
        public void Sway_RepeatsAfterOnePeriod()
        {
            var sway = new SwayEffect(3f, 2f);

            Assert.AreEqual(Styled(sway, 3, 0.2f).Offset.x, Styled(sway, 3, 0.7f).Offset.x, 0.001f);
        }

        [Test]
        public void Wave_MovesOnlyVertically()
        {
            var wave = new WaveEffect(4f, 1f);
            var style = Styled(wave, 2, 0.37f);

            Assert.AreEqual(0f, style.Offset.x);
            Assert.AreNotEqual(0f, style.Offset.y);
            Assert.LessOrEqual(Mathf.Abs(style.Offset.y), 4f + 0.0001f);
        }

        [Test]
        public void Effects_Stack_ByAddingOffsets()
        {
            var style = new GlyphStyle();
            var context = new GlyphContext(0, 0, 5, 0.25f);
            new SwayEffect(3f, 1f).Apply(context, ref style);
            new WaveEffect(4f, 1f).Apply(context, ref style);

            Assert.AreNotEqual(0f, style.Offset.x);
            Assert.AreNotEqual(0f, style.Offset.y);
        }

        [Test]
        public void Shake_IsDeterministic_AndStaysWithinAmplitude()
        {
            var shake = new ShakeEffect(2f, 30f);

            Assert.AreEqual(Styled(shake, 0, 1.234f, 7).Offset, Styled(shake, 0, 1.234f, 7).Offset);
            for (int glyph = 0; glyph < 50; glyph++)
            {
                var offset = Styled(shake, 0, 0.5f, glyph).Offset;
                Assert.LessOrEqual(Mathf.Abs(offset.x), 2f + 0.0001f);
                Assert.LessOrEqual(Mathf.Abs(offset.y), 2f + 0.0001f);
            }
        }

        [Test]
        public void Shake_HoldsWithinAStep_AndJumpsBetweenSteps_AndDiffersPerGlyph()
        {
            var shake = new ShakeEffect(2f, 10f);

            Assert.AreEqual(Styled(shake, 0, 0.51f, 3).Offset, Styled(shake, 0, 0.59f, 3).Offset);
            Assert.AreNotEqual(Styled(shake, 0, 0.51f, 3).Offset, Styled(shake, 0, 0.61f, 3).Offset);
            Assert.AreNotEqual(Styled(shake, 0, 0.51f, 3).Offset, Styled(shake, 0, 0.51f, 4).Offset);
        }

        [Test]
        public void Color_SetsTheColour()
        {
            var style = Styled(new ColorEffect(new Color32(10, 20, 30, 255)), 0, 0f);

            Assert.AreEqual(((byte)10, (byte)20, (byte)30), (style.Color.r, style.Color.g, style.Color.b));
        }

        [Test]
        public void Rainbow_ChangesOverTime_AndAcrossGlyphs_KeepingAlpha()
        {
            var rainbow = new RainbowEffect(0.5f);
            var early = Styled(rainbow, 0, 0f).Color;

            Assert.AreNotEqual(early, Styled(rainbow, 0, 0.7f).Color);
            Assert.AreNotEqual(early, Styled(rainbow, 4, 0f).Color);

            var style = new GlyphStyle { Color = new Color32(1, 2, 3, 77) };
            rainbow.Apply(new GlyphContext(0, 0, 1, 0.3f), ref style);
            Assert.AreEqual(77, style.Color.a);
        }

        [Test]
        public void Rainbow_IsPeriodic()
        {
            var rainbow = new RainbowEffect(0.5f);

            Assert.AreEqual(Styled(rainbow, 2, 0.1f).Color, Styled(rainbow, 2, 2.1f).Color);
        }
    }
}
