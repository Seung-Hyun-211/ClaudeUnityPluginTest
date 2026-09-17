using UnityEngine;

namespace Game.Weapons
{
    public interface IWeaponDisplay
    {
        Sprite Icon { get; }
        string DisplayName { get; }
    }
}
