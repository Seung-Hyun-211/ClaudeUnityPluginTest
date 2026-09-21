using UnityEngine;
using Game.Interaction;

namespace Game.Building
{
    /// <summary>
    /// A door piece: a leaf on a hinge that swings open with the interact key.
    /// (The scene door, DoorInteractable, drives an Animator; a built door
    /// needs no animation asset, so it just turns the hinge.) Open state and
    /// swing direction are saved with the structure.
    /// </summary>
    public class BuildDoor : MonoBehaviour, IInteractable, IBuildPieceState
    {
        [Tooltip("Pivot at one end of the door, parent of the leaf and its collider.")]
        [SerializeField] private Transform hinge;

        [SerializeField, Range(0f, 120f)] private float openAngle = 95f;

        public bool IsOpen { get; private set; }

        /// <summary>Which way the door swings (the other side of the wall).</summary>
        public bool Flipped { get; private set; }

        public string PromptText => IsOpen ? "문 닫기" : "문 열기";

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor) => SetState(!IsOpen, Flipped);

        public void SetState(bool open, bool flipped)
        {
            IsOpen = open;
            Flipped = flipped;

            if (hinge != null)
            {
                float angle = open ? (flipped ? -openAngle : openAngle) : 0f;
                hinge.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        void IBuildPieceState.Initialize(bool flipped) => SetState(false, flipped);

        void IBuildPieceState.Capture(PieceRecord record)
        {
            record.doorOpen = IsOpen;
            record.doorFlipped = Flipped;
        }

        void IBuildPieceState.Restore(PieceRecord record) => SetState(record.doorOpen, record.doorFlipped);
    }
}
