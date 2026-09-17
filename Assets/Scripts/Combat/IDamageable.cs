using System;

namespace Game.Combat
{
    /// <summary>
    /// Anything that can be attacked — a character or, later, a destructible
    /// object. Kept free of any character/AI dependency so it can be attached
    /// to non-character objects too.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damageInfo);
        event Action<DamageInfo> Damaged;
        event Action Died;
    }
}
