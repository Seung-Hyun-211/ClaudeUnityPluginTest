using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Items.Grid;
using Game.Persistence;

namespace Game.Items.Equipment
{
    /// <summary>
    /// Saves which container is worn in each slot and what sits where in each
    /// grid (item id, quantity, cell, rotation), key "player.containers".
    /// Items are stored by itemId and resolved through an ItemDatabase, so an
    /// item that no longer exists is skipped with a warning instead of
    /// breaking the whole load. Lives on the player, i.e. in a different scene
    /// than the boot-resident registry - hence SaveDataRegistry.Instance.
    /// </summary>
    public class ContainerEquipmentSaveProvider : MonoBehaviour, ISaveDataProvider
    {
        [SerializeField] private ContainerEquipmentController equipment;
        [SerializeField] private ItemDatabase database;

        public string SaveKey => "player.containers";

        private void OnEnable() => SaveDataRegistry.Instance?.Register(this);
        private void OnDisable() => SaveDataRegistry.Instance?.Unregister(this);

        public object CaptureState()
        {
            var state = new State();
            foreach (ContainerCategory category in Enum.GetValues(typeof(ContainerCategory)))
            {
                var grid = equipment.GetGrid(category);
                var gridState = new GridState
                {
                    category = (int)category,
                    containerId = equipment.GetEquipped(category)?.ItemId ?? string.Empty
                };

                foreach (var placed in grid.PlacedItems)
                {
                    gridState.items.Add(new PlacedState
                    {
                        itemId = placed.Stack.Item.ItemId,
                        quantity = placed.Stack.Quantity,
                        x = placed.Origin.x,
                        y = placed.Origin.y,
                        rotated = placed.IsRotated
                    });
                }

                state.grids.Add(gridState);
            }

            return state;
        }

        public void RestoreState(object state)
        {
            var loaded = JsonUtility.FromJson<State>((string)state);
            foreach (var gridState in loaded.grids)
            {
                var category = (ContainerCategory)gridState.category;
                var grid = equipment.GetGrid(category);

                // Empty first so equipping/unequipping evicts nothing and the
                // saved layout is the only content.
                grid.Clear();
                ApplyContainer(category, gridState.containerId);

                foreach (var saved in gridState.items)
                {
                    if (!database.TryGet(saved.itemId, out var item))
                    {
                        Debug.LogWarning($"{nameof(ContainerEquipmentSaveProvider)}: unknown item id '{saved.itemId}' - dropped from the loaded {category} grid.", this);
                        continue;
                    }

                    if (!grid.TryPlaceAt(item, saved.quantity, new Vector2Int(saved.x, saved.y), out _, saved.rotated))
                    {
                        Debug.LogWarning($"{nameof(ContainerEquipmentSaveProvider)}: {item.name} no longer fits at ({saved.x},{saved.y}) in the {category} grid - dropped.", this);
                    }
                }
            }
        }

        private void ApplyContainer(ContainerCategory category, string containerId)
        {
            if (string.IsNullOrEmpty(containerId))
            {
                // A slot with no container keeps its authored default shape
                // (the pocket) - only an equipped one is removed.
                if (equipment.GetEquipped(category) != null)
                {
                    equipment.Unequip(category);
                }
                return;
            }

            if (!database.TryGet(containerId, out var item) || item is not ContainerItemData container)
            {
                Debug.LogWarning($"{nameof(ContainerEquipmentSaveProvider)}: unknown container id '{containerId}' for {category}.", this);
                return;
            }

            if (equipment.GetEquipped(category) != container)
            {
                equipment.Equip(container);
            }
        }

        [Serializable]
        private class PlacedState
        {
            public string itemId;
            public int quantity;
            public int x;
            public int y;
            public bool rotated;
        }

        [Serializable]
        private class GridState
        {
            public int category;
            public string containerId;
            public List<PlacedState> items = new();
        }

        [Serializable]
        private class State
        {
            public List<GridState> grids = new();
        }
    }
}
