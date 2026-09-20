using System.Collections.Generic;
using UnityEngine;
using Game.Items.Grid;

namespace Game.Items.Equipment
{
    /// <summary>
    /// A snapshot of what a container holds and where, so a worn rig or
    /// backpack can be taken off, dropped into the world with its contents
    /// still inside, and put on again later. The contents belong to the
    /// container, not to the slot it was worn in.
    /// </summary>
    public sealed class ContainerContents
    {
        public readonly struct Entry
        {
            public ItemData Item { get; }
            public int Quantity { get; }
            public Vector2Int Origin { get; }
            public bool Rotated { get; }

            public Entry(ItemData item, int quantity, Vector2Int origin, bool rotated)
            {
                Item = item;
                Quantity = quantity;
                Origin = origin;
                Rotated = rotated;
            }
        }

        private readonly List<Entry> entries;

        public ContainerContents(IEnumerable<Entry> entries)
        {
            this.entries = new List<Entry>(entries);
        }

        public static ContainerContents Empty => new(System.Array.Empty<Entry>());

        public IReadOnlyList<Entry> Entries => entries;
        public bool IsEmpty => entries.Count == 0;

        public static ContainerContents Capture(IGridInventory grid)
        {
            var captured = new List<Entry>();
            foreach (var placed in grid.PlacedItems)
            {
                captured.Add(new Entry(placed.Stack.Item, placed.Stack.Quantity, placed.Origin, placed.IsRotated));
            }

            return new ContainerContents(captured);
        }

        /// <returns>Stacks that could not be put back exactly where they were (the grid's shape no longer has that cell).</returns>
        public List<ItemStack> RestoreInto(IGridInventory grid)
        {
            var overflow = new List<ItemStack>();
            foreach (var entry in entries)
            {
                if (!grid.TryPlaceAt(entry.Item, entry.Quantity, entry.Origin, out int leftover, entry.Rotated))
                {
                    overflow.Add(new ItemStack(entry.Item, entry.Quantity));
                }
                else if (leftover > 0)
                {
                    overflow.Add(new ItemStack(entry.Item, leftover));
                }
            }

            return overflow;
        }
    }

    /// <summary>A container together with what was in it - the unit that is worn, dropped and picked up.</summary>
    public readonly struct EquippedContainer
    {
        public ContainerItemData Item { get; }
        public ContainerContents Contents { get; }

        public EquippedContainer(ContainerItemData item, ContainerContents contents)
        {
            Item = item;
            Contents = contents;
        }
    }

    /// <summary>What wearing a container displaced: the container that was worn before (with its contents) and any stacks that had nowhere to go.</summary>
    public readonly struct EquipResult
    {
        public EquippedContainer? Replaced { get; }
        public IReadOnlyList<ItemStack> Overflow { get; }

        public EquipResult(EquippedContainer? replaced, IReadOnlyList<ItemStack> overflow)
        {
            Replaced = replaced;
            Overflow = overflow;
        }
    }
}
