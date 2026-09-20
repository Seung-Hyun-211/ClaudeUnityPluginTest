using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>What an effect gets to know about the glyph it is styling.</summary>
    public readonly struct GlyphContext
    {
        public GlyphContext(int lineIndex, int spanIndex, int spanLength, float time)
        {
            LineIndex = lineIndex;
            SpanIndex = spanIndex;
            SpanLength = spanLength;
            Time = time;
        }

        /// <summary>Index of the glyph in the whole plain text of the line.</summary>
        public int LineIndex { get; }

        /// <summary>Index of the glyph inside the span the effect is applied to.</summary>
        public int SpanIndex { get; }

        public int SpanLength { get; }

        /// <summary>Seconds; effects are pure functions of it so they can be tested without a renderer.</summary>
        public float Time { get; }
    }

    /// <summary>What an effect may change: a glyph's offset (in text-mesh units) and colour.</summary>
    public struct GlyphStyle
    {
        public Vector2 Offset;
        public Color32 Color;
    }

    /// <summary>
    /// A per-glyph text effect (colour, sway, ...). Stateless and free of any
    /// TextMeshPro dependency: the animator asks it for a style, so the maths
    /// can be unit-tested and new effects are just new classes (see
    /// TextEffectRegistry).
    /// </summary>
    public interface ITextEffect
    {
        void Apply(in GlyphContext context, ref GlyphStyle style);
    }
}
