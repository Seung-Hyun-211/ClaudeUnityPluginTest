using System;
using System.Collections.Generic;

namespace Game.Building
{
    /// <summary>
    /// The structural rules of one kind of piece: when it may be placed, what
    /// is created with it, what depends on it, whether it is still held up.
    /// StructureGraph knows nothing about specific kinds - a new kind (stairs,
    /// ladder) is a new rule registered for it (open-closed).
    /// </summary>
    public interface IPieceRule
    {
        /// <summary>Whether the player may demolish this kind directly (corner pillars cannot be).</summary>
        bool CanDemolish { get; }

        /// <param name="groundSupport">Whether a ground-level piece may go on this cell (inside a build zone); null = anywhere.</param>
        PlacementFailure Check(StructureGraph graph, PieceKey key, Func<int, int, bool> groundSupport);

        /// <summary>Pieces created together with the placed one (an edge piece makes its corner pillars).</summary>
        IEnumerable<PieceKey> Companions(PieceKey placed);

        /// <summary>Pieces to re-check once this one is gone, because they may have depended on it.</summary>
        IEnumerable<PieceKey> Dependents(PieceKey removed);

        /// <summary>Whether the piece is still held up (called for queued dependents).</summary>
        bool IsSupported(StructureGraph graph, PieceKey key);

        /// <summary>Pieces that go down with this one when it is destroyed by damage (a pillar takes the walls at its vertex).</summary>
        IEnumerable<PieceKey> Collateral(StructureGraph graph, PieceKey destroyed);
    }

    /// <summary>The stage-1 rules: floors, walls/doors, corner pillars (documents/building-system.md 4.1).</summary>
    public static class PieceRules
    {
        public static Dictionary<PieceKind, IPieceRule> CreateDefault()
        {
            var edge = new EdgeRule();
            return new Dictionary<PieceKind, IPieceRule>
            {
                [PieceKind.Floor] = new FloorRule(),
                [PieceKind.Wall] = edge,
                [PieceKind.Door] = edge,
                [PieceKind.Pillar] = new PillarRule(),
            };
        }
    }

    /// <summary>A ground floor needs ground; a higher floor needs a wall/door or pillar of the level below on one of its edges/corners.</summary>
    public sealed class FloorRule : IPieceRule
    {
        public bool CanDemolish => true;

        public PlacementFailure Check(StructureGraph graph, PieceKey key, Func<int, int, bool> groundSupport)
        {
            if (graph.Contains(key))
            {
                return PlacementFailure.Occupied;
            }

            bool held = key.Level == 0 ? groundSupport?.Invoke(key.X, key.Z) ?? true : HeldUp(graph, key.X, key.Z, key.Level);
            return held ? PlacementFailure.None : PlacementFailure.Unsupported;
        }

        public IEnumerable<PieceKey> Companions(PieceKey placed) => Array.Empty<PieceKey>();

        // Walls and doors of this level stood on the floor.
        public IEnumerable<PieceKey> Dependents(PieceKey removed)
        {
            foreach (var (ex, ez, axis) in BuildGeometry.CellEdges(removed.X, removed.Z))
            {
                yield return PieceKey.Wall(ex, ez, axis, removed.Level);
            }
        }

        public bool IsSupported(StructureGraph graph, PieceKey key) => key.Level == 0 || HeldUp(graph, key.X, key.Z, key.Level);

        public IEnumerable<PieceKey> Collateral(StructureGraph graph, PieceKey destroyed) => Array.Empty<PieceKey>();

        private static bool HeldUp(StructureGraph graph, int x, int z, int level)
        {
            foreach (var (ex, ez, axis) in BuildGeometry.CellEdges(x, z))
            {
                if (graph.TryGetEdgePiece(ex, ez, axis, level - 1, out _))
                {
                    return true;
                }
            }

            foreach (var (vx, vz) in BuildGeometry.CellCorners(x, z))
            {
                if (graph.HasPillar(vx, vz, level - 1))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Walls and doors share one slot per grid line and stand on a floor of their own level next to them; a different kind on the same slot swaps in.</summary>
    public sealed class EdgeRule : IPieceRule
    {
        public bool CanDemolish => true;

        public PlacementFailure Check(StructureGraph graph, PieceKey key, Func<int, int, bool> groundSupport)
        {
            if (graph.TryGetEdgePiece(key.X, key.Z, key.Axis, key.Level, out var existing))
            {
                // A different kind swaps in (wall <-> door); the same kind is a duplicate.
                return existing == key.Kind ? PlacementFailure.Occupied : PlacementFailure.None;
            }

            return IsSupported(graph, key) ? PlacementFailure.None : PlacementFailure.Unsupported;
        }

        public IEnumerable<PieceKey> Companions(PieceKey placed)
        {
            var (sx, sz) = BuildGeometry.EdgeStart(placed.X, placed.Z, placed.Axis);
            var (ex, ez) = BuildGeometry.EdgeEnd(placed.X, placed.Z, placed.Axis);
            yield return PieceKey.Pillar(sx, sz, placed.Level);
            yield return PieceKey.Pillar(ex, ez, placed.Level);
        }

        // The floor above may have hung on it, and the pillars at its ends may no longer be needed.
        public IEnumerable<PieceKey> Dependents(PieceKey removed)
        {
            var (ax, az) = BuildGeometry.EdgeCellA(removed.X, removed.Z, removed.Axis);
            var (bx, bz) = BuildGeometry.EdgeCellB(removed.X, removed.Z, removed.Axis);
            yield return PieceKey.Floor(ax, az, removed.Level + 1);
            yield return PieceKey.Floor(bx, bz, removed.Level + 1);

            var (sx, sz) = BuildGeometry.EdgeStart(removed.X, removed.Z, removed.Axis);
            var (ex, ez) = BuildGeometry.EdgeEnd(removed.X, removed.Z, removed.Axis);
            yield return PieceKey.Pillar(sx, sz, removed.Level);
            yield return PieceKey.Pillar(ex, ez, removed.Level);
        }

        public bool IsSupported(StructureGraph graph, PieceKey key)
        {
            var (ax, az) = BuildGeometry.EdgeCellA(key.X, key.Z, key.Axis);
            var (bx, bz) = BuildGeometry.EdgeCellB(key.X, key.Z, key.Axis);
            return graph.HasFloor(ax, az, key.Level) || graph.HasFloor(bx, bz, key.Level);
        }

        public IEnumerable<PieceKey> Collateral(StructureGraph graph, PieceKey destroyed) => Array.Empty<PieceKey>();
    }

    /// <summary>
    /// A corner pillar exists exactly while some wall or door touches its
    /// vertex. It is not part of the support chain (that would make pillars
    /// and walls hold each other up), but a pillar destroyed by damage takes
    /// every wall and door at its vertex with it.
    /// </summary>
    public sealed class PillarRule : IPieceRule
    {
        public bool CanDemolish => false;

        public PlacementFailure Check(StructureGraph graph, PieceKey key, Func<int, int, bool> groundSupport) =>
            PlacementFailure.NotPlaceable;

        public IEnumerable<PieceKey> Companions(PieceKey placed) => Array.Empty<PieceKey>();

        // Floors above may have hung on the pillar.
        public IEnumerable<PieceKey> Dependents(PieceKey removed)
        {
            foreach (var (cx, cz) in BuildGeometry.VertexCells(removed.X, removed.Z))
            {
                yield return PieceKey.Floor(cx, cz, removed.Level + 1);
            }
        }

        public bool IsSupported(StructureGraph graph, PieceKey key)
        {
            foreach (var (ex, ez, axis) in BuildGeometry.VertexEdges(key.X, key.Z))
            {
                if (graph.TryGetEdgePiece(ex, ez, axis, key.Level, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<PieceKey> Collateral(StructureGraph graph, PieceKey destroyed)
        {
            foreach (var (ex, ez, axis) in BuildGeometry.VertexEdges(destroyed.X, destroyed.Z))
            {
                if (graph.TryGetEdgePiece(ex, ez, axis, destroyed.Level, out var kind))
                {
                    yield return new PieceKey(kind, ex, ez, destroyed.Level, axis);
                }
            }
        }
    }
}
