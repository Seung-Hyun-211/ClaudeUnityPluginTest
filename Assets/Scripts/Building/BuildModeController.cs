using System;
using UnityEngine;
using Game.ActionMode;
using Game.Characters;
using Game.Items;

namespace Game.Building
{
    /// <summary>Why the piece under the crosshair can or cannot be placed (documents/building-system.md 7).</summary>
    public enum PlacementStatus
    {
        Ok,
        NoTarget,
        NotAvailable,
        OutOfZone,
        Occupied,
        Unsupported,
        Blocked,
        NoMaterials,
        TooFar,
    }

    /// <summary>
    /// The building mode's brain (scene composition root): aims, snaps the
    /// target to the grid, validates it (zone, support, characters in the way,
    /// materials, reach), drives the ghost, and places or demolishes on
    /// request. Input comes from BuildInputHandler; the structure itself is
    /// StructureManager's.
    /// </summary>
    public class BuildModeController : MonoBehaviour
    {
        [SerializeField] private PlayerActionModeSwitch modeSwitch;
        [SerializeField] private StructureManager structures;
        [SerializeField] private GhostPreview ghost;
        [SerializeField] private Transform player;

        [Tooltip("Must implement IItemStore (PlayerItemStore).")]
        [SerializeField] private MonoBehaviour storeSource;

        [Tooltip("Must implement IAimSource (CameraAimSource).")]
        [SerializeField] private MonoBehaviour aimSource;

        [SerializeField, Min(1f)] private float maxReach = 5f;
        [SerializeField, Min(1f)] private float rayLength = 40f;

        private const float FlushTolerance = 0.02f;

        private readonly RaycastHit[] rayHits = new RaycastHit[16];
        private readonly Collider[] overlaps = new Collider[16];

        private IItemStore store;
        private IAimSource aim;
        private bool hasTarget;
        private PieceKey target;

        public BuildCategory Selected { get; private set; } = BuildCategory.Wall;
        public bool DoorFlipped { get; private set; }
        public PlacementStatus Status { get; private set; } = PlacementStatus.NoTarget;
        public PieceKey? Target => hasTarget ? target : null;
        public bool IsBuilding => modeSwitch != null && modeSwitch.Current == PlayerActionMode.Build;
        public StructureManager Structures => structures;

        public event Action SelectionChanged;

        private void Awake()
        {
            store = storeSource as IItemStore;
            aim = aimSource as IAimSource;

            if (store == null)
            {
                Debug.LogError($"{nameof(storeSource)} must implement {nameof(IItemStore)}.", this);
            }

            if (aim == null)
            {
                Debug.LogError($"{nameof(aimSource)} must implement {nameof(IAimSource)}.", this);
            }
        }

        private void Update()
        {
            if (!IsBuilding)
            {
                ghost?.Hide();
                hasTarget = false;
                return;
            }

            Evaluate();
            UpdateGhost();
        }

        public void SelectCategory(BuildCategory category)
        {
            if (structures == null || structures.Catalog == null || !structures.Catalog.IsAvailable(category) || Selected == category)
            {
                return;
            }

            Selected = category;
            SelectionChanged?.Invoke();
        }

        public void ToggleDoorFlip()
        {
            if (Selected == BuildCategory.Door)
            {
                DoorFlipped = !DoorFlipped;
                SelectionChanged?.Invoke();
            }
        }

        /// <summary>Whether the player holds every material a piece costs.</summary>
        public bool CanAfford(BuildPieceData data)
        {
            foreach (var entry in data.Cost)
            {
                if (store.CountOf(entry.item) < entry.count)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Builds the piece under the crosshair when it is valid, spending its materials.</summary>
        public bool TryPlace()
        {
            if (!IsBuilding || !hasTarget || Status != PlacementStatus.Ok)
            {
                return false;
            }

            var data = structures.Catalog.Get(Selected);
            var result = structures.TryPlace(target, DoorFlipped);
            if (!result.Success)
            {
                return false;
            }

            foreach (var entry in data.Cost)
            {
                store.TryConsume(entry.item, entry.count);
            }

            // A door built over a wall (or the other way round) hands the old piece's materials back.
            foreach (var replaced in result.Replaced)
            {
                Refund(structures.Catalog.ForKind(replaced.Kind));
            }

            return true;
        }

        /// <summary>Demolishes the piece under the crosshair (within reach), refunding part of its materials.</summary>
        public bool TryDemolish()
        {
            if (!IsBuilding || !AimAtPiece(out var piece) || piece.Key.Kind == PieceKind.Pillar || !InReach(piece.transform.position))
            {
                return false;
            }

            var data = piece.Data;
            var removed = structures.Demolish(piece.Key);
            if (removed.Count == 0)
            {
                return false;
            }

            // Only the piece itself gives materials back; whatever collapsed with it does not.
            Refund(data);
            return true;
        }

        private void Refund(BuildPieceData data)
        {
            if (data == null)
            {
                return;
            }

            foreach (var entry in data.Cost)
            {
                int amount = data.RefundOf(entry);
                if (amount <= 0)
                {
                    continue;
                }

                int left = store.Add(entry.item, amount);
                if (left > 0 && WorldItemFactory.Instance != null)
                {
                    WorldItemFactory.Instance.SpawnAll(new[] { new ItemStack(entry.item, left) }, player != null ? player.position : transform.position);
                }
            }
        }

        private void Evaluate()
        {
            hasTarget = false;
            Status = PlacementStatus.NoTarget;

            if (structures == null || structures.Zone == null || !TryRaycast(out var hit))
            {
                return;
            }

            var data = structures.Catalog != null ? structures.Catalog.Get(Selected) : null;
            if (data == null)
            {
                Status = PlacementStatus.NotAvailable;
                return;
            }

            // A hair inside the surface: the top of a level-0 wall (y = 2.5) would otherwise read as the bottom of the level-1 slab.
            Vector3 inside = hit.point - hit.normal * 0.05f;
            target = BuildGrid.KeyAt(BuildCatalog.KindOf(Selected), inside, structures.Zone.Origin);
            hasTarget = true;
            Status = Validate(target, data);
        }

        private PlacementStatus Validate(PieceKey key, BuildPieceData data)
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

            return CanAfford(data) ? PlacementStatus.Ok : PlacementStatus.NoMaterials;
        }

        private bool InReach(Vector3 point) => player == null || Vector3.Distance(player.position, point) <= maxReach;

        // Pieces may overlap each other by design (pillars, wall ends); only people standing there matter.
        private bool CharacterInTheWay(PieceKey key, Vector3 center)
        {
            Vector3 half = BuildGrid.Size(key.Kind) * 0.45f;
            int count = Physics.OverlapBoxNonAlloc(center, half, overlaps, BuildGrid.Rotation(key), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (overlaps[i].GetComponentInParent<CharacterMotor>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryRaycast(out RaycastHit result)
        {
            result = default;
            if (aim == null || !aim.TryGetRay(out var ray))
            {
                return false;
            }

            int count = Physics.RaycastNonAlloc(ray, rayHits, rayLength, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                // The player and enemies are in the way of the ray, not part of the ground.
                if (rayHits[i].distance < best && rayHits[i].collider.GetComponentInParent<CharacterMotor>() == null)
                {
                    best = rayHits[i].distance;
                    result = rayHits[i];
                    found = true;
                }
            }

            // A ground-level floor is flush with the ground, so both are hit at (nearly) the same distance.
            // Take the piece then - otherwise a floor could never be aimed at to demolish it.
            for (int i = 0; found && i < count; i++)
            {
                if (rayHits[i].distance <= best + FlushTolerance && rayHits[i].collider.GetComponentInParent<BuildPiece>() != null)
                {
                    result = rayHits[i];
                    break;
                }
            }

            return found;
        }

        private bool AimAtPiece(out BuildPiece piece)
        {
            piece = null;
            if (!TryRaycast(out var hit))
            {
                return false;
            }

            piece = hit.collider.GetComponentInParent<BuildPiece>();
            return piece != null;
        }

        private void UpdateGhost()
        {
            if (ghost == null)
            {
                return;
            }

            var data = structures != null && structures.Catalog != null ? structures.Catalog.Get(Selected) : null;
            if (!hasTarget || data == null)
            {
                ghost.Hide();
                return;
            }

            ghost.Show(data, BuildGrid.Center(target, structures.Zone.Origin), BuildGrid.Rotation(target), Status == PlacementStatus.Ok);
        }
    }
}
