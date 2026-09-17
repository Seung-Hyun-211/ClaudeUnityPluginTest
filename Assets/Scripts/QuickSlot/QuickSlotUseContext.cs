using UnityEngine;

namespace Game.QuickSlot
{
    public readonly struct QuickSlotUseContext
    {
        public GameObject User { get; }

        public QuickSlotUseContext(GameObject user)
        {
            User = user;
        }
    }
}
