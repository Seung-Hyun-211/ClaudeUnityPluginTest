using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialogue
{
    [CreateAssetMenu(menuName = "Dialogue/Sequence", fileName = "New Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        [SerializeField] private string sequenceId;
        [SerializeField] private bool skippable = true;
        [SerializeField] private string entryNodeId;
        [SerializeField] private DialogueNode[] nodes = Array.Empty<DialogueNode>();

        [NonSerialized] private Dictionary<string, DialogueNode> nodeById;

        public string SequenceId => sequenceId;

        /// <summary>False blocks the hold-to-skip input entirely (key story beats).</summary>
        public bool Skippable => skippable;
        public string EntryNodeId => entryNodeId;

        public bool TryGetNode(string id, out DialogueNode node)
        {
            if (nodeById == null)
            {
                nodeById = new Dictionary<string, DialogueNode>();
                foreach (var candidate in nodes)
                {
                    nodeById[candidate.id] = candidate;
                }
            }

            return nodeById.TryGetValue(id, out node);
        }

        private void OnValidate() => nodeById = null;
    }
}
