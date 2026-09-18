namespace Game.Characters.Enemy
{
    /// <summary>
    /// Classification value only - used for loot tables/UI (e.g. showing a
    /// boss health bar), never as a reason to subclass EnemyController (see
    /// character-system.md's Enemy 절: "EnemyTier는 ... 클래스 상속의 근거로 쓰지
    /// 않는다").
    /// </summary>
    public enum EnemyTier
    {
        Normal,
        Elite,
        Boss
    }
}
