using UnityEngine.Localization;

namespace Game.Dialogue
{
    /// <summary>
    /// Turns what a node stores (an optional string-table reference plus the
    /// authored text) into the text to show in the current language. The
    /// runner only knows this interface, so it stays free of the localization
    /// package's runtime state and can be tested with a stub.
    /// </summary>
    public interface IDialogueTextResolver
    {
        /// <param name="localized">The node's table reference; null or empty when the node has none.</param>
        /// <param name="authored">The text written on the node itself (source language); the fallback.</param>
        string Resolve(LocalizedString localized, string authored);
    }

    /// <summary>Always the authored text - no localization (tests, scenes without a locale setup).</summary>
    public sealed class AuthoredTextResolver : IDialogueTextResolver
    {
        public static readonly AuthoredTextResolver Instance = new();

        public string Resolve(LocalizedString localized, string authored) => authored;
    }
}
