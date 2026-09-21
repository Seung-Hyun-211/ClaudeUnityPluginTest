using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.ActionMode;
using Game.Items;

namespace Game.Building.UI
{
    /// <summary>
    /// While building, a five-slot palette (keys 1-5) with each piece's cost
    /// and how many the player holds, the selected slot highlighted, and why
    /// the current spot is red. Hidden in combat. Texts are Korean literals
    /// for now (localization of UI strings is still to do).
    /// </summary>
    public class BuildPaletteUIView : MonoBehaviour
    {
        private static readonly BuildCategory[] Slots =
        {
            BuildCategory.Wall, BuildCategory.Floor, BuildCategory.Stairs, BuildCategory.Ladder, BuildCategory.Door,
        };

        [SerializeField] private PlayerActionModeSwitch modeSwitch;
        [SerializeField] private BuildModeController controller;

        [Tooltip("Must implement IItemStore (PlayerItemStore).")]
        [SerializeField] private MonoBehaviour storeSource;

        [SerializeField] private GameObject root;

        [Tooltip("One per slot, in key order 1-5.")]
        [SerializeField] private Image[] slotBackgrounds = new Image[5];

        [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[5];
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_FontAsset fontOverride;

        [SerializeField] private Color normalColor = new(0.05f, 0.05f, 0.1f, 0.8f);
        [SerializeField] private Color selectedColor = new(0.2f, 0.45f, 0.9f, 0.9f);
        [SerializeField] private Color unavailableColor = new(0.15f, 0.15f, 0.15f, 0.5f);

        private readonly StringBuilder text = new();
        private IItemStore store;

        private void Awake()
        {
            store = storeSource as IItemStore;

            if (fontOverride != null)
            {
                foreach (var label in slotLabels)
                {
                    if (label != null)
                    {
                        label.font = fontOverride;
                    }
                }

                if (statusLabel != null)
                {
                    statusLabel.font = fontOverride;
                }
            }
        }

        private void Update()
        {
            bool building = modeSwitch != null && modeSwitch.Current == PlayerActionMode.Build;
            if (root != null && root.activeSelf != building)
            {
                root.SetActive(building);
            }

            if (!building || controller == null || controller.Structures == null)
            {
                return;
            }

            var catalog = controller.Structures.Catalog;
            for (int i = 0; i < Slots.Length; i++)
            {
                var data = catalog != null ? catalog.Get(Slots[i]) : null;
                bool selected = Slots[i] == controller.Selected;

                if (slotLabels[i] != null)
                {
                    slotLabels[i].text = Describe(i + 1, Slots[i], data);
                }

                if (slotBackgrounds[i] != null)
                {
                    slotBackgrounds[i].color = data == null ? unavailableColor : (selected ? selectedColor : normalColor);
                }
            }

            if (statusLabel != null)
            {
                statusLabel.text = StatusMessage(controller.Status);
            }
        }

        private string Describe(int number, BuildCategory category, BuildPieceData data)
        {
            text.Clear();
            text.Append(number).Append(". ").Append(NameOf(category, data));

            if (data == null)
            {
                text.Append("\n(준비 중)");
            }
            else
            {
                foreach (var entry in data.Cost)
                {
                    text.Append('\n').Append(entry.item != null ? entry.item.DisplayName : "?")
                        .Append(" x").Append(entry.count)
                        .Append(" (").Append(store != null ? store.CountOf(entry.item) : 0).Append(')');
                }
            }

            return text.ToString();
        }

        private static string NameOf(BuildCategory category, BuildPieceData data)
        {
            if (data != null && !string.IsNullOrEmpty(data.DisplayName))
            {
                return data.DisplayName;
            }

            switch (category)
            {
                case BuildCategory.Wall:
                    return "벽";
                case BuildCategory.Floor:
                    return "바닥";
                case BuildCategory.Stairs:
                    return "계단";
                case BuildCategory.Ladder:
                    return "사다리";
                default:
                    return "문";
            }
        }

        private static string StatusMessage(PlacementStatus status)
        {
            switch (status)
            {
                case PlacementStatus.Ok:
                    return "좌클릭: 건설    우클릭: 철거    T: 건설 종료";
                case PlacementStatus.Occupied:
                    return "이미 있습니다";
                case PlacementStatus.Unsupported:
                    return "받쳐 주는 것이 없습니다 (바닥이 먼저 필요)";
                case PlacementStatus.OutOfZone:
                    return "건설 구역이 아닙니다";
                case PlacementStatus.Blocked:
                    return "누군가 서 있습니다";
                case PlacementStatus.NoMaterials:
                    return "재료가 부족합니다";
                case PlacementStatus.TooFar:
                    return "너무 멉니다";
                case PlacementStatus.NotAvailable:
                    return "아직 지을 수 없는 종류입니다";
                default:
                    return "T: 건설 종료";
            }
        }
    }
}
