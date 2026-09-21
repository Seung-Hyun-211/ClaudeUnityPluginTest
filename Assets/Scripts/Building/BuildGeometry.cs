using System.Collections.Generic;

namespace Game.Building
{
    /// <summary>
    /// Which cells, edges and vertices touch which on the grid (see
    /// documents/building-system.md 3.1). Everything is integer math, so the
    /// structure rules built on it are exact and testable.
    /// </summary>
    public static class BuildGeometry
    {
        /// <summary>The two cells an edge separates.</summary>
        public static (int x, int z) EdgeCellA(int x, int z, Axis axis) => axis == Axis.X ? (x, z - 1) : (x - 1, z);

        public static (int x, int z) EdgeCellB(int x, int z, Axis axis) => (x, z);

        /// <summary>The two vertices an edge connects.</summary>
        public static (int x, int z) EdgeStart(int x, int z, Axis axis) => (x, z);

        public static (int x, int z) EdgeEnd(int x, int z, Axis axis) => axis == Axis.X ? (x + 1, z) : (x, z + 1);

        /// <summary>The four edges around a cell.</summary>
        public static IEnumerable<(int x, int z, Axis axis)> CellEdges(int x, int z)
        {
            yield return (x, z, Axis.X);
            yield return (x, z + 1, Axis.X);
            yield return (x, z, Axis.Z);
            yield return (x + 1, z, Axis.Z);
        }

        /// <summary>The four corner vertices of a cell.</summary>
        public static IEnumerable<(int x, int z)> CellCorners(int x, int z)
        {
            yield return (x, z);
            yield return (x + 1, z);
            yield return (x, z + 1);
            yield return (x + 1, z + 1);
        }

        /// <summary>The four edges that end at (or start from) a vertex.</summary>
        public static IEnumerable<(int x, int z, Axis axis)> VertexEdges(int x, int z)
        {
            yield return (x - 1, z, Axis.X);
            yield return (x, z, Axis.X);
            yield return (x, z - 1, Axis.Z);
            yield return (x, z, Axis.Z);
        }

        /// <summary>The four cells that meet at a vertex.</summary>
        public static IEnumerable<(int x, int z)> VertexCells(int x, int z)
        {
            yield return (x - 1, z - 1);
            yield return (x, z - 1);
            yield return (x - 1, z);
            yield return (x, z);
        }
    }
}
