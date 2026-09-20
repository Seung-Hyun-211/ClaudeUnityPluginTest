using System.Collections.Generic;
using System.Globalization;

namespace Game.Dialogue
{
    /// <summary>
    /// The arguments of one tag: <c>&lt;color=#fff&gt;</c> has a positional value,
    /// <c>&lt;wave amp=4 speed=2&gt;</c> has named ones. Everything is optional;
    /// effects supply their own defaults.
    /// </summary>
    public sealed class TextTagArgs
    {
        public static readonly TextTagArgs Empty = new(string.Empty, new Dictionary<string, string>());

        private readonly Dictionary<string, string> named;

        private TextTagArgs(string positional, Dictionary<string, string> named)
        {
            Positional = positional;
            this.named = named;
        }

        /// <summary>The value after '=' directly behind the tag name, or empty.</summary>
        public string Positional { get; }

        /// <summary>Parses what follows the tag name, e.g. "=#fff" or " amp=4 speed=2".</summary>
        public static TextTagArgs Parse(string raw)
        {
            raw = raw?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                return Empty;
            }

            if (raw[0] == '=')
            {
                return new TextTagArgs(raw.Substring(1).Trim(), new Dictionary<string, string>());
            }

            var pairs = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var token in raw.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = token.IndexOf('=');
                if (eq > 0)
                {
                    pairs[token.Substring(0, eq)] = token.Substring(eq + 1);
                }
            }

            return new TextTagArgs(string.Empty, pairs);
        }

        public float Float(string key, float fallback)
        {
            return named.TryGetValue(key, out var value) && TryParseFloat(value, out float result) ? result : fallback;
        }

        public static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
