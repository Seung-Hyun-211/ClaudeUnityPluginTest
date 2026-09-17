using UnityEngine;

namespace Game.Weapons
{
    /// <summary>A magazine is itself a WeaponPartData (occupies the Magazine slot) plus ammo compatibility/capacity.</summary>
    [CreateAssetMenu(menuName = "Weapons/Magazine", fileName = "New Magazine")]
    public class MagazineData : WeaponPartData
    {
        [SerializeField, Min(1)] private int capacity = 30;
        [SerializeField] private AmmoType compatibleAmmoType;

        public int Capacity => capacity;
        public AmmoType CompatibleAmmoType => compatibleAmmoType;
    }
}
