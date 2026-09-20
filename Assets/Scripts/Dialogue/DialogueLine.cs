using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>A line ready to be shown: the speaker and text are already in the current language.</summary>
    public readonly struct DialogueLine
    {
        public DialogueLine(string speaker, string text, Sprite portrait)
        {
            Speaker = speaker;
            Text = text;
            Portrait = portrait;
        }

        public string Speaker { get; }

        /// <summary>May contain effect markup (see DialogueMarkup).</summary>
        public string Text { get; }

        public Sprite Portrait { get; }
    }
}
