namespace Game.Weapons
{
    /// <summary>
    /// Anything a WeaponLoadout can hold and use — a firearm or a melee
    /// weapon today, extensible to more later (thrown weapons, etc.) without
    /// touching WeaponLoadout or the input code that calls Use.
    /// </summary>
    public interface IWeapon
    {
        WeaponItemData Item { get; }
        bool CanUse(WeaponUseContext context);
        void Use(WeaponUseContext context);
        void Tick(float deltaTime);
    }
}
