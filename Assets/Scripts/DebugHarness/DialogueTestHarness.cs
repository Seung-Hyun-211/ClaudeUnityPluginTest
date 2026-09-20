using System.Text;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Game.Dialogue;

namespace Game.DebugHarness
{
    /// <summary>
    /// OnGUI helper for the dialogue system: shows playback state and story
    /// flags, starts any test sequence without walking up to an NPC (like the
    /// F1 sequence-jump the design doc recommends), and resets the flags.
    /// </summary>
    public class DialogueTestHarness : MonoBehaviour
    {
        [SerializeField] private DialoguePlayer player;
        [SerializeField] private DialogueFlagStore flags;
        [SerializeField] private DialogueSequence[] sequences;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, 260), GUI.skin.box);
            GUILayout.Label("WASD move. Walk next to an NPC and press F to talk.");
            GUILayout.Label($"State: {(player.IsPlaying ? player.State.ToString() : "Idle")}");

            var flagList = new StringBuilder();
            foreach (var flag in flags.SetFlags)
            {
                flagList.Append(flag).Append(' ');
            }
            GUILayout.Label($"Flags: {(flagList.Length > 0 ? flagList.ToString() : "(none)")}");

            foreach (var sequence in sequences)
            {
                if (GUILayout.Button($"Play {sequence.SequenceId}{(sequence.Skippable ? string.Empty : " (not skippable)")}"))
                {
                    player.Play(sequence, null);
                }
            }

            if (GUILayout.Button("Reset flags"))
            {
                flags.Clear();
            }

            LanguageButtons();
            GUILayout.EndArea();
        }

        // The language applies from the next line on (a line already on screen is not re-resolved).
        private static void LanguageButtons()
        {
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                GUILayout.Label("Language: loading...");
                return;
            }

            GUILayout.Label($"Language: {LocalizationSettings.SelectedLocale}");
            GUILayout.BeginHorizontal();
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (GUILayout.Button(locale.Identifier.Code))
                {
                    LocalizationSettings.SelectedLocale = locale;
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
