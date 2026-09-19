using System;
using UnityEngine;
using Game.Items;

namespace Game.Dialogue
{
    /// <summary>
    /// One node of a sequence. A single flat serializable class (fields used
    /// depend on <see cref="type"/>) instead of a subclass per node kind:
    /// Unity has no built-in inspector for polymorphic [SerializeReference]
    /// lists, so a flat class stays editable and JSON-friendly. An empty
    /// next-node id ends the sequence.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        public string id;
        public DialogueNodeType type;

        // Line
        public string speakerName;
        [TextArea(2, 5)] public string text;
        public Sprite portrait;

        // Line / Event / Wait: the following node. Branch: node when the condition is met.
        public string nextNodeId;

        // Choice
        public DialogueChoiceOption[] choices;

        // Branch
        public DialogueCondition condition = new() { expected = true };
        public string nextNodeIfFalseId;

        // Event
        public DialogueEventType eventType;
        [Tooltip("SetFlag: flag name. Quest/shop events: the quest/shop id.")]
        public string eventParam;
        public bool eventFlagValue = true;
        public ItemData eventItem;
        [Min(1)] public int eventCount = 1;
        [Tooltip("Modal events only (quest offer, ...): node when the player declines. Empty = same as nextNodeId.")]
        public string nextNodeIfDeclinedId;

        // Wait
        [Min(0f)] public float duration;
    }
}
