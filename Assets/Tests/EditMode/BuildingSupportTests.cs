using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.ActionMode;
using Game.Building;
using Game.Items;
using Game.Items.Equipment;

namespace Game.Tests
{
    /// <summary>Building's supporting pieces: save data, the item store that pays for pieces, and the action mode.</summary>
    public class BuildingSupportTests : TestBase
    {
        // ---- save data ----

        [Test]
        public void SaveData_SurvivesJson()
        {
            var data = new StructureSaveData();
            data.Set("Combat", new List<PieceRecord>
            {
                new() { pieceId = "wall", kind = (int)PieceKind.Wall, x = 2, z = -3, level = 1, axis = (int)Axis.Z, health = 42.5f, doorOpen = true, doorFlipped = true },
            });

            var loaded = JsonUtility.FromJson<StructureSaveData>(JsonUtility.ToJson(data));

            var record = loaded.Find("Combat").pieces[0];
            Assert.AreEqual("wall", record.pieceId);
            Assert.AreEqual(PieceKey.Wall(2, -3, Axis.Z, 1), record.ToKey());
            Assert.AreEqual(42.5f, record.health);
            Assert.IsTrue(record.doorOpen);
            Assert.IsTrue(record.doorFlipped);
        }

        [Test]
        public void SaveData_SetReplacesOneScene_AndKeepsTheOthers()
        {
            var data = new StructureSaveData();
            data.Set("Lobby", new List<PieceRecord> { new() { pieceId = "floor" } });
            data.Set("Combat", new List<PieceRecord> { new() { pieceId = "wall" } });

            data.Set("Combat", new List<PieceRecord> { new() { pieceId = "door" } });

            Assert.AreEqual(2, data.scenes.Count);
            Assert.AreEqual("floor", data.Find("Lobby").pieces[0].pieceId);
            Assert.AreEqual("door", data.Find("Combat").pieces[0].pieceId);
        }

        [Test]
        public void SaveData_AnEmptySceneIsDropped()
        {
            var data = new StructureSaveData();
            data.Set("Combat", new List<PieceRecord> { new() { pieceId = "wall" } });

            data.Set("Combat", new List<PieceRecord>());

            Assert.IsNull(data.Find("Combat"));
            Assert.AreEqual(0, data.scenes.Count);
        }

        [Test]
        public void PieceKey_DoorAndWallShareASlot_ButAreDifferentPieces()
        {
            var wall = PieceKey.Wall(1, 2, Axis.X, 0);
            var door = PieceKey.Door(1, 2, Axis.X, 0);

            Assert.AreEqual(wall.Slot, door.Slot);
            Assert.AreNotEqual(wall, door);
            Assert.AreEqual(wall.GetHashCode(), PieceKey.Wall(1, 2, Axis.X, 0).GetHashCode());
        }

        // ---- item store ----

        private PlayerItemStore MakeStore(out ContainerEquipmentController equipment, out Inventory flat)
        {
            equipment = MakeEquipment();
            flat = AddComponent<Inventory>();
            Invoke(flat, "Awake");

            var store = AddComponent<PlayerItemStore>();
            Set(store, "equipment", equipment);
            Set(store, "flatInventory", flat);
            return store;
        }

        [Test]
        public void ItemStore_CountsGridsAndTheFlatInventoryTogether()
        {
            var wood = MakeItem("wood", maxStack: 10);
            var store = MakeStore(out var equipment, out var flat);
            equipment.GetGrid(ContainerCategory.Pocket).TryAddItem(wood, 4);
            flat.AddItem(wood, 3);

            Assert.AreEqual(7, store.CountOf(wood));
            Assert.AreEqual(0, store.CountOf(MakeItem("stone")));
            Assert.AreEqual(0, store.CountOf(null));
        }

        [Test]
        public void ItemStore_ConsumesFromTheGridFirst_ThenTheFlatInventory()
        {
            var wood = MakeItem("wood", maxStack: 10);
            var store = MakeStore(out var equipment, out var flat);
            var pocket = equipment.GetGrid(ContainerCategory.Pocket);
            pocket.TryAddItem(wood, 4);
            flat.AddItem(wood, 3);

            Assert.IsTrue(store.TryConsume(wood, 6));

            Assert.AreEqual(0, pocket.CountOf(wood));
            Assert.AreEqual(1, flat.GetQuantity(wood));
        }

        [Test]
        public void ItemStore_NotEnough_TakesNothing()
        {
            var wood = MakeItem("wood", maxStack: 10);
            var store = MakeStore(out var equipment, out var flat);
            equipment.GetGrid(ContainerCategory.Pocket).TryAddItem(wood, 4);
            flat.AddItem(wood, 3);

            Assert.IsFalse(store.TryConsume(wood, 8));

            Assert.AreEqual(7, store.CountOf(wood));
        }

        [Test]
        public void ItemStore_SpendsTheSmallestStackFirst_SoBigStacksStayWhole()
        {
            var wood = MakeItem("wood", maxStack: 10);
            var store = MakeStore(out var equipment, out _);
            var pocket = equipment.GetGrid(ContainerCategory.Pocket);
            pocket.TryAddItem(wood, 10);
            pocket.TryAddItem(wood, 3);

            Assert.IsTrue(store.TryConsume(wood, 2));

            var quantities = new List<int>();
            foreach (var placed in pocket.PlacedItems)
            {
                quantities.Add(placed.Stack.Quantity);
            }
            CollectionAssert.AreEquivalent(new[] { 10, 1 }, quantities);
        }

        [Test]
        public void ItemStore_EmptiedStacksLeaveTheGrid_AndTheGridReportsTheChange()
        {
            var wood = MakeItem("wood", maxStack: 10);
            var store = MakeStore(out var equipment, out _);
            var pocket = equipment.GetGrid(ContainerCategory.Pocket);
            pocket.TryAddItem(wood, 3);
            int changes = 0;
            pocket.GridChanged += () => changes++;

            store.TryConsume(wood, 3);

            Assert.AreEqual(0, pocket.PlacedItems.Count);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void ItemStore_AddFillsGridsFirst_AndReturnsWhatDidNotFit()
        {
            var wood = MakeItem("wood", maxStack: 10);
            var store = MakeStore(out var equipment, out var flat);

            int left = store.Add(wood, 5);

            Assert.AreEqual(0, left);
            Assert.AreEqual(5, equipment.GetGrid(ContainerCategory.Pocket).CountOf(wood));
            Assert.AreEqual(0, flat.GetQuantity(wood));
        }

        [Test]
        public void ItemStore_ConsumingNothing_Succeeds()
        {
            var store = MakeStore(out _, out _);

            Assert.IsTrue(store.TryConsume(MakeItem("wood"), 0));
            Assert.IsFalse(store.TryConsume(null, 1));
        }

        // ---- action mode ----

        [Test]
        public void ActionMode_StartsInCombat_AndTogglesWithAnEventOnlyOnRealChanges()
        {
            var mode = AddComponent<PlayerActionModeSwitch>();
            var seen = new List<PlayerActionMode>();
            mode.Changed += seen.Add;

            Assert.IsTrue(mode.IsCombat);

            mode.Set(PlayerActionMode.Combat); // no change
            mode.Toggle();
            mode.Set(PlayerActionMode.Build);  // no change
            mode.Toggle();

            CollectionAssert.AreEqual(new[] { PlayerActionMode.Build, PlayerActionMode.Combat }, seen);
            Assert.IsTrue(mode.IsCombat);
        }

        [Test]
        public void ActionMode_CanStartInBuild()
        {
            var mode = AddComponent<PlayerActionModeSwitch>();
            Set(mode, "startMode", PlayerActionMode.Build);

            Assert.AreEqual(PlayerActionMode.Build, mode.Current);
            Assert.IsFalse(mode.IsCombat);
        }

        [Test]
        public void PieceData_RefundIsARoundedDownShareOfTheCost()
        {
            var data = NewAsset<BuildPieceData>();
            Set(data, "refundRatio", 0.5f);

            Assert.AreEqual(2, data.RefundOf(new BuildCost { count = 5 }));
            Assert.AreEqual(0, data.RefundOf(new BuildCost { count = 1 }));
        }
    }
}
