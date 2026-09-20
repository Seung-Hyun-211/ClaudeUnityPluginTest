using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>Builds an effect from a tag's arguments; returns null when the arguments are unusable.</summary>
    public delegate ITextEffect TextEffectFactory(TextTagArgs args);

    /// <summary>
    /// Maps tag names to effect factories. A new effect is a class plus one
    /// Register call; the parser and animator never change (open-closed).
    /// "pause" is a typing tag handled by the parser, not an effect.
    /// </summary>
    public sealed class TextEffectRegistry
    {
        private readonly Dictionary<string, TextEffectFactory> factories = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Shared registry with the built-in effects.</summary>
        public static TextEffectRegistry Default { get; } = CreateDefault();

        /// <summary>A fresh registry with the built-ins, for callers (tests) that must not touch Default.</summary>
        public static TextEffectRegistry CreateDefault()
        {
            var registry = new TextEffectRegistry();
            registry.Register("color", args => TryParseColor(args.Positional, out Color32 color) ? new ColorEffect(color) : null);
            registry.Register("sway", args => new SwayEffect(args.Float("amp", 3f), args.Float("speed", 1.5f)));
            registry.Register("wave", args => new WaveEffect(args.Float("amp", 4f), args.Float("speed", 1.2f)));
            registry.Register("shake", args => new ShakeEffect(args.Float("amp", 1.5f), args.Float("rate", 30f)));
            registry.Register("rainbow", args => new RainbowEffect(args.Float("speed", 0.5f)));
            return registry;
        }

        /// <summary>Adds or replaces the effect behind a tag name.</summary>
        public void Register(string name, TextEffectFactory factory)
        {
            factories[name] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public bool IsKnown(string name) => factories.ContainsKey(name);

        public bool TryCreate(string name, TextTagArgs args, out ITextEffect effect)
        {
            effect = null;
            return factories.TryGetValue(name, out var factory) && (effect = factory(args)) != null;
        }

        private static bool TryParseColor(string value, out Color32 color)
        {
            color = default;
            if (string.IsNullOrEmpty(value) || !ColorUtility.TryParseHtmlString(value, out Color parsed))
            {
                return false;
            }

            color = parsed;
            return true;
        }
    }
}
