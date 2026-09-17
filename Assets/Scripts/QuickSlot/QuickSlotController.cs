using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.QuickSlot
{
    /// <summary>
    /// Owns several banks of quick slots (default 2 banks x 7 = 14 total).
    /// Only one bank is visible/usable at a time; SwapBank() (bound to `~`)
    /// cycles to the next one. Adding a third bank later is a serialized-field
    /// change, not a structural one.
    ///
    /// Keys 1/2/3 are reserved for the fixed weapon loadout (see
    /// Game.Weapons.WeaponLoadoutInputHandler) and never belong to a bank —
    /// slotsPerBank defaults to 7 (keys 4-9,0) to match.
    /// </summary>
    public class QuickSlotController : MonoBehaviour, IQuickSlotController
    {
        [SerializeField, Min(1)] private int slotsPerBank = 7;
        [SerializeField, Min(1)] private int bankCount = 2;

        private readonly List<QuickSlotBank> banks = new();

        public int SlotsPerBank => slotsPerBank;
        public int BankCount => banks.Count;
        public int ActiveBankIndex { get; private set; }
        public event Action Changed;

        private void Awake()
        {
            for (int i = 0; i < bankCount; i++)
            {
                banks.Add(new QuickSlotBank(slotsPerBank));
            }
        }

        private void Update()
        {
            foreach (var bank in banks)
            {
                for (int i = 0; i < bank.SlotCount; i++)
                {
                    if (bank.GetSlot(i) is SkillQuickSlotEntry skillEntry)
                    {
                        skillEntry.Skill.Tick(Time.deltaTime);
                    }
                }
            }
        }

        public IQuickSlottable GetSlot(int index) => banks[ActiveBankIndex].GetSlot(index);

        public void SetSlot(int index, IQuickSlottable entry)
        {
            banks[ActiveBankIndex].SetSlot(index, entry);
            Changed?.Invoke();
        }

        public void Activate(int index, QuickSlotUseContext context)
        {
            var entry = GetSlot(index);
            if (entry == null || !entry.IsUsable)
            {
                return;
            }

            entry.Use(context);
            Changed?.Invoke();
        }

        public void SwapBank()
        {
            ActiveBankIndex = (ActiveBankIndex + 1) % banks.Count;
            Changed?.Invoke();
        }
    }
}
