using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// Scene-side owner of what has been built: keeps the StructureGraph and
    /// the spawned piece objects in step, turns damage into demolition, and
    /// snapshots/rebuilds the scene structures for saving. Costs, range and
    /// aiming belong to BuildModeController - this only knows the structure.
    /// Piece-specific state (a door being open) is reached only through
    /// <see cref="IBuildPieceState"/>.
    /// </summary>
    public class StructureManager : MonoBehaviour
    {
        [SerializeField] private BuildCatalog catalog;
        [SerializeField] private BuildZone zone;

        [Tooltip("Parent for spawned pieces. Empty = this object.")]
        [SerializeField] private Transform container;

        private readonly StructureGraph graph = new();
        private readonly Dictionary<PieceKey, BuildPiece> pieces = new();

        public BuildCatalog Catalog => catalog;
        public BuildZone Zone => zone;
        public int PieceCount => pieces.Count;
        public string SceneId => gameObject.scene.name;
        public IEnumerable<BuildPiece> Pieces => pieces.Values;

        /// <summary>Raised after anything was placed, demolished, destroyed or rebuilt.</summary>
        public event Action Changed;

        private void OnEnable() => StructureRepository.Instance?.Attach(this);

        // The repository can come to life after this object OnEnable (same scene), so ask again.
        private void Start() => StructureRepository.Instance?.Attach(this);

        private void OnDisable() => StructureRepository.Instance?.Detach(this);

        public bool TryGetPiece(PieceKey key, out BuildPiece piece) => pieces.TryGetValue(key, out piece);

        /// <summary>Whether a piece could be placed here as far as the structure and the zone are concerned.</summary>
        public PlacementFailure Check(PieceKey key)
        {
            if (zone == null || catalog == null || key.Level > zone.MaxLevel)
            {
                return PlacementFailure.NotPlaceable;
            }

            var data = catalog.ForKind(key.Kind);
            if (data == null || data.Prefab == null)
            {
                return PlacementFailure.NotPlaceable;
            }

            return graph.Check(key, zone.ContainsCell);
        }

        /// <summary>Places a piece (and its corner pillars) and spawns the objects. Materials are not touched.</summary>
        /// <param name="flipped">The player variant choice (which way a door swings).</param>
        public PlaceResult TryPlace(PieceKey key, bool flipped = false)
        {
            var result = TryPlaceCore(key, flipped);
            if (result.Success)
            {
                Changed?.Invoke();
            }

            return result;
        }

        /// <summary>The player demolishes a piece. Whatever it held up falls too.</summary>
        /// <returns>Every piece removed, the demolished one first (empty for pieces that cannot be demolished by hand).</returns>
        public IReadOnlyList<PieceKey> Demolish(PieceKey key)
        {
            var removed = graph.Remove(key);
            DestroyRemoved(removed);
            return removed;
        }

        private PlaceResult TryPlaceCore(PieceKey key, bool flipped)
        {
            var failure = Check(key);
            if (failure != PlacementFailure.None)
            {
                return PlaceResult.Fail(failure);
            }

            var result = graph.TryPlace(key, zone.ContainsCell);
            if (!result.Success)
            {
                return result;
            }

            foreach (var replaced in result.Replaced)
            {
                DestroyPieceObject(replaced);
            }

            foreach (var added in result.Added)
            {
                Spawn(added, added == key && flipped);
            }

            return result;
        }

        private void HandleDied(BuildPiece piece)
        {
            if (!pieces.ContainsKey(piece.Key))
            {
                return; // already removed as part of a collapse
            }

            DestroyRemoved(graph.Destroy(piece.Key));
        }

        private void DestroyRemoved(IReadOnlyList<PieceKey> removed)
        {
            if (removed.Count == 0)
            {
                return;
            }

            foreach (var key in removed)
            {
                DestroyPieceObject(key);
            }

            Changed?.Invoke();
        }

        private void DestroyPieceObject(PieceKey key)
        {
            if (pieces.Remove(key, out var piece) && piece != null)
            {
                // Off first: Destroy only takes effect at the end of the frame.
                piece.gameObject.SetActive(false);
                Destroy(piece.gameObject);
            }
        }

        private void Spawn(PieceKey key, bool flipped)
        {
            var data = catalog.ForKind(key.Kind);
            if (data == null || data.Prefab == null)
            {
                Debug.LogWarning($"StructureManager: no prefab for {key.Kind}; {key} has no object.", this);
                return;
            }

            var instance = Instantiate(data.Prefab, BuildGrid.Center(key, zone.Origin), BuildGrid.Rotation(key), container != null ? container : transform);
            instance.name = key.ToString();

            var piece = instance.GetComponent<BuildPiece>();
            if (piece == null)
            {
                Debug.LogError($"Prefab of '{data.name}' has no {nameof(BuildPiece)}.", data);
                Destroy(instance);
                return;
            }

            piece.Initialize(key, data);
            piece.Health.SetMaxHealth(data.MaxHealth);
            piece.Health.Died += () => HandleDied(piece);

            foreach (var state in instance.GetComponents<IBuildPieceState>())
            {
                state.Initialize(flipped);
            }

            pieces[key] = piece;
        }

        /// <summary>Whether every piece still has its object. During scene teardown objects may go before this manager is disabled, and a snapshot then would lose pieces.</summary>
        public bool CanSnapshotCompletely => pieces.Values.All(piece => piece != null);

        /// <summary>Everything built in this scene, for saving.</summary>
        public SceneStructures Snapshot()
        {
            var scene = new SceneStructures { sceneId = SceneId };
            foreach (var piece in pieces.Values)
            {
                if (piece == null)
                {
                    continue; // its object is already gone (scene teardown); the snapshot is then incomplete - see Complete
                }

                var key = piece.Key;
                var record = new PieceRecord
                {
                    pieceId = piece.Data != null ? piece.Data.PieceId : string.Empty,
                    kind = (int)key.Kind,
                    x = key.X,
                    z = key.Z,
                    level = key.Level,
                    axis = (int)key.Axis,
                    health = piece.Health.Current,
                };

                foreach (var state in piece.GetComponents<IBuildPieceState>())
                {
                    state.Capture(record);
                }

                scene.pieces.Add(record);
            }

            return scene;
        }

        /// <summary>Replaces what is built here with saved structures. Pieces whose support is gone are skipped with a warning.</summary>
        public void Rebuild(SceneStructures saved)
        {
            Clear();
            if (saved == null || zone == null || catalog == null)
            {
                Changed?.Invoke();
                return;
            }

            // Floors before walls, lower levels before higher ones, so every piece finds its support.
            var ordered = saved.pieces
                .Where(r => (PieceKind)r.kind != PieceKind.Pillar)
                .OrderBy(r => r.level)
                .ThenBy(r => (PieceKind)r.kind == PieceKind.Floor ? 0 : 1)
                .ToList();

            foreach (var record in ordered)
            {
                var key = record.ToKey();
                var result = TryPlaceCore(key, record.doorFlipped);
                if (!result.Success)
                {
                    Debug.LogWarning($"StructureManager: saved {key} could not be restored ({result.Failure}) - skipped.", this);
                    continue;
                }

                if (pieces.TryGetValue(key, out var piece))
                {
                    RestoreState(piece, record);
                }
            }

            // Pillars were created by their walls; give them their saved health.
            foreach (var record in saved.pieces.Where(r => (PieceKind)r.kind == PieceKind.Pillar))
            {
                if (pieces.TryGetValue(record.ToKey(), out var pillar))
                {
                    RestoreState(pillar, record);
                }
            }

            Changed?.Invoke();
        }

        private static void RestoreState(BuildPiece piece, PieceRecord record)
        {
            if (record.health > 0f)
            {
                piece.Health.RestoreHealth(record.health);
            }

            foreach (var state in piece.GetComponents<IBuildPieceState>())
            {
                state.Restore(record);
            }
        }

        private void Clear()
        {
            foreach (var key in pieces.Keys.ToList())
            {
                DestroyPieceObject(key);
            }

            foreach (var key in graph.All().Where(k => k.Kind != PieceKind.Pillar).ToList())
            {
                graph.Remove(key);
            }
        }
    }
}
