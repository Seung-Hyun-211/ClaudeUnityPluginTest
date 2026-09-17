using System;

namespace Game.Weapons
{
    [Serializable]
    public class WeaponStatModifier
    {
        public WeaponStatType Stat;
        public float FlatBonus;
        public float PercentBonus;
    }
}
