using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Interaction
{
    /// <summary>
    /// Reads the interact key and fires it at whatever InteractionDetector is
    /// currently pointing at. Rebinding the key never touches detection or any
    /// IInteractable implementation.
    /// </summary>
    public class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private InteractionDetector detector;
        [SerializeField] private Key interactKey = Key.F;

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current[interactKey].wasPressedThisFrame)
            {
                return;
            }

            detector.Current?.Interact(gameObject);
        }
    }
}
