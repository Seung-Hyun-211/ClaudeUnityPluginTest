using UnityEngine;
using UnityEngine.InputSystem;
using Game.UI.Windows;

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
        [SerializeField] private WindowManager windowManager;

        private void Update()
        {
            // design-conflict-review.md #3: UI가 열려 있으면 F로 상호작용이
            // 발동하면 안 된다.
            if (Keyboard.current == null || !Keyboard.current[interactKey].wasPressedThisFrame || windowManager.IsAnyWindowOpen)
            {
                return;
            }

            detector.Current?.Interact(gameObject);
        }
    }
}
