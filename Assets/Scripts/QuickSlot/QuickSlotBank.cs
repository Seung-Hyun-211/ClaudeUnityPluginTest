namespace Game.QuickSlot
{
    /// <summary>One page of quick slots (e.g. 10). A controller owns several of these.</summary>
    public class QuickSlotBank
    {
        private readonly IQuickSlottable[] slots;

        public int SlotCount => slots.Length;

        public QuickSlotBank(int slotCount)
        {
            slots = new IQuickSlottable[slotCount];
        }

        public IQuickSlottable GetSlot(int index) => slots[index];

        public void SetSlot(int index, IQuickSlottable entry)
        {
            slots[index] = entry;
        }
    }
}
