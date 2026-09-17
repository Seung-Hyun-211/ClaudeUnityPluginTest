using UnityEngine;

namespace Game.QuickSlot
{
    /// <summary>
    /// Anything that can occupy a quick slot — an item or a skill today,
    /// possibly other things later — without the slot UI needing to know which.
    /// </summary>
    public interface IQuickSlottable
    {
        Sprite Icon { get; }
        bool IsUsable { get; }
        void Use(QuickSlotUseContext context);
    }
}
