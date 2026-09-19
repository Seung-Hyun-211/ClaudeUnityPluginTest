using System;
using UnityEngine;

namespace Game.Dialogue
{
    [Serializable]
    public class DialogueChoiceOption
    {
        public string text;
        public string nextNodeId;
        [Tooltip("Option is hidden while this is not met. Empty flag = always shown.")]
        public DialogueCondition condition = new() { expected = true };
    }
}
