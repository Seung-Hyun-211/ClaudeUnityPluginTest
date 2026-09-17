using System;

namespace Game.Weapons
{
    /// <summary>
    /// Ammo readout for HUD purposes (documents/hud-system.md's IWeaponInfo,
    /// now that a real weapon system exists to own it). Only firearms
    /// implement this — melee has no ammo, so the HUD checks for this
    /// interface rather than assuming every IWeapon has it.
    /// </summary>
    public interface IWeaponInfo : IWeaponDisplay
    {
        int CurrentAmmo { get; }
        int MagazineSize { get; }
        event Action Changed;
    }
}
