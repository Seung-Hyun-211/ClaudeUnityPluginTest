using UnityEngine;

namespace Game.Weapons
{
    [CreateAssetMenu(menuName = "Weapons/Melee Weapon Data", fileName = "New Melee Weapon")]
    public class MeleeWeaponData : WeaponItemData
    {
        [SerializeField] private float damage = 25f;
        [SerializeField] private float attackRate = 1.5f;
        [SerializeField] private float range = 2f;

        public float Damage => damage;
        public float AttackRate => attackRate;
        public float Range => range;
    }
}
