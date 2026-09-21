using System;

namespace Game.Building
{
    public enum Axis : byte
    {
        /// <summary>An edge running from vertex (x, z) to (x + 1, z).</summary>
        X,

        /// <summary>An edge running from vertex (x, z) to (x, z + 1).</summary>
        Z,
    }

    public enum PieceKind : byte
    {
        Floor,
        Wall,
        Door,

        /// <summary>Corner post made automatically where walls meet; never placed by the player.</summary>
        Pillar,
    }

    /// <summary>
    /// Identifies one building piece on the grid. Coordinates by kind:
    /// Floor = cell (X, Z); Wall/Door = edge starting at vertex (X, Z) along
    /// <see cref="Axis"/>; Pillar = vertex (X, Z). Level counts floors upward
    /// (0 = ground floor). A wall and a door on the same edge share one slot.
    /// </summary>
    public readonly struct PieceKey : IEquatable<PieceKey>
    {
        public PieceKey(PieceKind kind, int x, int z, int level, Axis axis = Axis.X)
        {
            Kind = kind;
            X = x;
            Z = z;
            Level = level;
            Axis = kind == PieceKind.Wall || kind == PieceKind.Door ? axis : Axis.X;
        }

        public PieceKind Kind { get; }
        public int X { get; }
        public int Z { get; }
        public int Level { get; }
        public Axis Axis { get; }

        public bool IsEdgePiece => Kind == PieceKind.Wall || Kind == PieceKind.Door;

        public static PieceKey Floor(int x, int z, int level) => new(PieceKind.Floor, x, z, level);
        public static PieceKey Wall(int x, int z, Axis axis, int level) => new(PieceKind.Wall, x, z, level, axis);
        public static PieceKey Door(int x, int z, Axis axis, int level) => new(PieceKind.Door, x, z, level, axis);
        public static PieceKey Pillar(int x, int z, int level) => new(PieceKind.Pillar, x, z, level);

        /// <summary>The key of the slot this piece occupies: a wall and a door on one edge have the same slot.</summary>
        public PieceKey Slot => Kind == PieceKind.Door ? new PieceKey(PieceKind.Wall, X, Z, Level, Axis) : this;

        public PieceKey WithKind(PieceKind kind) => new(kind, X, Z, Level, Axis);

        public bool Equals(PieceKey other) =>
            Kind == other.Kind && X == other.X && Z == other.Z && Level == other.Level && Axis == other.Axis;

        public override bool Equals(object obj) => obj is PieceKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine((int)Kind, X, Z, Level, (int)Axis);

        public static bool operator ==(PieceKey a, PieceKey b) => a.Equals(b);
        public static bool operator !=(PieceKey a, PieceKey b) => !a.Equals(b);

        public override string ToString() =>
            IsEdgePiece ? $"{Kind}({X},{Z},{Axis},L{Level})" : $"{Kind}({X},{Z},L{Level})";
    }
}
