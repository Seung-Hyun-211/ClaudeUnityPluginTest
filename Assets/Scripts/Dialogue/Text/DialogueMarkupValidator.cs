using System.Collections.Generic;
using System.Linq;

namespace Game.Dialogue
{
    /// <summary>
    /// Finds markup mistakes in dialogue text: tags that do not parse, and
    /// translations that lost or invented a tag compared with the source
    /// language. Pure (no editor or localization dependency) so the editor
    /// validation command and the tests share it.
    /// </summary>
    public static class DialogueMarkupValidator
    {
        /// <summary>Problems in one text on its own (unclosed tags, unusable arguments, ...).</summary>
        public static IReadOnlyList<string> Check(string text, TextEffectRegistry registry = null)
        {
            return DialogueMarkup.Parse(text, registry).Warnings;
        }

        /// <summary>
        /// Problems in a translation: its own markup errors, plus every tag the
        /// source has that the translation lacks (or the other way round).
        /// Tag order and position may differ - languages order words differently.
        /// </summary>
        public static IReadOnlyList<string> Compare(string source, string translation, TextEffectRegistry registry = null)
        {
            var problems = new List<string>();
            var src = DialogueMarkup.Parse(source, registry);
            var dst = DialogueMarkup.Parse(translation, registry);

            problems.AddRange(dst.Warnings);

            var missing = Multiset(src).ToList();
            foreach (var tag in Multiset(dst))
            {
                if (!missing.Remove(tag))
                {
                    problems.Add($"has <{tag}> which the source does not");
                }
            }

            foreach (var tag in missing)
            {
                problems.Add($"lacks <{tag}> from the source");
            }

            if (src.Pauses.Count != dst.Pauses.Count)
            {
                problems.Add($"has {dst.Pauses.Count} <pause> tag(s), the source has {src.Pauses.Count}");
            }

            return problems;
        }

        private static IEnumerable<string> Multiset(ParsedText parsed) => parsed.Spans.Select(s => s.Tag);
    }
}
