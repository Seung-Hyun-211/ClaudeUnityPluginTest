using UnityEngine;
using Game.Items;
using Game.Items.Equipment;

namespace Game.DebugHarness
{
    /// <summary>
    /// Manual test harness for item-system.md/inventory-system.md — exercises
    /// the flat Inventory (used by WorldItem/CraftingSystem) and the grid
    /// ContainerEquipmentController (Pocket/Rig/Backpack) side by side.
    /// OnGUI only; never shipped in a real build.
    /// </summary>
    public class InventoryTestHarness : MonoBehaviour
    {
        [SerializeField] private Inventory flatInventory;
        [SerializeField] private ContainerEquipmentController equipment;
        [SerializeField] private ItemData sampleItemSmall;
        [SerializeField] private ItemData sampleItemBig;
        [SerializeField] private ContainerItemData rigContainer;
        [SerializeField] private ContainerItemData backpackContainer;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 460, 420), GUI.skin.box);
            GUILayout.Label("Inventory Test Harness");
            GUILayout.Label("F on the world item picks it up into the flat Inventory (WorldItem -> IInventory).");

            GUILayout.Space(8);
            GUILayout.Label($"Flat Inventory: {sampleItemSmall.DisplayName} x{flatInventory.GetQuantity(sampleItemSmall)}");
            if (GUILayout.Button("Add small item directly to flat Inventory"))
            {
                flatInventory.AddItem(sampleItemSmall, 1);
            }

            GUILayout.Space(8);
            GUILayout.Label("Grid inventory (Pocket/Rig/Backpack):");
            if (GUILayout.Button("Add small item to Pocket grid"))
            {
                equipment.GetGrid(ContainerCategory.Pocket).TryAddItem(sampleItemSmall, 1);
            }
            if (GUILayout.Button("Add big item to Pocket grid"))
            {
                equipment.GetGrid(ContainerCategory.Pocket).TryAddItem(sampleItemBig, 1);
            }
            if (GUILayout.Button("Equip Rig"))
            {
                equipment.Equip(rigContainer);
            }
            if (GUILayout.Button("Unequip Rig"))
            {
                equipment.Unequip(ContainerCategory.Rig);
            }
            if (GUILayout.Button("Equip Backpack"))
            {
                equipment.Equip(backpackContainer);
            }
            if (GUILayout.Button("Unequip Backpack"))
            {
                equipment.Unequip(ContainerCategory.Backpack);
            }

            GUILayout.Space(8);
            DrawGrid("Pocket", ContainerCategory.Pocket);
            DrawGrid("Rig", ContainerCategory.Rig);
            DrawGrid("Backpack", ContainerCategory.Backpack);
            GUILayout.EndArea();
        }

        private void DrawGrid(string label, ContainerCategory category)
        {
            var grid = equipment.GetGrid(category);
            string shapeInfo = grid.Shape != null ? $"{grid.Shape.Width}x{grid.Shape.Height} ({grid.Shape.CellCount} cells)" : "(unequipped)";
            GUILayout.Label($"{label}: {shapeInfo}, placed stacks={grid.PlacedItems.Count}");
        }
    }
}
