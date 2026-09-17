using System;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Finds the nearest usable IInteractable in range every frame and exposes
    /// it. Detection is fully separated from "what happens on key press"
    /// (PlayerInteractionController) and from "what the interaction does"
    /// (each IInteractable implementation) — three responsibilities, three
    /// classes.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private float range = 2.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        public IInteractable Current { get; private set; }
        public event Action<IInteractable> TargetChanged;

        private void Update()
        {
            var next = FindNearest();
            if (next == Current)
            {
                return;
            }

            Current = next;
            TargetChanged?.Invoke(Current);
        }

        private IInteractable FindNearest()
        {
            var hits = Physics.OverlapSphere(transform.position, range, interactableLayers);

            IInteractable best = null;
            float bestSqrDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent(out IInteractable candidate) || !candidate.CanInteract(gameObject))
                {
                    continue;
                }

                float sqrDistance = (hit.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
