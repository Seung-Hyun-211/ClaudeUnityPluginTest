using UnityEngine;
using Game.Characters;
using Game.Characters.Player;

namespace Game.DebugHarness
{
    /// <summary>
    /// OnGUI harness for the real WASD/attack input path (PlayerInputHandler
    /// -> PlayerController -> CharacterMotor/PlayerLocomotion). Shows position,
    /// move speed and stamina so movement/sprint/jump are all visible without
    /// switching to the Scene view.
    /// </summary>
    public class PlayerMovementTestHarness : MonoBehaviour
    {
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private StaminaController stamina;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 380, 90), GUI.skin.box);
            GUILayout.Label("WASD move, Shift sprint, Space jump, click attack (no weapon equipped).");
            GUILayout.Label($"Position: {motor.transform.position:F2}   MoveSpeed: {motor.MoveSpeed:0.0}   Grounded: {motor.IsGrounded}");
            GUILayout.Label($"Stamina: {stamina.Stamina.Current:0}/{stamina.Stamina.Max:0}");
            GUILayout.EndArea();
        }
    }
}
