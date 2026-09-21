using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Building
{
    /// <summary>A ray through the middle of the screen, or through the mouse cursor (top-down test scenes and unlocked-cursor play).</summary>
    public class CameraAimSource : MonoBehaviour, IAimSource
    {
        [Tooltip("Empty = the main camera.")]
        [SerializeField] private Camera aimCamera;

        [SerializeField] private bool useMousePosition;

        public bool TryGetRay(out Ray ray)
        {
            var cam = aimCamera != null ? aimCamera : Camera.main;
            if (cam == null)
            {
                ray = default;
                return false;
            }

            Vector2 point = useMousePosition && Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            ray = cam.ScreenPointToRay(point);
            return true;
        }
    }
}
