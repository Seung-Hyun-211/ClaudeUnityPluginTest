using UnityEngine;
using Game.Items;

namespace Game.Weapons
{
    /// <summary>
    /// A thrown weapon (grenade 등). Deliberately an ordinary ItemData/
    /// IQuickSlottable item, NOT a WeaponLoadout slot — design-conflict-review.md
    /// #10 resolved the open "3 vs 4 loadout slots" question by keeping
    /// WeaponLoadout at exactly Primary/Secondary/Melee (keys 1/2/3, fixed).
    /// A throwable never claims a key of its own: it only fires from whatever
    /// quickslot key (4~9,0) the player happened to bind it to, exactly like
    /// any other item — no separate keymap entry exists or should be added.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Weapons/Throwable Item Data", fileName = "New Throwable")]
    public class ThrowableItemData : ItemData
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0f)] private float throwForce = 12f;

        public override void OnUse(GameObject user)
        {
            if (projectilePrefab == null || user == null)
            {
                return;
            }

            Vector3 spawnPoint = user.transform.position + user.transform.forward + Vector3.up;
            GameObject projectile = Object.Instantiate(projectilePrefab, spawnPoint, Quaternion.LookRotation(user.transform.forward));

            if (projectile.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForce(user.transform.forward * throwForce, ForceMode.VelocityChange);
            }
        }
    }
}
