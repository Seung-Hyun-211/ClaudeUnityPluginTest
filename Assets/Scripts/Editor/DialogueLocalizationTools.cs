using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Game.Dialogue;

namespace Game.EditorTools
{
    /// <summary>
    /// Keeps dialogue sequences and the "Dialogue" string table in step
    /// (documents/dialogue-localization.md). The text written on a node is the
    /// source language; this tool mirrors it into the table and points the
    /// node at the entry, so translators only ever edit the other languages.
    /// </summary>
    public static class DialogueLocalizationTools
    {
        public const string TableName = "Dialogue";
        public const string SourceLocaleCode = "ko";
        private const string TablesFolder = "Assets/Localization/Tables";
        private const string SpeakerKeyPrefix = "speaker.";

        [MenuItem("Game/Dialogue/Sync Sequences To String Table")]
        public static void Sync()
        {
            var collection = EnsureCollection();
            var source = collection.GetTable(new LocaleIdentifier(SourceLocaleCode)) as StringTable;
            if (source == null)
            {
                Debug.LogError($"Dialogue sync: the '{TableName}' table has no '{SourceLocaleCode}' (source language) table.");
                return;
            }

            int entries = 0;
            foreach (var sequence in LoadSequences())
            {
                bool changed = false;
                foreach (var node in Nodes(sequence))
                {
                    changed |= SyncNode(collection, source, sequence.SequenceId, node, ref entries);
                }

                if (changed)
                {
                    EditorUtility.SetDirty(sequence);
                }
            }

            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            Debug.Log($"Dialogue sync: {entries} source-language entries written to '{TableName}' ({SourceLocaleCode}).");
        }

        [MenuItem("Game/Dialogue/Validate Text Markup")]
        public static void Validate()
        {
            int problems = 0;

            foreach (var sequence in LoadSequences())
            {
                foreach (var node in Nodes(sequence))
                {
                    foreach (var (where, text) in AuthoredTexts(node))
                    {
                        foreach (var issue in DialogueMarkupValidator.Check(text))
                        {
                            Debug.LogWarning($"Dialogue markup: {sequence.SequenceId}/{node.id} {where}: {issue}", sequence);
                            problems++;
                        }
                    }
                }
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
            if (collection != null)
            {
                problems += ValidateTranslations(collection);
            }

            Debug.Log(problems == 0
                ? "Dialogue markup: no problems."
                : $"Dialogue markup: {problems} problem(s) - see the warnings above.");
        }

        private static int ValidateTranslations(StringTableCollection collection)
        {
            int problems = 0;
            var source = collection.GetTable(new LocaleIdentifier(SourceLocaleCode)) as StringTable;
            if (source == null)
            {
                return 0;
            }

            foreach (var table in collection.StringTables)
            {
                if (table == source)
                {
                    continue;
                }

                foreach (var pair in source.Values)
                {
                    if (string.IsNullOrEmpty(pair.Value))
                    {
                        continue;
                    }

                    var translated = table.GetEntry(pair.KeyId);
                    if (translated == null || string.IsNullOrWhiteSpace(translated.Value))
                    {
                        continue; // not translated yet: the game falls back to the source text
                    }

                    foreach (var issue in DialogueMarkupValidator.Compare(pair.Value, translated.Value))
                    {
                        Debug.LogWarning($"Dialogue markup: [{table.LocaleIdentifier.Code}] {pair.Key}: {issue}", table);
                        problems++;
                    }
                }
            }

            return problems;
        }

        private static bool SyncNode(StringTableCollection collection, StringTable source, string sequenceId, DialogueNode node, ref int entries)
        {
            bool changed = false;

            if (node.type == DialogueNodeType.Line)
            {
                if (!string.IsNullOrEmpty(node.text))
                {
                    changed |= Bind(collection, source, ref node.localizedText, $"{sequenceId}.{node.id}", node.text, ref entries);
                }

                if (!string.IsNullOrEmpty(node.speakerName))
                {
                    // One entry per speaker name, shared by every line they speak.
                    changed |= Bind(collection, source, ref node.localizedSpeaker, SpeakerKeyPrefix + node.speakerName, node.speakerName, ref entries);
                }
            }

            if (node.choices != null)
            {
                for (int i = 0; i < node.choices.Length; i++)
                {
                    var option = node.choices[i];
                    if (!string.IsNullOrEmpty(option.text))
                    {
                        changed |= Bind(collection, source, ref option.localizedText, $"{sequenceId}.{node.id}.c{i}", option.text, ref entries);
                    }
                }
            }

            return changed;
        }

        // Writes the source text under the key and makes sure the reference points at it.
        private static bool Bind(StringTableCollection collection, StringTable source, ref LocalizedString reference, string key, string text, ref int entries)
        {
            bool changed = false;

            if (collection.SharedData.GetEntry(key) == null)
            {
                collection.SharedData.AddKey(key);
            }

            var entry = source.GetEntry(key);
            if (entry == null)
            {
                source.AddEntry(key, text);
                changed = true;
            }
            else if (entry.Value != text)
            {
                entry.Value = text;
                changed = true;
            }

            entries++;

            reference ??= new LocalizedString();
            if (reference.IsEmpty || reference.TableReference.TableCollectionName != TableName || reference.TableEntryReference.Key != key)
            {
                reference.SetReference(TableName, key);
                changed = true;
            }

            return changed;
        }

        private static StringTableCollection EnsureCollection()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
            var locales = LocalizationEditorSettings.GetLocales().ToList();

            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(TableName, TablesFolder, locales);
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

        private static IEnumerable<DialogueSequence> LoadSequences()
        {
            return AssetDatabase.FindAssets("t:DialogueSequence")
                .Select(guid => AssetDatabase.LoadAssetAtPath<DialogueSequence>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(s => s != null);
        }

        private static IEnumerable<DialogueNode> Nodes(DialogueSequence sequence)
        {
            var field = typeof(DialogueSequence).GetField("nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (field?.GetValue(sequence) as DialogueNode[]) ?? System.Array.Empty<DialogueNode>();
        }

        private static IEnumerable<(string where, string text)> AuthoredTexts(DialogueNode node)
        {
            if (!string.IsNullOrEmpty(node.text))
            {
                yield return ("text", node.text);
            }

            if (node.choices == null)
            {
                yield break;
            }

            for (int i = 0; i < node.choices.Length; i++)
            {
                if (!string.IsNullOrEmpty(node.choices[i].text))
                {
                    yield return ($"choice {i}", node.choices[i].text);
                }
            }
        }
    }
}
