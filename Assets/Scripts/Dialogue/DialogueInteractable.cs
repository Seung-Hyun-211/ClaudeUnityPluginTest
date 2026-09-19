using UnityEngine;
using Game.Interaction;

namespace Game.Dialogue
{
    /// <summary>
    /// Starts a DialogueSequence with this NPC through the scene's
    /// DialoguePlayer. Sits next to NpcController on Village NPC prefabs; the
    /// sequence is assigned per instance.
    /// </summary>
    public class DialogueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcName;
        [SerializeField] private DialogueSequence sequence;

        public string PromptText => $"{npcName}와 대화하기";

        public bool CanInteract(GameObject interactor)
        {
            var player = DialoguePlayer.Instance;
            return sequence != null && player != null && !player.IsPlaying;
        }

        public void Interact(GameObject interactor)
        {
            DialoguePlayer.Instance?.Play(sequence, interactor);
        }
    }
}
