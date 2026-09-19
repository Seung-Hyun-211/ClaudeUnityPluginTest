using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Minimal placeholder proving the extension point for a second, very
    /// different interaction kind. Opening an actual dialogue UI/tree is a
    /// follow-up (see documents/npc-roles.md).
    /// </summary>
    public class DialogueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcName;

        public string PromptText => $"{npcName}와 대화하기";
        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            // TODO: 대화 UI/트리 연동.
        }
    }
}
