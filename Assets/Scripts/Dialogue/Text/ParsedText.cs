using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>An effect applied to plain-text characters [Start, Start + Length).</summary>
    public readonly struct TextSpan
    {
        public TextSpan(int start, int length, ITextEffect effect, string tag = null)
        {
            Start = start;
            Length = length;
            Effect = effect;
            Tag = tag ?? string.Empty;
        }

        public int Start { get; }
        public int Length { get; }
        public ITextEffect Effect { get; }

        /// <summary>The opening tag as written, normalised ("color=#4aa3ff", "wave amp=4") - for validation and debugging.</summary>
        public string Tag { get; }

        public bool Covers(int index) => index >= Start && index < Start + Length;
    }

    /// <summary>The typing stops for Seconds once Index characters are visible.</summary>
    public readonly struct TextPause
    {
        public TextPause(int index, float seconds)
        {
            Index = index;
            Seconds = seconds;
        }

        public int Index { get; }
        public float Seconds { get; }
    }

    /// <summary>
    /// A dialogue line with its markup taken apart: the plain text to lay out,
    /// the effect spans (indices into that plain text) and typing pauses.
    /// </summary>
    public sealed class ParsedText
    {
        private const string LiteralOpen = "<noparse><</noparse>";

        public ParsedText(string plain, IReadOnlyList<TextSpan> spans, IReadOnlyList<TextPause> pauses, IReadOnlyList<string> warnings)
        {
            Plain = plain;
            Spans = spans;
            Pauses = pauses;
            Warnings = warnings;
        }

        public string Plain { get; }
        public IReadOnlyList<TextSpan> Spans { get; }
        public IReadOnlyList<TextPause> Pauses { get; }

        /// <summary>Authoring problems (unclosed tags, bad arguments) for validation and logs.</summary>
        public IReadOnlyList<string> Warnings { get; }

        public bool HasEffects => Spans.Count > 0;

        /// <summary>
        /// The text with only its static colours kept, as TextMeshPro rich text -
        /// for places that cannot animate (history log, choice labels).
        /// </summary>
        public string ToStaticMarkup()
        {
            var builder = new StringBuilder(Plain.Length + 16);
            Color32? open = null;

            for (int i = 0; i < Plain.Length; i++)
            {
                Color32? color = StaticColorAt(i);
                if (!SameColor(open, color))
                {
                    if (open.HasValue)
                    {
                        builder.Append("</color>");
                    }

                    if (color.HasValue)
                    {
                        builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGBA(color.Value)).Append('>');
                    }

                    open = color;
                }

                if (Plain[i] == '<')
                {
                    builder.Append(LiteralOpen);
                }
                else
                {
                    builder.Append(Plain[i]);
                }
            }

            if (open.HasValue)
            {
                builder.Append("</color>");
            }

            return builder.ToString();
        }

        // Spans are ordered by where their tag opened, so the last covering colour is the innermost.
        private Color32? StaticColorAt(int index)
        {
            Color32? result = null;
            foreach (var span in Spans)
            {
                if (span.Effect is ColorEffect color && span.Covers(index))
                {
                    result = color.Color;
                }
            }

            return result;
        }

        private static bool SameColor(Color32? a, Color32? b)
        {
            if (a.HasValue != b.HasValue)
            {
                return false;
            }

            return !a.HasValue || (a.Value.r == b.Value.r && a.Value.g == b.Value.g && a.Value.b == b.Value.b && a.Value.a == b.Value.a);
        }
    }
}
