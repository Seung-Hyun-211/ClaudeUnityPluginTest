using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// Snapping a point in the world to a grid piece, and where a piece sits
    /// in the world (documents/building-system.md ch. 3). Pure math, no scene
    /// objects; the per-kind shapes live in <see cref="IPieceGeometry"/>. The
    /// origin of the grid is the corner of a build zone at ground height; +X
    /// and +Z are cell counts.
    ///
    /// Vertical layout per level: the floor slab top is at
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

            return PieceGeometries.Default.For(kind).KeyAt(kind, px, pz, level);
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

        public static Vector3 Size(PieceKind kind) => PieceGeometries.Default.For(kind).Size;

        /// <summary>World position of the piece center.</summary>
        public static Vector3 Center(PieceKey key, Vector3 origin) => PieceGeometries.Default.For(key.Kind).Center(key, origin);

        public static Quaternion Rotation(PieceKey key) => PieceGeometries.Default.For(key.Kind).Rotation(key);

        // Math.Round would round exact halves to even, which makes ties flip between neighbours.
        private static int RoundHalfUp(float value) => Mathf.FloorToInt(value + 0.5f);
    }
}
