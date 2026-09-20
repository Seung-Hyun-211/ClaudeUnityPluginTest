using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Game.Dialogue
{
    [Serializable]
    public class DialogueChoiceOption
    {
        [Tooltip("Source-language text; also the fallback when there is no translation.")]
        public string text;
        public LocalizedString localizedText;
        public string nextNodeId;
        [Tooltip("Option is hidden while this is not met. Empty flag = always shown.")]
        public DialogueCondition condition = new() { expected = true };
    }
}
