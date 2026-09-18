using UnityEngine;
using Game.Items;

namespace Game.QuickSlot
{
    /// <summary>
    /// Wraps an item so it can occupy a quick slot. The actual use effect is
    /// the item's own responsibility (ItemData.OnUse) — this entry only
    /// satisfies the slot contract so items and skills can share the same
    /// hotbar, and never needs to change when a new item behaviour is added.
    /// </summary>
    public class ItemQuickSlotEntry : IQuickSlottable
    {
        public ItemData Item { get; }

        public ItemQuickSlotEntry(ItemData item)
        {
            Item = item;
        }

        public Sprite Icon => Item.Icon;
        public bool IsUsable => true;

        public void Use(QuickSlotUseContext context)
        {
            Item.OnUse(context.User);
        }
    }
}
