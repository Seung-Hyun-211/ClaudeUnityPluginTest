using UnityEngine;

namespace Game.Interaction
{
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        private static readonly int IsOpenParam = Animator.StringToHash("IsOpen");

        [SerializeField] private Animator animator;
        [SerializeField] private bool isLocked;

        private bool isOpen;

        public string PromptText => isLocked ? "잠김" : (isOpen ? "문 닫기" : "문 열기");
        public bool CanInteract(GameObject interactor) => !isLocked;

        public void Interact(GameObject interactor)
        {
            isOpen = !isOpen;
            animator.SetBool(IsOpenParam, isOpen);
        }
    }
}
