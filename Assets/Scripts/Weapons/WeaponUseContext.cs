using UnityEngine;

namespace Game.Weapons
{
    public readonly struct WeaponUseContext
    {
        public GameObject Wielder { get; }
        public Vector3 AimOrigin { get; }
        public Vector3 AimDirection { get; }

        public WeaponUseContext(GameObject wielder, Vector3 aimOrigin, Vector3 aimDirection)
        {
            Wielder = wielder;
            AimOrigin = aimOrigin;
            AimDirection = aimDirection;
        }
    }
}
