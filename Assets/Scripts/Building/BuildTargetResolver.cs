using System;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// Turns where the player points into a grid target: casts the aim ray,
    /// ignores characters standing in the way, and snaps the hit to the piece
    /// being built. Also finds the piece being pointed at for demolition.
    /// </summary>
    public sealed class BuildTargetResolver
    {
        // A ground-level floor is flush with the ground, so both are hit at (nearly) the same distance.
        private const float FlushTolerance = 0.02f;

        // Aim a hair inside the surface: the top of a level-0 wall (y = 2.5) would otherwise read as the bottom of the level-1 slab.
        private const float InsideOffset = 0.05f;

        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly IAimSource aim;
        private readonly Func<Collider, bool> isCharacter;
        private readonly float rayLength;

        /// <param name="isCharacter">Which colliders are people (in the way of the ray, not part of the ground).</param>
        public BuildTargetResolver(IAimSource aim, Func<Collider, bool> isCharacter, float rayLength)
        {
            this.aim = aim ?? throw new ArgumentNullException(nameof(aim));
            this.isCharacter = isCharacter ?? throw new ArgumentNullException(nameof(isCharacter));
            this.rayLength = rayLength;
        }

        /// <summary>Snaps the aimed spot to a piece of this data's kind.</summary>
        public bool TrySnap(BuildPieceData data, Vector3 zoneOrigin, out PieceKey key)
        {
            key = default;
            if (!TryRaycast(out var hit))
            {
                return false;
            }

            key = BuildGrid.KeyAt(data.Kind, hit.point - hit.normal * InsideOffset, zoneOrigin);
            return true;
        }

        public bool TryAimAtPiece(out BuildPiece piece)
        {
            piece = null;
            if (!TryRaycast(out var hit))
            {
                return false;
            }

            piece = hit.collider.GetComponentInParent<BuildPiece>();
            return piece != null;
        }

        private bool TryRaycast(out RaycastHit result)
        {
            result = default;
            if (!aim.TryGetRay(out var ray))
            {
                return false;
            }

            int count = Physics.RaycastNonAlloc(ray, hits, rayLength, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance < best && !isCharacter(hits[i].collider))
                {
                    best = hits[i].distance;
                    result = hits[i];
                    found = true;
                }
            }

            // Take the piece over the flush ground - otherwise a floor could never be aimed at to demolish it.
            for (int i = 0; found && i < count; i++)
            {
                if (hits[i].distance <= best + FlushTolerance && hits[i].collider.GetComponentInParent<BuildPiece>() != null)
                {
                    result = hits[i];
                    break;
                }
            }

            return found;
        }
    }
}
