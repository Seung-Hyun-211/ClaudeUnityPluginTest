using UnityEngine;
using Game.Items;
using Game.QuickSlot;
using Game.Skills;
using Game.Weapons;

namespace Game.DebugHarness
{
    /// <summary>
    /// Manual test harness for quickslot-and-skills.md/weapon-system.md.
    /// Wires a live FirearmInstance/MeleeWeaponInstance/SkillInstance at
    /// runtime (they are plain C# objects, not assets, so nothing here can be
    /// pre-wired in the inspector) and drives QuickSlotController/WeaponLoadout
    /// through their public API. OnGUI only; never shipped in a real build.
    /// </summary>
    public class QuickSlotWeaponsTestHarness : MonoBehaviour
    {
        [SerializeField] private QuickSlotController quickSlot;
        [SerializeField] private WeaponLoadout weaponLoadout;
        [SerializeField] private ItemData sampleItem;
        [SerializeField] private ScanSkillData scanSkill;
        [SerializeField] private FirearmData firearmData;
        [SerializeField] private MagazineData magazineData;
        [SerializeField] private AmmoData ammoData;
        [SerializeField] private MeleeWeaponData meleeData;

        private string log = "";

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 460, 520), GUI.skin.box);
            GUILayout.Label("QuickSlot + Weapons Test Harness");
            GUILayout.Label("Keys 1/2/3 = weapon slots (fixed). Keys 4-9,0 + ` = quickslot bank.");

            GUILayout.Space(8);
            GUILayout.Label("Quickslot:");
            if (GUILayout.Button("Put item in slot 0")) quickSlot.SetSlot(0, new ItemQuickSlotEntry(sampleItem));
            if (GUILayout.Button("Put scan skill in slot 1")) quickSlot.SetSlot(1, new SkillQuickSlotEntry(new SkillInstance(scanSkill)));
            if (GUILayout.Button("Activate slot 0")) quickSlot.Activate(0, new QuickSlotUseContext(gameObject));
            if (GUILayout.Button("Activate slot 1 (scan)")) quickSlot.Activate(1, new QuickSlotUseContext(gameObject));
            if (GUILayout.Button("Swap bank (`)")) quickSlot.SwapBank();
            GUILayout.Label($"Active bank: {quickSlot.ActiveBankIndex}/{quickSlot.BankCount}, slots/bank={quickSlot.SlotsPerBank}");
            GUILayout.Label($"Slot 0: {DescribeSlot(0)}   Slot 1: {DescribeSlot(1)}");

            GUILayout.Space(8);
            GUILayout.Label("Weapons:");
            if (GUILayout.Button("Equip loaded firearm into Primary")) EquipFirearm(WeaponLoadoutSlot.Primary);
            if (GUILayout.Button("Equip loaded firearm into Secondary")) EquipFirearm(WeaponLoadoutSlot.Secondary);
            if (GUILayout.Button("Equip melee weapon")) weaponLoadout.EquipMelee(new MeleeWeaponInstance(meleeData));
            if (GUILayout.Button("Switch to Primary (1)")) weaponLoadout.SwitchTo(WeaponLoadoutSlot.Primary);
            if (GUILayout.Button("Switch to Secondary (2)")) weaponLoadout.SwitchTo(WeaponLoadoutSlot.Secondary);
            if (GUILayout.Button("Switch to Melee (3)")) weaponLoadout.SwitchTo(WeaponLoadoutSlot.Melee);
            if (GUILayout.Button("Fire / swing active weapon")) FireActive();

            GUILayout.Label($"Active slot: {weaponLoadout.ActiveSlot}");
            if (weaponLoadout.ActiveWeapon is IWeaponInfo info)
            {
                GUILayout.Label($"{info.DisplayName}: {info.CurrentAmmo}/{info.MagazineSize}");
            }
            else if (weaponLoadout.ActiveWeapon != null)
            {
                GUILayout.Label($"{weaponLoadout.ActiveWeapon.Item.DisplayName} (melee, no ammo)");
            }

            GUILayout.Space(8);
            GUILayout.Label(log);
            GUILayout.EndArea();
        }

        private string DescribeSlot(int index)
        {
            var entry = quickSlot.GetSlot(index);
            return entry == null ? "(empty)" : entry.GetType().Name;
        }

        private void EquipFirearm(WeaponLoadoutSlot slot)
        {
            var firearm = new FirearmInstance(firearmData);
            var magazine = new MagazineInstance(magazineData);
            magazine.TryLoadRounds(ammoData, magazineData.Capacity);
            firearm.TryInsertMagazine(magazine, out _);
            weaponLoadout.EquipFirearm(firearm, slot);
            log = $"Equipped {firearmData.DisplayName} into {slot} with {magazine.LoadedRounds} rounds loaded.";
        }

        private void FireActive()
        {
            var context = new WeaponUseContext(gameObject, transform.position, transform.forward);
            bool used = weaponLoadout.TryUseActive(context);
            log = used ? "Fired/swung active weapon." : "Active weapon could not be used (empty/on cooldown/none equipped).";
        }
    }
}
