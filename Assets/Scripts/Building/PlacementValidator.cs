using System;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// Decides whether a piece may be placed at a target: the structure and
    /// zone rules, the player reach, people standing in the way, and the
    /// materials (documents/building-system.md 7). The first thing that fails
    /// is the reason reported.
    /// </summary>
    public sealed class PlacementValidator
    {
        // Pieces overlap each other by design (pillars, wall ends), so only people matter, and a slightly shrunk box is used.
        private const float BoxShrink = 0.45f;

        private readonly Collider[] overlaps = new Collider[64];
        private readonly StructureManager structures;
        private readonly BuildEconomy economy;
        private readonly Func<Collider, bool> isCharacter;
        private readonly Transform player;
        private readonly float maxReach;

        public PlacementValidator(StructureManager structures, BuildEconomy economy, Func<Collider, bool> isCharacter, Transform player, float maxReach)
        {
            this.structures = structures ?? throw new ArgumentNullException(nameof(structures));
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.isCharacter = isCharacter ?? throw new ArgumentNullException(nameof(isCharacter));
            this.player = player;
            this.maxReach = maxReach;
        }

        public bool InReach(Vector3 point) => player == null || Vector3.Distance(player.position, point) <= maxReach;

        public PlacementStatus Validate(PieceKey key, BuildPieceData data)
        {
            switch (structures.Check(key))
            {
                case PlacementFailure.Occupied:
                    return PlacementStatus.Occupied;
                case PlacementFailure.Unsupported:
                    return structures.Zone.ContainsCell(key.X, key.Z) || key.IsEdgePiece ? PlacementStatus.Unsupported : PlacementStatus.OutOfZone;
                case PlacementFailure.NotPlaceable:
                    return PlacementStatus.OutOfZone;
            }

            Vector3 center = BuildGrid.Center(key, structures.Zone.Origin);
            if (!InReach(center))
            {
                return PlacementStatus.TooFar;
            }

            if (CharacterInTheWay(key, center))
            {
                return PlacementStatus.Blocked;
            }

            return economy.CanAfford(data) ? PlacementStatus.Ok : PlacementStatus.NoMaterials;
        }

        private bool CharacterInTheWay(PieceKey key, Vector3 center)
        {
            Vector3 half = BuildGrid.Size(key.Kind) * BoxShrink;
            int count = Physics.OverlapBoxNonAlloc(center, half, overlaps, BuildGrid.Rotation(key), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (isCharacter(overlaps[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
