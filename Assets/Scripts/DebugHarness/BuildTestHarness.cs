using System.Linq;
using UnityEngine;
using Game.Building;
using Game.Combat;
using Game.Items;

namespace Game.DebugHarness
{
    /// <summary>
    /// OnGUI helper for the building system: shows the mode, selection and why
    /// the spot under the cursor is (in)valid, hands out materials, damages
    /// pieces to see them collapse, and round-trips the structures through the
    /// saved JSON. Test-only, like every harness here.
    /// </summary>
    public class BuildTestHarness : MonoBehaviour
    {
        [SerializeField] private BuildModeController controller;
        [SerializeField] private StructureManager structures;

        [Tooltip("Must implement IItemStore.")]
        [SerializeField] private MonoBehaviour storeSource;

        [SerializeField] private Transform player;
        [SerializeField] private ItemData wood;
        [SerializeField] private ItemData metal;

        private string savedJson = string.Empty;

        private void OnGUI()
        {
            var store = storeSource as IItemStore;

            GUILayout.BeginArea(new Rect(10, 10, 380, 420), GUI.skin.box);
            GUILayout.Label("T: build mode. Build: 1 wall, 2 floor, 5 door (3/4 = stage 2), LMB place, RMB demolish, wheel flips the door.");
            GUILayout.Label($"Mode: {(controller.IsBuilding ? "BUILD" : "combat")}    Selected: {controller.Selected}");
            GUILayout.Label($"Target: {(controller.Target.HasValue ? controller.Target.Value.ToString() : "-")}    Status: {controller.Status}");
            GUILayout.Label($"Pieces: {structures.PieceCount}    Wood: {store?.CountOf(wood)}    Metal: {store?.CountOf(metal)}");

            if (GUILayout.Button("Give 40 wood + 10 metal"))
            {
                store?.Add(wood, 40);
                store?.Add(metal, 10);
            }

            if (GUILayout.Button("Damage nearest piece (30)"))
            {
                Nearest(false)?.Health.TakeDamage(Hit(30f));
            }

            if (GUILayout.Button("Destroy nearest pillar"))
            {
                Nearest(true)?.Health.TakeDamage(Hit(9999f));
            }

            if (GUILayout.Button("Capture structures (JSON)") && StructureRepository.Instance != null)
            {
                savedJson = JsonUtility.ToJson(StructureRepository.Instance.CaptureState());
            }

            if (GUILayout.Button("Clear the scene's structures"))
            {
                structures.Rebuild(null);
            }

            if (GUILayout.Button($"Restore captured JSON ({savedJson.Length} chars)") && StructureRepository.Instance != null && savedJson.Length > 0)
            {
                StructureRepository.Instance.RestoreState(savedJson);
            }

            GUILayout.EndArea();
        }

        private DamageInfo Hit(float amount) => new(amount, DamageType.Physical, gameObject, player != null ? player.position : Vector3.zero);

        private BuildPiece Nearest(bool pillarsOnly)
        {
            return structures.Pieces
                .Where(p => !pillarsOnly || p.Key.Kind == PieceKind.Pillar)
                .OrderBy(p => Vector3.Distance(p.transform.position, player != null ? player.position : Vector3.zero))
                .FirstOrDefault();
        }
    }
}
