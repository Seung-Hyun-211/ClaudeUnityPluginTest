using System;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// Snapping a point in the world to a grid piece, and where a piece sits
    /// in the world (documents/building-system.md ch. 3). Pure math, no scene
    /// objects. The grid's origin is the corner of a build zone at ground
    /// height; +X and +Z are cell counts.
    ///
    /// Vertical layout per level: the floor slab's top is at
    /// <c>groundY + level * FloorHeight</c> and the slab reaches
    /// <see cref="SlabThickness"/> below it (the ground floor is sunk flush
    /// with the ground so nobody has to step up onto it); walls, doors and
    /// pillars stand on that top for <see cref="WallHeight"/>.
    /// </summary>
    public static class BuildGrid
    {
        public const float CellSize = 1f;
        public const float FloorHeight = 3f;
        public const float SlabThickness = 0.5f;
        public const float WallHeight = 2.5f;
        public const float WallThickness = 0.5f;
        public const float PillarWidth = 0.5f;

        /// <summary>The level whose pieces a point at height <paramref name="y"/> belongs to (slab sides count for their own level).</summary>
        public static int LevelAt(float y, float groundY) =>
            Mathf.FloorToInt((y - groundY + SlabThickness) / FloorHeight);

        public static float TopOf(int level, float groundY) => groundY + level * FloorHeight;

        /// <summary>Snaps a world point to the piece of the given kind nearest to it.</summary>
        public static PieceKey KeyAt(PieceKind kind, Vector3 worldPoint, Vector3 origin)
        {
            float px = (worldPoint.x - origin.x) / CellSize;
            float pz = (worldPoint.z - origin.z) / CellSize;
            int level = Mathf.Max(0, LevelAt(worldPoint.y, origin.y));

            switch (kind)
            {
                case PieceKind.Floor:
                    return PieceKey.Floor(Mathf.FloorToInt(px), Mathf.FloorToInt(pz), level);

                case PieceKind.Wall:
                case PieceKind.Door:
                {
                    var (x, z, axis) = EdgeAt(px, pz);
                    return new PieceKey(kind, x, z, level, axis);
                }

                default:
                    throw new ArgumentException("Pillars are made automatically and cannot be aimed at.", nameof(kind));
            }
        }

        /// <summary>The grid line nearest to a point (in cell units): the edge it lies along.</summary>
        public static (int x, int z, Axis axis) EdgeAt(float px, float pz)
        {
            int rx = RoundHalfUp(px);
            int rz = RoundHalfUp(pz);
            float dx = Mathf.Abs(px - rx);
            float dz = Mathf.Abs(pz - rz);

            return dx <= dz
                ? (rx, Mathf.FloorToInt(pz), Axis.Z)
                : (Mathf.FloorToInt(px), rz, Axis.X);
        }

        public static Vector3 Size(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Floor:
                    return new Vector3(CellSize, SlabThickness, CellSize);
                case PieceKind.Pillar:
                    return new Vector3(PillarWidth, WallHeight, PillarWidth);
                default:
                    return new Vector3(CellSize, WallHeight, WallThickness);
            }
        }

        /// <summary>World position of the piece's center. Wall and door prefabs are authored along X and turned by <see cref="Rotation"/> for Z edges.</summary>
        public static Vector3 Center(PieceKey key, Vector3 origin)
        {
            float top = TopOf(key.Level, origin.y);
            float standingY = top + WallHeight * 0.5f;

            switch (key.Kind)
            {
                case PieceKind.Floor:
                    return new Vector3(origin.x + (key.X + 0.5f) * CellSize, top - SlabThickness * 0.5f, origin.z + (key.Z + 0.5f) * CellSize);
                case PieceKind.Pillar:
                    return new Vector3(origin.x + key.X * CellSize, standingY, origin.z + key.Z * CellSize);
                default:
                    return key.Axis == Axis.X
                        ? new Vector3(origin.x + (key.X + 0.5f) * CellSize, standingY, origin.z + key.Z * CellSize)
                        : new Vector3(origin.x + key.X * CellSize, standingY, origin.z + (key.Z + 0.5f) * CellSize);
            }
        }

        public static Quaternion Rotation(PieceKey key) =>
            key.IsEdgePiece && key.Axis == Axis.Z ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;

        // Math.Round would round exact halves to even, which makes ties flip between neighbours.
        private static int RoundHalfUp(float value) => Mathf.FloorToInt(value + 0.5f);
    }
}
