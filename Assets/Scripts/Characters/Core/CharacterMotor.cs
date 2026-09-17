using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Shared movement implementation for Player and Enemy alike (Player calls
    /// it from input, Enemy AI states call it) — actual pathfinding/NavMesh
    /// integration is a later task, this is a minimal working mover that
    /// satisfies the MoveTo/Stop contract (see character-system.md).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CharacterMotor : MonoBehaviour
    {
        [SerializeField] private CharacterStatsData statsData;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float stoppingDistance = 0.1f;
        [SerializeField, Min(0f)] private float rotationSpeedDegrees = 720f;

        private Rigidbody body;
        private Vector3? targetPosition;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        public bool IsMoving => targetPosition.HasValue;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.freezeRotation = true;

            if (statsData != null)
            {
                moveSpeed = statsData.MoveSpeed;
            }
        }

        public void MoveTo(Vector3 destination)
        {
            targetPosition = destination;
        }

        public void Stop()
        {
            targetPosition = null;
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
        }

        private void FixedUpdate()
        {
            if (!targetPosition.HasValue)
            {
                return;
            }

            Vector3 toTarget = targetPosition.Value - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= stoppingDistance)
            {
                Stop();
                return;
            }

            Vector3 direction = toTarget.normalized;
            Vector3 horizontalVelocity = direction * moveSpeed;
            body.linearVelocity = new Vector3(horizontalVelocity.x, body.linearVelocity.y, horizontalVelocity.z);

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeedDegrees * Time.fixedDeltaTime);
        }
    }
}
