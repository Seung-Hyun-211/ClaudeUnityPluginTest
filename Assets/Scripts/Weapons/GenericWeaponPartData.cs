using UnityEngine;

namespace Game.Weapons
{
    /// <summary>Stat-only part (barrel, sight, stock, muzzle, grip) with no extra behavior beyond its modifiers.</summary>
    [CreateAssetMenu(menuName = "Weapons/Generic Part", fileName = "New Weapon Part")]
    public class GenericWeaponPartData : WeaponPartData
    {
    }
}
