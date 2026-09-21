namespace Game.Items
{
    /// <summary>
    /// "Everything the player carries" as one pool: counting, spending and
    /// adding items without the caller knowing whether they sit in a pocket,
    /// rig, backpack or the flat inventory. Building materials use it.
    /// </summary>
    public interface IItemStore
    {
        int CountOf(ItemData item);

        /// <summary>Removes exactly <paramref name="quantity"/>, or nothing at all when there is not enough.</summary>
        bool TryConsume(ItemData item, int quantity);

        /// <returns>The amount that did not fit.</returns>
        int Add(ItemData item, int quantity);
    }
}
