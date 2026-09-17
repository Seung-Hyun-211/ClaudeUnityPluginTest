using UnityEngine;
using Game.Characters;
using Game.Combat;

namespace Game.AI
{
    /// <summary>
    /// Periodically scans for the closest hostile IDamageable in range. States
    /// only ever read DetectedTarget — they never see the detection algorithm
    /// (see ai-state-machine.md), which is what lets the same states run for
    /// any faction (Enemy hunting the player, or a Companion hunting Enemies).
    /// </summary>
    public class AiSensor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float detectionRadius = 10f;
        [SerializeField, Min(0f)] private float scanInterval = 0.25f;
        [SerializeField] private LayerMask detectionMask = ~0;

        private FactionMember selfFaction;
        private float scanTimer;

        public float DetectionRadius => detectionRadius;
        public IDamageable DetectedTarget { get; private set; }

        private void Awake()
        {
            selfFaction = GetComponent<FactionMember>();
        }

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer > 0f)
            {
                return;
            }

            scanTimer = scanInterval;
            Scan();
        }

        public void Scan()
        {
            DetectedTarget = FindClosestHostile();
        }

        private IDamageable FindClosestHostile()
        {
            if (selfFaction == null)
            {
                return null;
            }

            var colliders = Physics.OverlapSphere(transform.position, detectionRadius, detectionMask);
            IDamageable closest = null;
            float closestSqrDistance = float.MaxValue;

            foreach (var candidate in colliders)
            {
                if (!candidate.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
                {
                    continue;
                }

                if (!candidate.TryGetComponent(out FactionMember otherFaction) ||
                    !FactionUtility.IsHostileTo(selfFaction.Faction, otherFaction.Faction))
                {
                    continue;
                }

                float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = damageable;
                }
            }

            return closest;
        }
    }
}
