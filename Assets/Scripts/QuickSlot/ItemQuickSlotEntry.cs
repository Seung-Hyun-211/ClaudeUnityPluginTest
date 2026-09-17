using UnityEngine;
using Game.Items;

namespace Game.QuickSlot
{
    /// <summary>
    /// Wraps an item so it can occupy a quick slot. Per-item use effects
    /// (consume, quick-equip, ...) are a follow-up — this only satisfies the
    /// slot contract so items and skills can share the same hotbar.
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
            // TODO: 아이템 종류별 사용 효과(소비, 장비 교체 등) 연결.
        }
    }
}
