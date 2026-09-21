using System;
using UnityEngine;
using Game.ActionMode;
using Game.Characters;
using Game.Items;

namespace Game.Building
{
    /// <summary>
    /// The building mode (scene composition root): wires the aim, validation
    /// and economy helpers together, keeps the selected category and the
    /// current target, drives the ghost, and places or demolishes on request.
    /// The work itself is done elsewhere - BuildTargetResolver (aim and snap),
    /// PlacementValidator (may it go here), BuildEconomy (materials),
    /// StructureManager (the structure); input comes from BuildInputHandler.
    /// </summary>
    public class BuildModeController : MonoBehaviour
    {
        [Tooltip("Must implement IPlayerActionMode (PlayerActionModeSwitch).")]
        [SerializeField] private MonoBehaviour modeSource;

        [SerializeField] private StructureManager structures;
        [SerializeField] private GhostPreview ghost;
        [SerializeField] private Transform player;

        [Tooltip("Must implement IItemStore (PlayerItemStore).")]
        [SerializeField] private MonoBehaviour storeSource;

        [Tooltip("Must implement IAimSource (CameraAimSource).")]
        [SerializeField] private MonoBehaviour aimSource;

        [Tooltip("Optional, must implement IItemDropper. Empty = drop through the world item factory.")]
        [SerializeField] private MonoBehaviour dropperSource;

        [SerializeField, Min(1f)] private float maxReach = 5f;
        [SerializeField, Min(1f)] private float rayLength = 40f;

        private IPlayerActionMode mode;
        private IItemStore store;
        private IAimSource aim;
        private BuildTargetResolver resolver;
        private PlacementValidator validator;
        private BuildEconomy economy;
        private bool hasTarget;
        private PieceKey target;

        public BuildCategory Selected { get; private set; } = BuildCategory.Wall;
        public bool DoorFlipped { get; private set; }
        public PlacementStatus Status { get; private set; } = PlacementStatus.NoTarget;
        public PieceKey? Target => hasTarget ? target : null;
        public bool IsBuilding => mode != null && !mode.IsCombat();
        public StructureManager Structures => structures;

        public event Action SelectionChanged;

        // The one place that knows what a character is.
        private static bool IsCharacter(Collider collider) => collider.GetComponentInParent<CharacterMotor>() != null;

        private void Awake()
        {
            mode = modeSource as IPlayerActionMode;
            store = storeSource as IItemStore;
            aim = aimSource as IAimSource;

            if (mode == null || store == null || aim == null)
            {
                Debug.LogError($"{nameof(BuildModeController)}: {nameof(modeSource)}, {nameof(storeSource)} and {nameof(aimSource)} must implement IPlayerActionMode, IItemStore and IAimSource.", this);
                return;
            }

            var dropper = dropperSource as IItemDropper ?? new WorldItemDropper();
            economy = new BuildEconomy(store, dropper, () => player != null ? player.position : transform.position);
            resolver = new BuildTargetResolver(aim, IsCharacter, rayLength);
            validator = new PlacementValidator(structures, economy, IsCharacter, player, maxReach);
        }

        private void Update()
        {
            if (!IsBuilding || validator == null)
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
        public bool CanAfford(BuildPieceData data) => economy != null && economy.CanAfford(data);

        /// <summary>Builds the piece under the crosshair when it is valid, spending its materials.</summary>
        public bool TryPlace()
        {
            if (!IsBuilding || !hasTarget || Status != PlacementStatus.Ok)
            {
                return false;
            }

            // Pay first, so a piece is never built for free; give the cost back if the placement then fails.
            var data = structures.Catalog.Get(Selected);
            if (!economy.TryPay(data))
            {
                return false;
            }

            var result = structures.TryPlace(target, DoorFlipped);
            if (!result.Success)
            {
                economy.RefundAll(data);
                return false;
            }

            // A door built over a wall (or the other way round) hands the old piece materials back.
            foreach (var replaced in result.Replaced)
            {
                economy.Refund(structures.Catalog.ForKind(replaced.Kind));
            }

            return true;
        }

        /// <summary>Demolishes the piece under the crosshair (within reach), refunding part of its materials.</summary>
        public bool TryDemolish()
        {
            if (!IsBuilding || resolver == null || !resolver.TryAimAtPiece(out var piece) || !validator.InReach(piece.transform.position))
            {
                return false;
            }

            var data = piece.Data;
            if (structures.Demolish(piece.Key).Count == 0)
            {
                return false; // pillars, or already gone
            }

            // Only the piece itself gives materials back; whatever collapsed with it does not.
            economy.Refund(data);
            return true;
        }

        private void Evaluate()
        {
            hasTarget = false;
            Status = PlacementStatus.NoTarget;

            if (structures == null || structures.Zone == null)
            {
                return;
            }

            var data = structures.Catalog != null ? structures.Catalog.Get(Selected) : null;
            if (data == null)
            {
                Status = PlacementStatus.NotAvailable;
                return;
            }

            if (!resolver.TrySnap(data, structures.Zone.Origin, out target))
            {
                return;
            }

            hasTarget = true;
            Status = validator.Validate(target, data);
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
