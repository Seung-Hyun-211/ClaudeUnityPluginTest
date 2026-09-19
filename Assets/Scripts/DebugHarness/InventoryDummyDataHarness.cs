using System.Collections.Generic;
using UnityEngine;
using Game.Items;
using Game.Items.Equipment;
using Game.Items.Grid;

namespace Game.DebugHarness
{
    /// <summary>
    /// OnGUI helper for grid inventory testing with a varied set of dummy
    /// items/containers (see Assets/Data/Tests/Inventory/Dummy): fill grids
    /// with random items, add specific ones, equip alternate containers, or
    /// clear everything. Drag the results around in the real UGUI panels.
    /// </summary>
    public class InventoryDummyDataHarness : MonoBehaviour
    {
        [SerializeField] private ContainerEquipmentController equipment;
        [SerializeField] private ItemData[] dummyItems;
        [SerializeField] private ContainerItemData[] dummyContainers;
        [SerializeField, Min(1)] private int maxStackPerFill = 5;

        private Vector2 scroll;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(480, 10, 380, 440), GUI.skin.box);
            GUILayout.Label("Dummy Data (Grid Inventory)");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill all grids randomly")) FillAllRandomly();
            if (GUILayout.Button("Clear all grids")) ClearAll();
            GUILayout.EndHorizontal();

            GUILayout.Label("Containers (equip):");
            foreach (var container in dummyContainers)
            {
                var shape = container.Shape;
                if (GUILayout.Button($"{container.DisplayName} [{container.Category} {shape.Width}x{shape.Height}]"))
                {
                    WorldItemSpawner.SpawnAll(equipment.Equip(container), transform.position);
                }
            }

            GUILayout.Label("Add item (Pocket -> Rig -> Backpack):");
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(240));
            foreach (var item in dummyItems)
            {
                var size = item.GridSize;
                if (GUILayout.Button($"{item.DisplayName} ({size.x}x{size.y}, stack {item.MaxStackSize})"))
                {
                    AddToAnyGrid(item, item.IsStackable ? Mathf.Min(3, item.MaxStackSize) : 1);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private IEnumerable<GridInventory> Grids()
        {
            yield return equipment.GetGrid(ContainerCategory.Pocket);
            yield return equipment.GetGrid(ContainerCategory.Rig);
            yield return equipment.GetGrid(ContainerCategory.Backpack);
        }

        private int AddToAnyGrid(ItemData item, int quantity)
        {
            foreach (var grid in Grids())
            {
                quantity = grid.TryAddItem(item, quantity);
                if (quantity <= 0) break;
            }
            return quantity;
        }

        private void FillAllRandomly()
        {
            foreach (var grid in Grids())
            {
                if (grid.Shape == null) continue;

                // Several consecutive misses means the grid is effectively full.
                int misses = 0;
                while (misses < 6)
                {
                    var item = dummyItems[Random.Range(0, dummyItems.Length)];
                    int quantity = item.IsStackable ? Random.Range(1, Mathf.Min(maxStackPerFill, item.MaxStackSize) + 1) : 1;
                    misses = grid.TryAddItem(item, quantity) >= quantity ? misses + 1 : 0;
                }
            }
        }

        private void ClearAll()
        {
            foreach (var grid in Grids())
            {
                foreach (var placed in new List<PlacedItem>(grid.PlacedItems))
                {
                    grid.RemoveItem(placed);
                }
            }
        }
    }
}
