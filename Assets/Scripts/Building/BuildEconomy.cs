using System;
using UnityEngine;
using Game.Items;

namespace Game.Building
{
    /// <summary>
    /// What building costs and gives back: checks and spends a piece cost from
    /// the player items, and refunds part of it on demolition (what does not
    /// fit in the bags is dropped in the world). Knows nothing about grids,
    /// aiming or scene objects.
    /// </summary>
    public sealed class BuildEconomy
    {
        private readonly IItemStore store;
        private readonly IItemDropper dropper;
        private readonly Func<Vector3> dropPosition;

        /// <param name="dropPosition">Where refunded items that do not fit are dropped.</param>
        public BuildEconomy(IItemStore store, IItemDropper dropper, Func<Vector3> dropPosition)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.dropper = dropper ?? throw new ArgumentNullException(nameof(dropper));
            this.dropPosition = dropPosition ?? throw new ArgumentNullException(nameof(dropPosition));
        }

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

        /// <summary>Spends the whole cost, or nothing when any material is short.</summary>
        public bool TryPay(BuildPieceData data)
        {
            if (!CanAfford(data))
            {
                return false;
            }

            foreach (var entry in data.Cost)
            {
                store.TryConsume(entry.item, entry.count);
            }

            return true;
        }

        /// <summary>Gives the whole cost back (a placement that was paid for did not happen).</summary>
        public void RefundAll(BuildPieceData data)
        {
            foreach (var entry in data.Cost)
            {
                int left = store.Add(entry.item, entry.count);
                if (left > 0)
                {
                    dropper.Drop(entry.item, left, dropPosition());
                }
            }
        }

        public void Refund(BuildPieceData data)
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
                if (left > 0)
                {
                    dropper.Drop(entry.item, left, dropPosition());
                }
            }
        }
    }
}
