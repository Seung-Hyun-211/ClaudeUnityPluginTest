using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Anything the player can trigger with the interact key/action — a door,
    /// an NPC's dialogue, a pickup, and whatever gets added later. New kinds
    /// are new implementations of this interface; nothing that already
    /// consumes it (detector, input handler, UI prompt) needs to change.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short label for the interaction prompt UI, e.g. "문 열기", "대화하기".</summary>
        string PromptText { get; }

        /// <summary>Whether interacting would currently do anything (locked door, NPC mid-cutscene, ...).</summary>
        bool CanInteract(GameObject interactor);

        void Interact(GameObject interactor);
    }
}
