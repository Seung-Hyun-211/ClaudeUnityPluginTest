using NUnit.Framework;
using UnityEngine;
using Game.Items;
using Game.Items.Equipment;

namespace Game.Tests
{
    public class ContainerContentsTests : TestBase
    {
        [Test]
        public void Capture_ThenRestoreInto_RebuildsTheSameLayout()
        {
            var source = MakeGrid(6, 4);
            var coin = MakeItem("coin", 1, 1, 50);
            var plank = MakeItem("plank", 1, 3);
            source.TryPlaceAt(coin, 12, new Vector2Int(1, 1), out _);
            source.TryPlaceAt(plank, 1, new Vector2Int(2, 2), out _, rotated: true);

            var contents = ContainerContents.Capture(source);
            var target = MakeGrid(6, 4);
            var overflow = contents.RestoreInto(target);

            Assert.IsEmpty(overflow);
            Assert.AreEqual(2, target.PlacedItems.Count);
            Assert.AreEqual(12, target.PlacedItems[0].Stack.Quantity);
            Assert.IsTrue(target.PlacedItems[1].IsRotated);
            Assert.AreEqual(new Vector2Int(2, 2), target.PlacedItems[1].Origin);
        }

        [Test]
        public void Capture_IsASnapshot_LaterChangesToTheGridDoNotAffectIt()
        {
            var grid = MakeGrid(4, 2);
            grid.TryAddItem(MakeItem("key"), 1);

            var contents = ContainerContents.Capture(grid);
            grid.Clear();

            Assert.AreEqual(1, contents.Entries.Count);
        }

        [Test]
        public void RestoreInto_CellThatNoLongerExists_ComesBackAsOverflow()
        {
            var big = MakeGrid(6, 4);
            big.TryPlaceAt(MakeItem("key"), 1, new Vector2Int(5, 3), out _);
            var contents = ContainerContents.Capture(big);

            var overflow = contents.RestoreInto(MakeGrid(2, 2));

            Assert.AreEqual(1, overflow.Count);
        }
    }

    public class ContainerEquipmentDetachTests : TestBase
    {
        [Test]
        public void Detach_NothingWorn_ReturnsNull()
        {
            Assert.IsNull(MakeEquipment().Detach(ContainerCategory.Rig));
        }

        [Test]
        public void Detach_ReturnsTheContainerWithItsContents_AndEmptiesTheSlot()
        {
            var rig = MakeContainer("rig", ContainerCategory.Rig, MakeShape(3, 3));
            var coin = MakeItem("coin", 1, 1, 50);
            var equipment = MakeEquipment();
            equipment.Equip(rig);
            equipment.GetGrid(ContainerCategory.Rig).TryPlaceAt(coin, 20, new Vector2Int(1, 0), out _);

            var detached = equipment.Detach(ContainerCategory.Rig);

            Assert.IsTrue(detached.HasValue);
            Assert.AreSame(rig, detached.Value.Item);
            Assert.AreEqual(1, detached.Value.Contents.Entries.Count);
            Assert.AreEqual(20, detached.Value.Contents.Entries[0].Quantity);
            Assert.IsNull(equipment.GetEquipped(ContainerCategory.Rig));
            Assert.IsNull(equipment.GetGrid(ContainerCategory.Rig).Shape);
            Assert.AreEqual(0, equipment.GetGrid(ContainerCategory.Rig).PlacedItems.Count);
        }

        [Test]
        public void EquipWithContents_SwapsAndReturnsTheOldContainerWithItsOwnContents()
        {
            var smallBag = MakeContainer("small", ContainerCategory.Backpack, MakeShape(3, 3));
            var bigBag = MakeContainer("big", ContainerCategory.Backpack, MakeShape(6, 4));
            var key = MakeItem("key");
            var coin = MakeItem("coin", 1, 1, 50);
            var equipment = MakeEquipment();
            equipment.Equip(smallBag);
            equipment.GetGrid(ContainerCategory.Backpack).TryPlaceAt(key, 1, Vector2Int.zero, out _);

            var contents = new ContainerContents(new[] { new ContainerContents.Entry(coin, 9, new Vector2Int(5, 3), false) });
            var result = equipment.EquipWithContents(bigBag, contents);

            Assert.IsTrue(result.Replaced.HasValue);
            Assert.AreSame(smallBag, result.Replaced.Value.Item);
            Assert.AreEqual(1, result.Replaced.Value.Contents.Entries.Count, "the key stays with the small bag");
            Assert.AreSame(bigBag, equipment.GetEquipped(ContainerCategory.Backpack));
            var grid = equipment.GetGrid(ContainerCategory.Backpack);
            Assert.AreEqual(1, grid.PlacedItems.Count, "only the new bag's own contents are in the grid");
            Assert.AreEqual(9, grid.PlacedItems[0].Stack.Quantity);
            Assert.IsEmpty(result.Overflow);
        }

        [Test]
        public void EquipWithContents_ContentsThatDoNotFit_AreReportedAsOverflow()
        {
            var tiny = MakeContainer("tiny", ContainerCategory.Rig, MakeShape(2, 2));
            var key = MakeItem("key");
            var contents = new ContainerContents(new[] { new ContainerContents.Entry(key, 1, new Vector2Int(5, 5), false) });

            var result = MakeEquipment().EquipWithContents(tiny, contents);

            Assert.AreEqual(1, result.Overflow.Count);
        }

        [Test]
        public void EquipWithContents_NothingWornBefore_ReplacedIsNull()
        {
            var rig = MakeContainer("rig", ContainerCategory.Rig, MakeShape(3, 3));

            var result = MakeEquipment().EquipWithContents(rig, ContainerContents.Empty);

            Assert.IsFalse(result.Replaced.HasValue);
        }
    }

    public class ContainerWorldSpawnerTests : TestBase
    {
        [Test]
        public void CanSpawn_OnlyContainers()
        {
            var spawner = new ContainerWorldSpawner();

            Assert.IsTrue(spawner.CanSpawn(new WorldSpawnRequest(MakeContainer("rig", ContainerCategory.Rig, MakeShape(2, 2)), 1)));
            Assert.IsFalse(spawner.CanSpawn(new WorldSpawnRequest(MakeItem("key"), 1)));
        }

        [Test]
        public void Spawn_CreatesAPickupCarryingTheContents()
        {
            var rig = MakeContainer("rig", ContainerCategory.Rig, MakeShape(3, 3));
            var contents = new ContainerContents(new[] { new ContainerContents.Entry(MakeItem("key"), 1, Vector2Int.zero, false) });

            var go = Track(new ContainerWorldSpawner().Spawn(new WorldSpawnRequest(rig, 1, contents), Vector3.zero));

            var pickup = go.GetComponent<ContainerPickup>();
            Assert.AreSame(rig, pickup.Container);
            Assert.AreSame(contents, pickup.Contents);
            Assert.IsNull(go.GetComponent<WorldItem>(), "a container is worn, not stored as a plain item");
            StringAssert.Contains("1", pickup.PromptText);
        }

        [Test]
        public void Spawn_WithoutState_CarriesEmptyContents()
        {
            var rig = MakeContainer("rig", ContainerCategory.Rig, MakeShape(3, 3));

            var go = Track(new ContainerWorldSpawner().Spawn(new WorldSpawnRequest(rig, 1), Vector3.zero));

            Assert.IsTrue(go.GetComponent<ContainerPickup>().Contents.IsEmpty);
        }

        [Test]
        public void Drop_ExtensionSpawnsTheReplacedContainerAndTheOverflow()
        {
            var factory = AddComponent<WorldItemFactory>();
            factory.Register(new ContainerWorldSpawner());
            var rig = MakeContainer("rig", ContainerCategory.Rig, MakeShape(3, 3));
            var key = MakeItem("key");
            var result = new EquipResult(new EquippedContainer(rig, ContainerContents.Empty), new[] { new ItemStack(key, 2) });

            factory.Drop(result, Vector3.zero);

            var pickups = Object.FindObjectsByType<ContainerPickup>(FindObjectsInactive.Exclude);
            var items = Object.FindObjectsByType<WorldItem>(FindObjectsInactive.Exclude);
            foreach (var p in pickups) Track(p.gameObject);
            foreach (var w in items) Track(w.gameObject);
            Assert.AreEqual(1, pickups.Length);
            Assert.AreEqual(1, items.Length);
        }
    }
}
