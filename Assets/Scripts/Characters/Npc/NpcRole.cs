namespace Game.Characters.Npc
{
    /// <summary>
    /// Classification value only (spawn tables, save data, ...) — never a
    /// branch in NpcController's code. Behaviour differences come from which
    /// optional components a prefab attaches, exactly like EnemyTier (see
    /// npc-roles.md, character-system.md).
    /// </summary>
    public enum NpcRole
    {
        Village,
        CombatHelper
    }
}
