using UnityEngine;

namespace Game.HUD.Minimap
{
    /// <summary>
    /// Top-down orthographic camera that follows followTarget and renders into
    /// a RenderTexture for MinimapUIView to display. followTarget is a plain
    /// Transform (not a character controller type) to stay decoupled from the
    /// in-progress Characters track. orientToTargetYaw picks between
    /// north-up and player-heading-up without MinimapUIView needing to know —
    /// documents/hud-system.md leaves the concrete choice as a follow-up.
    /// </summary>
    public class MinimapCameraRig : MonoBehaviour
    {
        [SerializeField] private Camera topDownCamera;
        [SerializeField] private RenderTexture output;
        [SerializeField] private Transform followTarget;
        [SerializeField] private float height = 20f;
        [SerializeField] private bool orientToTargetYaw = false;

        public RenderTexture Output => output;
        public float CurrentYaw => topDownCamera.transform.eulerAngles.y;

        private void LateUpdate()
        {
            Vector3 position = followTarget.position;
            position.y += height;

            float yaw = orientToTargetYaw ? followTarget.eulerAngles.y : 0f;
            topDownCamera.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, yaw, 0f));
        }
    }
}
