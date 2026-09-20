using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Items.Grid;

namespace Game.Items.Equipment
{
    /// <summary>
    /// Owns the player's three container grids and swaps their shape whenever
    /// a pocket/rig/backpack item is equipped or removed.
    /// </summary>
    public class ContainerEquipmentController : MonoBehaviour
    {
        [SerializeField] private GridInventory pocketGrid;
        [SerializeField] private GridInventory rigGrid;
        [SerializeField] private GridInventory backpackGrid;

        private readonly Dictionary<ContainerCategory, ContainerItemData> equipped = new();

        public event Action<ContainerCategory> EquipmentChanged;

        public ContainerItemData GetEquipped(ContainerCategory category)
        {
            return equipped.TryGetValue(category, out var item) ? item : null;
        }

        public GridInventory GetGrid(ContainerCategory category)
        {
            return category switch
            {
                ContainerCategory.Pocket => pocketGrid,
                ContainerCategory.Rig => rigGrid,
                ContainerCategory.Backpack => backpackGrid,
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };
        }

        /// <summary>Auto-places into Pocket, then Rig, then Backpack (a fixed pickup priority).</summary>
        /// <returns>The quantity that fit nowhere.</returns>
        public int AddToContainers(ItemData item, int quantity)
        {
            foreach (var category in new[] { ContainerCategory.Pocket, ContainerCategory.Rig, ContainerCategory.Backpack })
            {
                var grid = GetGrid(category);
                if (grid != null)
                {
                    quantity = grid.TryAddItem(item, quantity);
                }

                if (quantity <= 0)
                {
                    break;
                }
            }

            return quantity;
        }

        /// <summary>
        /// Takes off what is worn in a slot and hands it back WITH its
        /// contents, leaving the slot empty. This is the gameplay operation
        /// (drop it, stash it); Unequip below is the low-level shape reset.
        /// </summary>
        /// <returns>Null if nothing was worn there.</returns>
        public EquippedContainer? Detach(ContainerCategory category)
        {
            var worn = GetEquipped(category);
            if (worn == null)
            {
                return null;
            }

            var grid = GetGrid(category);
            var contents = ContainerContents.Capture(grid);
            grid.Clear();
            Unequip(category);
            return new EquippedContainer(worn, contents);
        }

        /// <summary>
        /// Puts a container on together with its contents. Whatever was worn
        /// in that slot comes off with ITS contents (returned as Replaced), so
        /// items never get split off a bag that is being swapped out.
        /// </summary>
        public EquipResult EquipWithContents(ContainerItemData containerItem, ContainerContents contents)
        {
            var replaced = Detach(containerItem.Category);
            var overflow = Equip(containerItem);
            overflow.AddRange(contents.RestoreInto(GetGrid(containerItem.Category)));
            return new EquipResult(replaced, overflow);
        }

        /// <returns>Stacks evicted from the old grid because they no longer fit.</returns>
        public List<ItemStack> Equip(ContainerItemData containerItem)
        {
            var grid = GetGrid(containerItem.Category);
            var evicted = grid.SetShape(containerItem.Shape);
            equipped[containerItem.Category] = containerItem;

            EquipmentChanged?.Invoke(containerItem.Category);
            return evicted;
        }

        public List<ItemStack> Unequip(ContainerCategory category)
        {
            var grid = GetGrid(category);
            var evicted = grid.SetShape(null);
            equipped.Remove(category);

            EquipmentChanged?.Invoke(category);
            return evicted;
        }
    }
}
