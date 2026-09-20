using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>Sets the glyph colour (the innermost colour tag wins).</summary>
    public sealed class ColorEffect : ITextEffect
    {
        public ColorEffect(Color32 color) => Color = color;

        public Color32 Color { get; }

        public void Apply(in GlyphContext context, ref GlyphStyle style) => style.Color = Color;
    }

    /// <summary>Horizontal sway; the phase advances per glyph so the span ripples sideways.</summary>
    public sealed class SwayEffect : ITextEffect
    {
        private const float PhasePerGlyph = 0.6f;
        private readonly float amplitude;
        private readonly float frequency;

        public SwayEffect(float amplitude, float frequency)
        {
            this.amplitude = amplitude;
            this.frequency = frequency;
        }

        public void Apply(in GlyphContext context, ref GlyphStyle style)
        {
            float phase = 2f * Mathf.PI * frequency * context.Time + PhasePerGlyph * context.SpanIndex;
            style.Offset.x += amplitude * Mathf.Sin(phase);
        }
    }

    /// <summary>Vertical wave; the phase advances per glyph.</summary>
    public sealed class WaveEffect : ITextEffect
    {
        private const float PhasePerGlyph = 0.6f;
        private readonly float amplitude;
        private readonly float frequency;

        public WaveEffect(float amplitude, float frequency)
        {
            this.amplitude = amplitude;
            this.frequency = frequency;
        }

        public void Apply(in GlyphContext context, ref GlyphStyle style)
        {
            float phase = 2f * Mathf.PI * frequency * context.Time + PhasePerGlyph * context.SpanIndex;
            style.Offset.y += amplitude * Mathf.Sin(phase);
        }
    }

    /// <summary>Jitter: every glyph jumps to a new random spot Rate times per second (deterministic hash, no RNG state).</summary>
    public sealed class ShakeEffect : ITextEffect
    {
        private readonly float amplitude;
        private readonly float rate;

        public ShakeEffect(float amplitude, float rate)
        {
            this.amplitude = amplitude;
            this.rate = rate;
        }

        public void Apply(in GlyphContext context, ref GlyphStyle style)
        {
            int step = Mathf.FloorToInt(context.Time * rate);
            style.Offset.x += amplitude * Signed(Hash(context.LineIndex, step, 1));
            style.Offset.y += amplitude * Signed(Hash(context.LineIndex, step, 2));
        }

        private static uint Hash(int a, int b, int salt)
        {
            unchecked
            {
                uint h = (uint)a * 374761393u + (uint)b * 668265263u + (uint)salt * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        // Maps the hash to [-1, 1].
        private static float Signed(uint hash) => (hash & 0xFFFFu) / 32767.5f - 1f;
    }

    /// <summary>Cycles the hue over time, shifted per glyph so the colours run along the text.</summary>
    public sealed class RainbowEffect : ITextEffect
    {
        private const float HuePerGlyph = 0.08f;
        private readonly float speed;

        public RainbowEffect(float speed) => this.speed = speed;

        public void Apply(in GlyphContext context, ref GlyphStyle style)
        {
            float hue = Mathf.Repeat(speed * context.Time + HuePerGlyph * context.SpanIndex, 1f);
            Color32 rgb = Color.HSVToRGB(hue, 0.55f, 1f);
            style.Color = new Color32(rgb.r, rgb.g, rgb.b, style.Color.a);
        }
    }
}
