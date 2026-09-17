using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Weapons
{
    /// <summary>
    /// Keys 1/2/3 always mean Primary/Secondary/Melee — fixed, and never
    /// affected by the quickslot bank swap (`~`). This is the resolution to
    /// the digit-key conflict between weapon selection and the quickslot bar
    /// (see documents/design-conflict-review.md #1): weapons own 1-3 outright,
    /// the quickslot bank only ever sees 4-9,0.
    /// </summary>
    public class WeaponLoadoutInputHandler : MonoBehaviour
    {
        private static readonly (Key key, WeaponLoadoutSlot slot)[] SlotKeys =
        {
            (Key.Digit1, WeaponLoadoutSlot.Primary),
            (Key.Digit2, WeaponLoadoutSlot.Secondary),
            (Key.Digit3, WeaponLoadoutSlot.Melee)
        };

        [SerializeField] private WeaponLoadout loadout;

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            foreach (var (key, slot) in SlotKeys)
            {
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    loadout.SwitchTo(slot);
                }
            }
        }
    }
}
