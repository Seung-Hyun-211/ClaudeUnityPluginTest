namespace Game.AI
{
    /// <summary>
    /// Factory for an actor's initial state graph. Framework-level (not
    /// Enemy-specific) so Companion NPCs build their AI the same way Enemies
    /// do — see npc-roles.md's "IAiBrain을 Enemy 전용에서 프레임워크로 승격".
    /// </summary>
    public interface IAiBrain
    {
        IAiState BuildInitialState(AiContext context);
    }
}
