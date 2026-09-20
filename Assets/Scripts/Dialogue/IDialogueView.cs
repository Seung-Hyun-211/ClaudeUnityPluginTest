using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>
    /// Presentation of a running sequence. The runner/player only talk to this,
    /// so a cinematic (letterbox + subtitle) presentation can replace the
    /// text-box one later without touching playback logic.
    /// </summary>
    public interface IDialogueView
    {
        bool IsLogOpen { get; }

        void Show(bool skippable);
        void Hide();

        /// <summary>Types the line out; calls onFullyShown once the whole text is visible.</summary>
        void ShowLine(string speaker, string text, Sprite portrait, Action onFullyShown);

        /// <summary>Reveals the whole current line at once (Submit while typing).</summary>
        void CompleteTyping();

        /// <summary>Labels of the visible options in the current language (may carry effect markup); onChosen gets the index into this list.</summary>
        void ShowChoices(IReadOnlyList<string> labels, Action<int> onChosen);
        void MoveChoiceFocus(int delta);
        void ConfirmFocusedChoice();

        void ToggleLog(IReadOnlyList<DialogueLogEntry> log);
    }
}
