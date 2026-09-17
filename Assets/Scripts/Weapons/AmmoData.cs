using UnityEngine;
using Game.Items;

namespace Game.Weapons
{
    [CreateAssetMenu(menuName = "Weapons/Ammo Data", fileName = "New Ammo")]
    public class AmmoData : ItemData
    {
        [SerializeField] private AmmoType ammoType;
        public AmmoType AmmoType => ammoType;
    }
}
