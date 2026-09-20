using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;

namespace Game.EditorTools
{
    /// <summary>Creates the LocalizationSettings asset, the default locales and the UIStrings table. Safe to re-run.</summary>
    public static class LocalizationSetup
    {
        private const string RootFolder = "Assets/Localization";
        private const string LocalesFolder = RootFolder + "/Locales";
        private const string TablesFolder = RootFolder + "/Tables";
        private const string SettingsPath = RootFolder + "/LocalizationSettings.asset";
        private const string UIStringsName = "UIStrings";

        // Locale code, English name (the locale asset name), and the language name written in that language (used by language pickers).
        // "jp" is not the ISO 639-1 language code ("ja") - it is this project's chosen code, so Unity finds no matching culture for it.
        private static readonly (string code, string englishName, string nativeName)[] DefaultLocales =
        {
            ("en", "English", "English"),
            ("ko", "Korean", "한국어"),
            ("jp", "Japanese", "日本語"),
        };

        [MenuItem("Game/Localization/Setup Default Locales and Tables")]
        public static void Run()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(LocalesFolder);
            EnsureFolder(TablesFolder);

            EnsureSettings();
            var locales = EnsureLocales();
            var collection = EnsureStringTable(locales);
            SeedLanguageNames(collection);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Localization setup done: {locales.Count} locales, table '{UIStringsName}'.");
        }

        private static void EnsureSettings()
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            }
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }

        private static List<Locale> EnsureLocales()
        {
            var result = new List<Locale>();
            foreach (var (code, englishName, _) in DefaultLocales)
            {
                var locale = LocalizationEditorSettings.GetLocale(code);
                if (locale == null)
                {
                    locale = Locale.CreateLocale(new LocaleIdentifier(code));
                    locale.LocaleName = englishName; // a non-standard code has no culture to take the name from
                    AssetDatabase.CreateAsset(locale, $"{LocalesFolder}/{locale.LocaleName}.asset");
                    LocalizationEditorSettings.AddLocale(locale);
                }
                result.Add(locale);
            }
            return result;
        }

        private static StringTableCollection EnsureStringTable(List<Locale> locales)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(UIStringsName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(UIStringsName, TablesFolder, locales);
            }
            else
            {
                foreach (var locale in locales)
                {
                    if (collection.GetTable(locale.Identifier) == null)
                    {
                        collection.AddNewTable(locale.Identifier);
                    }
                }
            }
            return collection;
        }

        private static void SeedLanguageNames(StringTableCollection collection)
        {
            const string key = "language.name";
            var shared = collection.SharedData;
            if (shared.GetEntry(key) == null)
            {
                shared.AddKey(key).Metadata.AddMetadata(new Comment { CommentText = "Name of this language, written in that language. Shown in the language picker." });
            }

            foreach (var (code, _, nativeName) in DefaultLocales)
            {
                var table = collection.GetTable(new LocaleIdentifier(code)) as UnityEngine.Localization.Tables.StringTable;
                if (table == null) continue;
                var entry = table.GetEntry(key);
                if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
                {
                    table.AddEntry(key, nativeName);
                }
                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(shared);
            EditorUtility.SetDirty(collection);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
        }

        private static void EnsureFolder(string path)
        {
            if (Directory.Exists(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
