using System.Text;
using UnityEngine;
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
            GUILayout.BeginArea(new Rect(10, 10, 360, 220), GUI.skin.box);
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
            GUILayout.EndArea();
        }
    }
}
