namespace Game.Items
{
    public class InventorySlot
    {
        public ItemStack Stack { get; internal set; }
        public bool IsEmpty => Stack == null || Stack.Quantity <= 0;
    }
}
