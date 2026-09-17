using UnityEngine;

namespace Game.Weapons
{
    /// <summary>
    /// Runtime pairing of a MagazineData asset with its currently loaded
    /// ammo/count — same ItemData+state pattern as ItemStack/SkillInstance.
    /// Two magazines sharing the same MagazineData can hold different ammo
    /// and different round counts, which is why this can't just be a field
    /// on MagazineData itself.
    /// </summary>
    public class MagazineInstance
    {
        public MagazineData Data { get; }
        public AmmoData LoadedAmmo { get; private set; }
        public int LoadedRounds { get; private set; }

        public bool IsEmpty => LoadedRounds <= 0;
        public bool IsFull => LoadedRounds >= Data.Capacity;

        public MagazineInstance(MagazineData data)
        {
            Data = data;
        }

        /// <returns>The leftover rounds that didn't fit or didn't match the ammo type.</returns>
        public int TryLoadRounds(AmmoData ammo, int count)
        {
            if (ammo.AmmoType != Data.CompatibleAmmoType)
            {
                return count;
            }

            if (LoadedRounds > 0 && LoadedAmmo != ammo)
            {
                return count;
            }

            int addable = Mathf.Min(count, Data.Capacity - LoadedRounds);
            LoadedRounds += addable;
            LoadedAmmo = ammo;
            return count - addable;
        }

        public bool TryConsumeRound()
        {
            if (IsEmpty)
            {
                return false;
            }

            LoadedRounds--;
            return true;
        }
    }
}
