using UnityEngine;

namespace Game.Weapons.UI
{
    /// <summary>
    /// Fixed 1/2/3 weapon slot display — always visible in the same bar as
    /// the quickslot bank (4-9,0) but never affected by its `~` swap. Clicking
    /// a slot switches to it via the same WeaponLoadout the input handler uses.
    /// </summary>
    public class WeaponLoadoutBarUIView : MonoBehaviour
    {
        private static readonly (WeaponLoadoutSlot slot, string keyHint)[] Slots =
        {
            (WeaponLoadoutSlot.Primary, "1"),
            (WeaponLoadoutSlot.Secondary, "2"),
            (WeaponLoadoutSlot.Melee, "3")
        };

        [SerializeField] private WeaponLoadout loadout;
        [SerializeField] private WeaponSlotUIView[] slotViews;

        private void OnEnable()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                slotViews[i].Bind(Slots[i].slot, Slots[i].keyHint);
                slotViews[i].Clicked += loadout.SwitchTo;
            }

            loadout.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            loadout.Changed -= Refresh;

            foreach (var view in slotViews)
            {
                view.Clicked -= loadout.SwitchTo;
            }
        }

        private void Refresh()
        {
            for (int i = 0; i < slotViews.Length; i++)
            {
                var slot = Slots[i].slot;
                IWeapon weapon = slot switch
                {
                    WeaponLoadoutSlot.Primary => loadout.Primary,
                    WeaponLoadoutSlot.Secondary => loadout.Secondary,
                    WeaponLoadoutSlot.Melee => loadout.Melee,
                    _ => null
                };
                slotViews[i].Refresh(weapon, loadout.ActiveSlot == slot);
            }
        }
    }
}
