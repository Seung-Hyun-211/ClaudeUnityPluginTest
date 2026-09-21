using System;
using System.Collections.Generic;

namespace Game.Building
{
    public enum PlacementFailure
    {
        None,

        /// <summary>The same piece is already there.</summary>
        Occupied,

        /// <summary>Nothing holds it up (documents/building-system.md 4.1).</summary>
        Unsupported,

        /// <summary>Pillars are made automatically, and levels below 0 do not exist.</summary>
        NotPlaceable,
    }

    public readonly struct PlaceResult
    {
        private PlaceResult(PlacementFailure failure, IReadOnlyList<PieceKey> added, IReadOnlyList<PieceKey> replaced)
        {
            Failure = failure;
            Added = added;
            Replaced = replaced;
        }

        public bool Success => Failure == PlacementFailure.None;
        public PlacementFailure Failure { get; }

        /// <summary>The piece itself plus any corner pillars created for it.</summary>
        public IReadOnlyList<PieceKey> Added { get; }

        /// <summary>The wall or door that was swapped out, if any.</summary>
        public IReadOnlyList<PieceKey> Replaced { get; }

        public static PlaceResult Fail(PlacementFailure failure) =>
            new(failure, Array.Empty<PieceKey>(), Array.Empty<PieceKey>());

        public static PlaceResult Ok(IReadOnlyList<PieceKey> added, IReadOnlyList<PieceKey> replaced) =>
            new(PlacementFailure.None, added, replaced);
    }

    /// <summary>
    /// The topology of what has been built: which floors, walls, doors and
    /// corner pillars exist, whether a new piece would be held up, and what
    /// falls down when something goes (documents/building-system.md ch. 4).
    /// Pure logic - no physics, materials or GameObjects - so every rule is
    /// covered by EditMode tests.
    ///
    /// Rules: a level-0 floor needs ground (decided by the caller); a higher
    /// floor needs a wall/door or pillar of the level below on one of its
    /// edges/corners; a wall/door needs a floor of its own level next to it.
    /// Pillars are derived: one exists exactly while some wall/door touches
    /// its vertex, and is not part of the support chain - but when a pillar is
    /// destroyed by damage the walls at that vertex go with it.
    /// </summary>
    public sealed class StructureGraph
    {
        private readonly HashSet<PieceKey> floors = new();
        private readonly Dictionary<PieceKey, PieceKind> edges = new(); // key = wall-kind slot, value = Wall or Door
        private readonly HashSet<PieceKey> pillars = new();

        public int Count => floors.Count + edges.Count + pillars.Count;

        public bool Contains(PieceKey key)
        {
            switch (key.Kind)
            {
                case PieceKind.Floor:
                    return floors.Contains(key);
                case PieceKind.Pillar:
                    return pillars.Contains(key);
                default:
                    return edges.TryGetValue(key.Slot, out var kind) && kind == key.Kind;
            }
        }

        public bool HasFloor(int x, int z, int level) => floors.Contains(PieceKey.Floor(x, z, level));

        public bool HasPillar(int x, int z, int level) => pillars.Contains(PieceKey.Pillar(x, z, level));

        public bool TryGetEdgePiece(int x, int z, Axis axis, int level, out PieceKind kind) =>
            edges.TryGetValue(PieceKey.Wall(x, z, axis, level), out kind);

        public IEnumerable<PieceKey> All()
        {
            foreach (var floor in floors)
            {
                yield return floor;
            }

            foreach (var pair in edges)
            {
                yield return pair.Key.WithKind(pair.Value);
            }

            foreach (var pillar in pillars)
            {
                yield return pillar;
            }
        }

        /// <param name="groundSupport">Whether a level-0 floor may go on this cell (inside a build zone); null = anywhere.</param>
        public PlacementFailure Check(PieceKey key, Func<int, int, bool> groundSupport = null)
        {
            if (key.Kind == PieceKind.Pillar || key.Level < 0)
            {
                return PlacementFailure.NotPlaceable;
            }

            if (key.Kind == PieceKind.Floor)
            {
                if (floors.Contains(key))
                {
                    return PlacementFailure.Occupied;
                }

                bool held = key.Level == 0 ? groundSupport?.Invoke(key.X, key.Z) ?? true : FloorHeldUp(key.X, key.Z, key.Level);
                return held ? PlacementFailure.None : PlacementFailure.Unsupported;
            }

            if (edges.TryGetValue(key.Slot, out var existing))
            {
                // A different kind swaps in (wall <-> door); the same kind is a duplicate.
                return existing == key.Kind ? PlacementFailure.Occupied : PlacementFailure.None;
            }

            return EdgeStandsOnFloor(key.X, key.Z, key.Axis, key.Level) ? PlacementFailure.None : PlacementFailure.Unsupported;
        }

        public PlaceResult TryPlace(PieceKey key, Func<int, int, bool> groundSupport = null)
        {
            var failure = Check(key, groundSupport);
            if (failure != PlacementFailure.None)
            {
                return PlaceResult.Fail(failure);
            }

            var added = new List<PieceKey>();
            var replaced = new List<PieceKey>();

            if (key.Kind == PieceKind.Floor)
            {
                floors.Add(key);
                added.Add(key);
                return PlaceResult.Ok(added, replaced);
            }

            var slot = key.Slot;
            if (edges.TryGetValue(slot, out var old))
            {
                replaced.Add(slot.WithKind(old));
            }

            edges[slot] = key.Kind;
            added.Add(key);

            var (sx, sz) = BuildGeometry.EdgeStart(key.X, key.Z, key.Axis);
            var (ex, ez) = BuildGeometry.EdgeEnd(key.X, key.Z, key.Axis);
            foreach (var (vx, vz) in new[] { (sx, sz), (ex, ez) })
            {
                var pillar = PieceKey.Pillar(vx, vz, key.Level);
                if (pillars.Add(pillar))
                {
                    added.Add(pillar);
                }
            }

            return PlaceResult.Ok(added, replaced);
        }

        /// <summary>
        /// Demolishes a floor, wall or door and everything that then loses its
        /// support. Pillars cannot be demolished directly (use
        /// <see cref="DestroyPillar"/> for damage).
        /// </summary>
        /// <returns>Every piece removed, the demolished one first; empty when it was not there.</returns>
        public IReadOnlyList<PieceKey> Remove(PieceKey key)
        {
            var removed = new List<PieceKey>();
            if (key.Kind == PieceKind.Pillar || !Contains(key))
            {
                return removed;
            }

            var pending = new Queue<PieceKey>();
            RemoveOne(key, removed, pending);
            Settle(removed, pending);
            return removed;
        }

        /// <summary>A pillar destroyed by damage: it goes, every wall and door touching its vertex goes, and whatever they held up falls.</summary>
        public IReadOnlyList<PieceKey> DestroyPillar(PieceKey pillar)
        {
            var removed = new List<PieceKey>();
            if (pillar.Kind != PieceKind.Pillar || !pillars.Contains(pillar))
            {
                return removed;
            }

            var pending = new Queue<PieceKey>();
            RemoveOne(pillar, removed, pending);

            foreach (var (ex, ez, axis) in BuildGeometry.VertexEdges(pillar.X, pillar.Z))
            {
                var slot = PieceKey.Wall(ex, ez, axis, pillar.Level);
                if (edges.TryGetValue(slot, out var kind))
                {
                    RemoveOne(slot.WithKind(kind), removed, pending);
                }
            }

            Settle(removed, pending);
            return removed;
        }

        private bool EdgeStandsOnFloor(int x, int z, Axis axis, int level)
        {
            var (ax, az) = BuildGeometry.EdgeCellA(x, z, axis);
            var (bx, bz) = BuildGeometry.EdgeCellB(x, z, axis);
            return HasFloor(ax, az, level) || HasFloor(bx, bz, level);
        }

        private bool FloorHeldUp(int x, int z, int level)
        {
            foreach (var (ex, ez, axis) in BuildGeometry.CellEdges(x, z))
            {
                if (edges.ContainsKey(PieceKey.Wall(ex, ez, axis, level - 1)))
                {
                    return true;
                }
            }

            foreach (var (vx, vz) in BuildGeometry.CellCorners(x, z))
            {
                if (HasPillar(vx, vz, level - 1))
                {
                    return true;
                }
            }

            return false;
        }

        private bool PillarNeeded(int x, int z, int level)
        {
            foreach (var (ex, ez, axis) in BuildGeometry.VertexEdges(x, z))
            {
                if (edges.ContainsKey(PieceKey.Wall(ex, ez, axis, level)))
                {
                    return true;
                }
            }

            return false;
        }

        // Removes one piece and queues what may have depended on it.
        private void RemoveOne(PieceKey key, List<PieceKey> removed, Queue<PieceKey> pending)
        {
            switch (key.Kind)
            {
                case PieceKind.Floor:
                    if (floors.Remove(key))
                    {
                        removed.Add(key);
                        foreach (var (ex, ez, axis) in BuildGeometry.CellEdges(key.X, key.Z))
                        {
                            pending.Enqueue(PieceKey.Wall(ex, ez, axis, key.Level));
                        }
                    }
                    break;

                case PieceKind.Pillar:
                    if (pillars.Remove(key))
                    {
                        removed.Add(key);
                        foreach (var (cx, cz) in BuildGeometry.VertexCells(key.X, key.Z))
                        {
                            pending.Enqueue(PieceKey.Floor(cx, cz, key.Level + 1));
                        }
                    }
                    break;

                default:
                    if (edges.Remove(key.Slot))
                    {
                        removed.Add(key);

                        var (ax, az) = BuildGeometry.EdgeCellA(key.X, key.Z, key.Axis);
                        var (bx, bz) = BuildGeometry.EdgeCellB(key.X, key.Z, key.Axis);
                        pending.Enqueue(PieceKey.Floor(ax, az, key.Level + 1));
                        pending.Enqueue(PieceKey.Floor(bx, bz, key.Level + 1));

                        var (sx, sz) = BuildGeometry.EdgeStart(key.X, key.Z, key.Axis);
                        var (ex, ez) = BuildGeometry.EdgeEnd(key.X, key.Z, key.Axis);
                        pending.Enqueue(PieceKey.Pillar(sx, sz, key.Level));
                        pending.Enqueue(PieceKey.Pillar(ex, ez, key.Level));
                    }
                    break;
            }
        }

        // Re-checks queued pieces until nothing more falls.
        private void Settle(List<PieceKey> removed, Queue<PieceKey> pending)
        {
            while (pending.Count > 0)
            {
                var key = pending.Dequeue();
                switch (key.Kind)
                {
                    case PieceKind.Floor:
                        if (floors.Contains(key) && key.Level > 0 && !FloorHeldUp(key.X, key.Z, key.Level))
                        {
                            RemoveOne(key, removed, pending);
                        }
                        break;

                    case PieceKind.Pillar:
                        if (pillars.Contains(key) && !PillarNeeded(key.X, key.Z, key.Level))
                        {
                            RemoveOne(key, removed, pending);
                        }
                        break;

                    default:
                        if (edges.TryGetValue(key.Slot, out var kind) && !EdgeStandsOnFloor(key.X, key.Z, key.Axis, key.Level))
                        {
                            RemoveOne(key.Slot.WithKind(kind), removed, pending);
                        }
                        break;
                }
            }
        }
    }
}
