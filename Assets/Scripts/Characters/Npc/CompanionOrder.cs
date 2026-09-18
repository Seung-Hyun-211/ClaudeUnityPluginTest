namespace Game.Characters.Npc
{
    /// <summary>
    /// Orders the player can give a Companion NPC. Input method (hotkey,
    /// radial menu, ...) is out of scope here (see npc-roles.md) —
    /// CompanionOrderReceiver only stores the result.
    /// </summary>
    public enum CompanionOrder
    {
        Follow,
        Hold,
        AttackTarget
    }
}
