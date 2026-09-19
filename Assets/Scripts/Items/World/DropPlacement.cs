using System;
using UnityEngine;
using Game.Interaction;

namespace Game.Items
{
    /// <summary>Where dropped items end up: spread around a centre, then resting on the ground.</summary>
    public static class DropPlacement
    {
        private const float GoldenAngle = 2.39996323f;

        /// <summary>
        /// Offset on the ground plane for item [index] of [count] - a sunflower
        /// spiral, which keeps neighbours about [spacing] apart however many
        /// there are. A single item stays exactly on the centre.
        /// </summary>
        public static Vector2 ScatterOffset(int index, int count, float spacing)
        {
            if (count <= 1)
            {
                return Vector2.zero;
            }

            float radius = spacing * Mathf.Sqrt(index + 0.5f);
            float angle = index * GoldenAngle;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        /// <summary>
        /// Looks straight down for the ground. Trigger colliders, anything
        /// with a Rigidbody (characters) and anything interactable (other
        /// pickups, NPCs, doors) are skipped, so an item never lands on top of
        /// another item or a person's head - whatever layer they are on.
        /// </summary>
        public static bool TryFindGround(Vector3 position, float probeHeight, float probeDistance, LayerMask mask, out float groundY)
        {
            var origin = position + Vector3.up * probeHeight;
            var hits = Physics.RaycastAll(origin, Vector3.down, probeHeight + probeDistance, mask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.rigidbody == null && hit.collider.GetComponentInParent<IInteractable>() == null)
                {
                    groundY = hit.point.y;
                    return true;
                }
            }

            groundY = 0f;
            return false;
        }

        /// <summary>Moves the object vertically so its lowest point rests on the ground.</summary>
        public static void RestOnGround(GameObject go, float groundY)
        {
            if (!TryGetLowestY(go, out float lowestY))
            {
                return;
            }

            go.transform.position += Vector3.up * (groundY - lowestY);
        }

        private static bool TryGetLowestY(GameObject go, out float lowestY)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                lowestY = bounds.min.y;
                return true;
            }

            var colliders = go.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                var bounds = colliders[0].bounds;
                foreach (var collider in colliders)
                {
                    bounds.Encapsulate(collider.bounds);
                }
                lowestY = bounds.min.y;
                return true;
            }

            lowestY = 0f;
            return false;
        }
    }
}
