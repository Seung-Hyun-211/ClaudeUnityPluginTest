using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Dialogue
{
    /// <summary>
    /// Splits an authored line such as "떠났다 <c>&lt;wave&gt;</c>...<c>&lt;/wave&gt;</c>" into plain text,
    /// effect spans and typing pauses (see documents/dialogue-text-effects.md).
    /// Only tags the registry knows (plus "pause") are tags; anything else - "a &lt; b" -
    /// stays literal. "\&lt;" writes a literal '&lt;'.
    /// </summary>
    public static class DialogueMarkup
    {
        private const string PauseTag = "pause";
        private const float DefaultPauseSeconds = 0.4f;

        private sealed class OpenTag
        {
            public string Name;
            public int Start;
            public ITextEffect Effect;
            public int SpanSlot;
        }

        private sealed class SpanBuilder
        {
            public int Start;
            public int End;
            public ITextEffect Effect;
            public string Tag;
        }

        public static ParsedText Parse(string source, TextEffectRegistry registry = null)
        {
            registry ??= TextEffectRegistry.Default;
            source ??= string.Empty;

            var plain = new StringBuilder(source.Length);
            var stack = new List<OpenTag>();
            // Spans are stored in the order their tags opened (callers rely on that for "innermost wins").
            var slots = new List<SpanBuilder>();
            var pauses = new List<TextPause>();
            var warnings = new List<string>();

            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];

                if (c == '\\' && i + 1 < source.Length && source[i + 1] == '<')
                {
                    plain.Append('<');
                    i += 2;
                    continue;
                }

                if (c == '<' && TryReadTag(source, i, out bool closing, out string name, out string rawArgs, out int next)
                    && IsKnown(name, registry))
                {
                    if (closing)
                    {
                        Close(name, plain.Length, stack, slots, warnings);
                        i = next;
                        continue;
                    }

                    if (Open(name, rawArgs, plain.Length, registry, stack, slots, pauses, warnings))
                    {
                        i = next;
                        continue;
                    }
                }

                plain.Append(c);
                i++;
            }

            foreach (var tag in stack)
            {
                warnings.Add($"<{tag.Name}> is never closed; it runs to the end of the line.");
                slots[tag.SpanSlot].End = plain.Length;
            }

            var spans = new List<TextSpan>(slots.Count);
            foreach (var slot in slots)
            {
                if (slot.End > slot.Start)
                {
                    spans.Add(new TextSpan(slot.Start, slot.End - slot.Start, slot.Effect, slot.Tag));
                }
            }

            return new ParsedText(plain.ToString(), spans, pauses, warnings);
        }

        private static bool IsKnown(string name, TextEffectRegistry registry)
        {
            return name.Equals(PauseTag, StringComparison.OrdinalIgnoreCase) || registry.IsKnown(name);
        }

        private static bool Open(string name, string rawArgs, int plainLength, TextEffectRegistry registry,
            List<OpenTag> stack, List<SpanBuilder> slots, List<TextPause> pauses, List<string> warnings)
        {
            var args = TextTagArgs.Parse(rawArgs);

            if (name.Equals(PauseTag, StringComparison.OrdinalIgnoreCase))
            {
                float seconds = DefaultPauseSeconds;
                if (!string.IsNullOrEmpty(args.Positional) && !TextTagArgs.TryParseFloat(args.Positional, out seconds))
                {
                    warnings.Add($"<pause={args.Positional}> is not a number; using {DefaultPauseSeconds}s.");
                    seconds = DefaultPauseSeconds;
                }

                pauses.Add(new TextPause(plainLength, Math.Max(0f, seconds)));
                return true;
            }

            if (!registry.TryCreate(name, args, out ITextEffect effect))
            {
                warnings.Add($"<{name}{rawArgs}> has unusable arguments; it is shown as text.");
                return false;
            }

            stack.Add(new OpenTag { Name = name, Start = plainLength, Effect = effect, SpanSlot = slots.Count });
            slots.Add(new SpanBuilder { Start = plainLength, End = -1, Effect = effect, Tag = name.ToLowerInvariant() + rawArgs.TrimEnd() });
            return true;
        }

        private static void Close(string name, int plainLength, List<OpenTag> stack,
            List<SpanBuilder> slots, List<string> warnings)
        {
            int match = stack.FindLastIndex(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match < 0)
            {
                warnings.Add($"</{name}> has no matching opening tag; ignored.");
                return;
            }

            // Anything opened after the match is still open: close it with the match.
            for (int k = stack.Count - 1; k >= match; k--)
            {
                if (k > match)
                {
                    warnings.Add($"<{stack[k].Name}> was closed implicitly by </{name}>.");
                }

                var tag = stack[k];
                slots[tag.SpanSlot].End = plainLength;
                stack.RemoveAt(k);
            }
        }

        // Reads "<name args>" or "</name>" at source[start]; false if it is not shaped like a tag.
        private static bool TryReadTag(string source, int start, out bool closing, out string name, out string rawArgs, out int next)
        {
            closing = false;
            name = null;
            rawArgs = null;
            next = start;

            int end = source.IndexOf('>', start + 1);
            if (end < 0)
            {
                return false;
            }

            int pos = start + 1;
            if (pos < end && source[pos] == '/')
            {
                closing = true;
                pos++;
            }

            int nameStart = pos;
            while (pos < end && char.IsLetter(source[pos]))
            {
                pos++;
            }

            if (pos == nameStart)
            {
                return false;
            }

            // The name must be followed by the end, '=' or whitespace ("<blue>" is not "<b>" + "lue").
            if (pos < end && source[pos] != '=' && !char.IsWhiteSpace(source[pos]))
            {
                return false;
            }

            string inner = source.Substring(pos, end - pos);
            if (inner.IndexOf('<') >= 0)
            {
                return false;
            }

            name = source.Substring(nameStart, pos - nameStart);
            rawArgs = inner;
            next = end + 1;
            return true;
        }
    }
}
