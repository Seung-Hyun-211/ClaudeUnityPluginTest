using UnityEngine;
using UnityEngine.InputSystem;
using Game.Characters;

namespace Game.Characters.Player
{
    /// <summary>
    /// Player-only extension of CharacterMotor: gates Sprint/Jump behind
    /// StaminaController + PlayerActionCosts, then drives the actual
    /// speed change / jump physics (see player-attributes.md).
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private Rigidbody body;
        [SerializeField] private StaminaController stamina;
        [SerializeField] private PlayerActionCosts actionCosts;
        [SerializeField, Min(1f)] private float sprintSpeedMultiplier = 1.6f;
        [SerializeField, Min(0f)] private float jumpImpulse = 5f;
        [SerializeField] private Key sprintKey = Key.LeftShift;
        [SerializeField] private Key jumpKey = Key.Space;

        private float walkSpeed;
        private bool sprintHeld;

        private void Awake()
        {
            walkSpeed = motor.MoveSpeed;
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                SetSprintHeld(Keyboard.current[sprintKey].isPressed);

                if (Keyboard.current[jumpKey].wasPressedThisFrame)
                {
                    TryJump();
                }
            }

            if (sprintHeld)
            {
                float cost = actionCosts.GetCost(PlayerActionType.Sprint) * Time.deltaTime;
                if (stamina.TryConsume(cost))
                {
                    motor.MoveSpeed = walkSpeed * sprintSpeedMultiplier;
                }
                else
                {
                    SetSprintHeld(false);
                }
            }
        }

        public void SetSprintHeld(bool held)
        {
            if (sprintHeld == held)
            {
                return;
            }

            sprintHeld = held;
            if (!held)
            {
                motor.MoveSpeed = walkSpeed;
            }
        }

        public bool TryJump()
        {
            if (!stamina.TryConsume(actionCosts.GetCost(PlayerActionType.Jump)))
            {
                return false;
            }

            body.AddForce(Vector3.up * jumpImpulse, ForceMode.VelocityChange);
            return true;
        }
    }
}
