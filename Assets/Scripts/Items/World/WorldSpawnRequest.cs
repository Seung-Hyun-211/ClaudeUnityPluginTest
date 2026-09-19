namespace Game.Items
{
    /// <summary>
    /// What to put into the world: an item, how many, and optionally the
    /// per-instance state of a kind that has some (a weapon's attached parts
    /// and loaded magazine). State is an opaque object so this module never
    /// has to know the concrete types - the spawner registered for that kind
    /// does (Weapons already depends on Items, not the other way round).
    /// </summary>
    public readonly struct WorldSpawnRequest
    {
        public ItemData Item { get; }
        public int Quantity { get; }
        public object State { get; }

        public WorldSpawnRequest(ItemData item, int quantity, object state = null)
        {
            Item = item;
            Quantity = quantity;
            State = state;
        }

        public static WorldSpawnRequest FromStack(ItemStack stack) => new(stack.Item, stack.Quantity);
    }
}
