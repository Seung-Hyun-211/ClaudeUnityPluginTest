using UnityEngine;

namespace Game.Combat
{
    public readonly struct DamageInfo
    {
        public float Amount { get; }
        public DamageType Type { get; }
        public GameObject Source { get; }
        public Vector3 HitPoint { get; }

        public DamageInfo(float amount, DamageType type, GameObject source, Vector3 hitPoint)
        {
            Amount = amount;
            Type = type;
            Source = source;
            HitPoint = hitPoint;
        }
    }
}
