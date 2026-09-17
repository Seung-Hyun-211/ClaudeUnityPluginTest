using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Items;

namespace Game.Weapons
{
    /// <summary>
    /// A detachable weapon part (barrel, sight, stock, ...). It's an ItemData
    /// like any other, so it can be picked up and stored in the grid
    /// inventory (see inventory-system.md) — modding a firearm is just
    /// swapping which part occupies a slot on a FirearmInstance.
    /// </summary>
    public abstract class WeaponPartData : ItemData
    {
        [SerializeField] private WeaponPartSlot slot;
        [SerializeField] private WeaponStatModifier[] modifiers = Array.Empty<WeaponStatModifier>();

        public WeaponPartSlot Slot => slot;
        public IReadOnlyList<WeaponStatModifier> Modifiers => modifiers;
    }
}
