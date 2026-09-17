using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Base stats for one firearm model. Effective in-game stats are these
    /// base values combined with whatever parts are attached at runtime
    /// (see FirearmInstance.GetEffectiveStat) — this asset never changes,
    /// only what's clipped onto an instance of it does.
    /// </summary>
    [CreateAssetMenu(menuName = "Weapons/Firearm Data", fileName = "New Firearm")]
    public class FirearmData : WeaponItemData
    {
        [SerializeField] private AmmoType compatibleAmmoType;
        [SerializeField] private FireMode fireMode;
        [SerializeField] private float baseDamage = 20f;
        [SerializeField] private float baseFireRate = 6f;
        [SerializeField] private float baseAccuracy = 1f;
        [SerializeField] private float baseReloadSpeed = 1f;
        [SerializeField] private float baseRecoilControl = 1f;
        [SerializeField] private float range = 100f;

        public AmmoType CompatibleAmmoType => compatibleAmmoType;
        public FireMode FireMode => fireMode;
        public float Range => range;

        public float GetBaseStat(WeaponStatType stat)
        {
            return stat switch
            {
                WeaponStatType.Damage => baseDamage,
                WeaponStatType.FireRate => baseFireRate,
                WeaponStatType.Accuracy => baseAccuracy,
                WeaponStatType.ReloadSpeed => baseReloadSpeed,
                WeaponStatType.RecoilControl => baseRecoilControl,
                _ => 0f
            };
        }
    }
}
