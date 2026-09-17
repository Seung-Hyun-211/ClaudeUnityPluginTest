using System;

namespace Game.QuickSlot
{
    public interface IQuickSlotController
    {
        int SlotsPerBank { get; }
        int BankCount { get; }
        int ActiveBankIndex { get; }
        event Action Changed;

        IQuickSlottable GetSlot(int index);
        void SetSlot(int index, IQuickSlottable entry);
        void Activate(int index, QuickSlotUseContext context);
        void SwapBank();
    }
}
