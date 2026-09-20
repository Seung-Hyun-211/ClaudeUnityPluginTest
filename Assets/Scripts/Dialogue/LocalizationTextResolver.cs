using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Game.Dialogue
{
    /// <summary>
    /// Looks the text up in the string table for the selected locale. A node
    /// without a reference, or an entry that does not exist in this language,
    /// falls back to the authored text (the source language) instead of
    /// showing Unity's "No translation found" message.
    /// </summary>
    public sealed class LocalizationTextResolver : IDialogueTextResolver
    {
        public string Resolve(LocalizedString localized, string authored)
        {
            if (localized == null || localized.IsEmpty)
            {
                return authored;
            }

            var database = LocalizationSettings.StringDatabase;
            var found = database.GetTableEntry(localized.TableReference, localized.TableEntryReference, LocalizationSettings.SelectedLocale);
            if (found.Entry == null || string.IsNullOrEmpty(found.Entry.Value))
            {
                return authored;
            }

            // Goes through the LocalizedString so smart-string placeholders are formatted.
            return localized.GetLocalizedString();
        }
    }
}
