using System;
using System.Collections.Generic;
using System.Linq;

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
    /// The topology of what has been built: which pieces exist, whether a new
    /// one would be held up, and what falls down when something goes
    /// (documents/building-system.md ch. 4). Pure logic - no physics,
    /// materials or GameObjects - so every rule is covered by EditMode tests.
    ///
    /// The graph itself is kind-agnostic: it stores pieces per slot and asks
    /// the <see cref="IPieceRule"/> of each kind what to check, create and
    /// re-check (see <see cref="PieceRules"/> for the stage-1 rules). A wall
    /// and a door on one edge share a slot, so a slot holds one kind at a time.
    /// </summary>
    public sealed class StructureGraph
    {
        private readonly Dictionary<PieceKey, PieceKind> pieces = new(); // slot -> the kind standing there
        private readonly IReadOnlyDictionary<PieceKind, IPieceRule> rules;

        public StructureGraph() : this(PieceRules.CreateDefault())
        {
        }

        public StructureGraph(IReadOnlyDictionary<PieceKind, IPieceRule> rules)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public int Count => pieces.Count;

        public bool Contains(PieceKey key) => pieces.TryGetValue(key.Slot, out var kind) && kind == key.Kind;

        public bool HasFloor(int x, int z, int level) => Contains(PieceKey.Floor(x, z, level));

        public bool HasPillar(int x, int z, int level) => Contains(PieceKey.Pillar(x, z, level));

        public bool TryGetEdgePiece(int x, int z, Axis axis, int level, out PieceKind kind) =>
            pieces.TryGetValue(PieceKey.Wall(x, z, axis, level), out kind);

        public IEnumerable<PieceKey> All() => pieces.Select(pair => pair.Key.WithKind(pair.Value));

        /// <param name="groundSupport">Whether a level-0 floor may go on this cell (inside a build zone); null = anywhere.</param>
        public PlacementFailure Check(PieceKey key, Func<int, int, bool> groundSupport = null)
        {
            if (key.Level < 0 || !rules.TryGetValue(key.Kind, out var rule))
            {
                return PlacementFailure.NotPlaceable;
            }

            return rule.Check(this, key, groundSupport);
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

            var slot = key.Slot;
            if (pieces.TryGetValue(slot, out var old) && old != key.Kind)
            {
                replaced.Add(slot.WithKind(old));
            }

            pieces[slot] = key.Kind;
            added.Add(key);

            foreach (var companion in rules[key.Kind].Companions(key))
            {
                if (!pieces.ContainsKey(companion.Slot))
                {
                    pieces[companion.Slot] = companion.Kind;
                    added.Add(companion);
                }
            }

            return PlaceResult.Ok(added, replaced);
        }

        /// <summary>
        /// The player demolishes a piece; everything that then loses its support
        /// falls too. Kinds whose rule forbids it (corner pillars) are left alone.
        /// </summary>
        /// <returns>Every piece removed, the demolished one first; empty when it was not there.</returns>
        public IReadOnlyList<PieceKey> Remove(PieceKey key)
        {
            var removed = new List<PieceKey>();
            if (!Contains(key) || !rules[key.Kind].CanDemolish)
            {
                return removed;
            }

            var pending = new Queue<PieceKey>();
            RemoveOne(key, removed, pending);
            Settle(removed, pending);
            return removed;
        }

        /// <summary>
        /// A piece destroyed by damage: it goes, so does whatever its rule says
        /// goes with it (a pillar takes the walls at its vertex), and whatever
        /// that held up falls.
        /// </summary>
        public IReadOnlyList<PieceKey> Destroy(PieceKey key)
        {
            var removed = new List<PieceKey>();
            if (!Contains(key))
            {
                return removed;
            }

            var collateral = rules[key.Kind].Collateral(this, key).ToList();
            var pending = new Queue<PieceKey>();
            RemoveOne(key, removed, pending);
            foreach (var other in collateral)
            {
                RemoveOne(other, removed, pending);
            }

            Settle(removed, pending);
            return removed;
        }

        // Removes one piece and queues what may have depended on it.
        private void RemoveOne(PieceKey key, List<PieceKey> removed, Queue<PieceKey> pending)
        {
            if (!Contains(key))
            {
                return;
            }

            pieces.Remove(key.Slot);
            removed.Add(key);

            foreach (var dependent in rules[key.Kind].Dependents(key))
            {
                pending.Enqueue(dependent);
            }
        }

        // Re-checks queued pieces until nothing more falls.
        private void Settle(List<PieceKey> removed, Queue<PieceKey> pending)
        {
            while (pending.Count > 0)
            {
                var queued = pending.Dequeue();
                if (!pieces.TryGetValue(queued.Slot, out var kind))
                {
                    continue;
                }

                var actual = queued.Slot.WithKind(kind);
                if (!rules[kind].IsSupported(this, actual))
                {
                    RemoveOne(actual, removed, pending);
                }
            }
        }
    }
}
