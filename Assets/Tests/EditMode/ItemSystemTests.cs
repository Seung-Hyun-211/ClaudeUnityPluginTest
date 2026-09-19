using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Game.Items;
using Game.Items.Equipment;
using Game.Items.Grid;

namespace Game.Tests
{
    public class ItemDatabaseTests : TestBase
    {
        [Test]
        public void TryGet_FindsByIdAndRejectsUnknownOrEmpty()
        {
            var coin = MakeItem("coin");
            var database = MakeDatabase(coin, MakeItem("key"));

            Assert.IsTrue(database.TryGet("coin", out var found));
            Assert.AreSame(coin, found);
            Assert.IsFalse(database.TryGet("nope", out _));
            Assert.IsFalse(database.TryGet("", out _));
            Assert.IsFalse(database.TryGet(null, out _));
        }

        [Test]
        public void FindProblems_ReportsEmptyIdsDuplicatesAndNullEntries()
        {
            var database = MakeDatabase(MakeItem("coin"), MakeItem("coin"), MakeItem(""), null);

            var problems = database.FindProblems();

            Assert.AreEqual(3, problems.Count);
        }

        [Test]
        public void FindProblems_CleanDatabase_ReportsNothing()
        {
            Assert.IsEmpty(MakeDatabase(MakeItem("a"), MakeItem("b")).FindProblems());
        }
    }

    public class DropPlacementTests
    {
        [Test]
        public void ScatterOffset_SingleItem_StaysOnTheCentre()
        {
            Assert.AreEqual(Vector2.zero, DropPlacement.ScatterOffset(0, 1, 0.5f));
        }

        [Test]
        public void ScatterOffset_ManyItems_KeepsNeighboursApart()
        {
            const int count = 12;
            const float spacing = 0.5f;
            var offsets = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                offsets[i] = DropPlacement.ScatterOffset(i, count, spacing);
            }

            float minDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    minDistance = Mathf.Min(minDistance, Vector2.Distance(offsets[i], offsets[j]));
                }
            }

            Assert.Greater(minDistance, spacing * 0.7f, "no two drops should overlap");
        }

        [Test]
        public void ScatterOffset_IsDeterministic()
        {
            Assert.AreEqual(DropPlacement.ScatterOffset(3, 8, 0.4f), DropPlacement.ScatterOffset(3, 8, 0.4f));
        }
    }

    public class WorldItemFactoryTests : TestBase
    {
        private class FakeSpawner : IWorldItemSpawner
        {
            public readonly string Name;
            public bool Accepts = true;
            public int Spawned;

            public FakeSpawner(string name) => Name = name;

            public bool CanSpawn(in WorldSpawnRequest request) => Accepts;

            public GameObject Spawn(in WorldSpawnRequest request, Vector3 position)
            {
                Spawned++;
                var go = new GameObject(Name);
                go.transform.position = position;
                return go;
            }
        }

        private class OtherFakeSpawner : FakeSpawner
        {
            public OtherFakeSpawner(string name) : base(name) { }
        }

        private WorldItemFactory MakeFactory() => AddComponent<WorldItemFactory>();

        [Test]
        public void Spawn_AsksTheLatestRegisteredSpawnerFirst()
        {
            var factory = MakeFactory();
            var first = new FakeSpawner("first");
            var second = new OtherFakeSpawner("second");
            factory.Register(first);
            factory.Register(second);

            var go = Track(factory.Spawn(new WorldSpawnRequest(MakeItem("k"), 1), Vector3.zero));

            Assert.AreEqual("second", go.name);
        }

        [Test]
        public void Spawn_SkipsSpawnersThatDeclineTheRequest()
        {
            var factory = MakeFactory();
            var declining = new OtherFakeSpawner("declining") { Accepts = false };
            var accepting = new FakeSpawner("accepting");
            factory.Register(accepting);
            factory.Register(declining);

            var go = Track(factory.Spawn(new WorldSpawnRequest(MakeItem("k"), 1), Vector3.zero));

            Assert.AreEqual("accepting", go.name);
            Assert.AreEqual(0, declining.Spawned);
        }

        [Test]
        public void Register_SameSpawnerTypeAgain_ReplacesTheOldOne()
        {
            var factory = MakeFactory();
            var old = new FakeSpawner("old");
            var replacement = new FakeSpawner("replacement");
            factory.Register(old);
            factory.Register(replacement);

            var go = Track(factory.Spawn(new WorldSpawnRequest(MakeItem("k"), 1), Vector3.zero));

            Assert.AreEqual("replacement", go.name);
            Assert.AreEqual(0, old.Spawned);
        }

        [Test]
        public void Spawn_NothingClaimsIt_FallsBackToTheItemSpawner()
        {
            var factory = MakeFactory();
            factory.Register(new FakeSpawner("no") { Accepts = false });

            var go = Track(factory.Spawn(new WorldSpawnRequest(MakeItem("key"), 4), Vector3.zero));

            Assert.IsNotNull(go.GetComponent<WorldItem>());
            Assert.AreEqual(4, go.GetComponent<WorldItem>().Quantity);
        }

        [Test]
        public void Spawn_MissingItemOrQuantity_ReturnsNullWithAWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("nothing can spawn"));
            LogAssert.Expect(LogType.Warning, new Regex("nothing can spawn"));
            var factory = MakeFactory();

            Assert.IsNull(factory.Spawn(new WorldSpawnRequest(null, 1), Vector3.zero));
            Assert.IsNull(factory.Spawn(new WorldSpawnRequest(MakeItem("k"), 0), Vector3.zero));
        }

        [Test]
        public void SpawnAll_SpawnsEveryValidStackAtDistinctPositions()
        {
            var factory = MakeFactory();
            var spawner = new FakeSpawner("stack");
            factory.Register(spawner);
            var key = MakeItem("key");
            var stacks = new[] { new ItemStack(key, 1), null, new ItemStack(key, 2), new ItemStack(key, 0), new ItemStack(key, 3) };

            factory.SpawnAll(stacks, new Vector3(5f, 0f, 5f));

            Assert.AreEqual(3, spawner.Spawned, "null and empty stacks are skipped");
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude))
            {
                if (go.name == "stack")
                {
                    Track(go);
                }
            }
        }
    }

    public class ItemWorldSpawnerTests : TestBase
    {
        [Test]
        public void ItemWithoutAWorldPrefab_GetsTheDefaultVisualAndAWorldItem()
        {
            var item = MakeItem("key");

            var go = Track(new ItemWorldSpawner().Spawn(new WorldSpawnRequest(item, 3), new Vector3(1f, 2f, 3f)));

            var worldItem = go.GetComponent<WorldItem>();
            Assert.AreSame(item, worldItem.Item);
            Assert.AreEqual(3, worldItem.Quantity);
            Assert.IsTrue(go.GetComponent<Collider>().isTrigger, "a pickup must not block the player");
            Assert.AreEqual(new Vector3(1f, 2f, 3f), go.transform.position);
        }

        [Test]
        public void PrefabWithoutAWorldItem_GetsOneAndACollider()
        {
            var prefab = NewGameObject("bare prefab");
            var item = MakeItem("key");
            Set(item, "worldPrefab", prefab);

            var go = Track(new ItemWorldSpawner().Spawn(new WorldSpawnRequest(item, 1), Vector3.zero));

            Assert.AreNotSame(prefab, go, "the prefab is instantiated, not reused");
            Assert.IsNotNull(go.GetComponent<WorldItem>());
            Assert.IsTrue(go.GetComponent<Collider>().isTrigger);
        }

        [Test]
        public void CanSpawn_RequiresAnItemAndAPositiveQuantity()
        {
            var spawner = new ItemWorldSpawner();

            Assert.IsTrue(spawner.CanSpawn(new WorldSpawnRequest(MakeItem("key"), 1)));
            Assert.IsFalse(spawner.CanSpawn(new WorldSpawnRequest(null, 1)));
            Assert.IsFalse(spawner.CanSpawn(new WorldSpawnRequest(MakeItem("key"), 0)));
        }
    }

    public class SaveProviderTests : TestBase
    {
        private ContainerEquipmentController MakeEquipment()
        {
            var equipment = AddComponent<ContainerEquipmentController>();
            Set(equipment, "pocketGrid", MakeGrid(4, 2));
            Set(equipment, "rigGrid", AddComponent<GridInventory>());
            Set(equipment, "backpackGrid", AddComponent<GridInventory>());
            return equipment;
        }

        [Test]
        public void ContainerSave_RoundTripsContainersStacksCellsAndRotation()
        {
            var rig = MakeContainer("rig", ContainerCategory.Rig, MakeShape(3, 3));
            var backpack = MakeContainer("bag", ContainerCategory.Backpack, MakeShape(6, 4));
            var coin = MakeItem("coin", 1, 1, 50);
            var plank = MakeItem("plank", 1, 3);
            var rifle = MakeItem("rifle", 4, 1);
            var database = MakeDatabase(rig, backpack, coin, plank, rifle);

            var equipment = MakeEquipment();
            var provider = AddComponent<ContainerEquipmentSaveProvider>();
            Set(provider, "equipment", equipment);
            Set(provider, "database", database);

            equipment.Equip(rig);
            equipment.Equip(backpack);
            equipment.GetGrid(ContainerCategory.Pocket).TryPlaceAt(coin, 37, new Vector2Int(1, 1), out _);
            equipment.GetGrid(ContainerCategory.Backpack).TryPlaceAt(rifle, 1, new Vector2Int(2, 1), out _);
            equipment.GetGrid(ContainerCategory.Backpack).TryPlaceAt(plank, 1, new Vector2Int(0, 2), out _, rotated: true);
            string json = JsonUtility.ToJson(provider.CaptureState());

            // wipe everything, then restore
            equipment.GetGrid(ContainerCategory.Pocket).Clear();
            equipment.Unequip(ContainerCategory.Rig);
            equipment.Unequip(ContainerCategory.Backpack);
            provider.RestoreState(json);

            Assert.AreSame(rig, equipment.GetEquipped(ContainerCategory.Rig));
            Assert.AreSame(backpack, equipment.GetEquipped(ContainerCategory.Backpack));
            var pocketItem = equipment.GetGrid(ContainerCategory.Pocket).PlacedItems[0];
            Assert.AreEqual(37, pocketItem.Stack.Quantity);
            Assert.AreEqual(new Vector2Int(1, 1), pocketItem.Origin);
            var bagItems = equipment.GetGrid(ContainerCategory.Backpack).PlacedItems;
            Assert.AreEqual(2, bagItems.Count);
            Assert.IsTrue(bagItems[1].IsRotated);
            Assert.AreEqual(new Vector2Int(0, 2), bagItems[1].Origin);
        }

        [Test]
        public void ContainerSave_Restore_SkipsUnknownItemsAndCellsThatNoLongerFit()
        {
            var coin = MakeItem("coin", 1, 1, 50);
            var database = MakeDatabase(coin);
            var equipment = MakeEquipment();
            var provider = AddComponent<ContainerEquipmentSaveProvider>();
            Set(provider, "equipment", equipment);
            Set(provider, "database", database);

            LogAssert.Expect(LogType.Warning, new Regex("unknown item id 'deleted'"));
            LogAssert.Expect(LogType.Warning, new Regex("no longer fits"));
            LogAssert.Expect(LogType.Warning, new Regex("unknown container id 'gone'"));
            string json = "{\"grids\":[{\"category\":0,\"containerId\":\"\",\"items\":["
                + "{\"itemId\":\"deleted\",\"quantity\":2,\"x\":0,\"y\":0,\"rotated\":false},"
                + "{\"itemId\":\"coin\",\"quantity\":5,\"x\":9,\"y\":9,\"rotated\":false},"
                + "{\"itemId\":\"coin\",\"quantity\":5,\"x\":1,\"y\":1,\"rotated\":false}]},"
                + "{\"category\":1,\"containerId\":\"gone\",\"items\":[]}]}";

            provider.RestoreState(json);

            var pocket = equipment.GetGrid(ContainerCategory.Pocket);
            Assert.AreEqual(1, pocket.PlacedItems.Count);
            Assert.AreEqual(new Vector2Int(1, 1), pocket.PlacedItems[0].Origin);
        }

        [Test]
        public void ContainerSave_Restore_KeepsThePocketShapeWhenNoContainerWasSaved()
        {
            var equipment = MakeEquipment();
            var provider = AddComponent<ContainerEquipmentSaveProvider>();
            Set(provider, "equipment", equipment);
            Set(provider, "database", MakeDatabase());

            provider.RestoreState("{\"grids\":[{\"category\":0,\"containerId\":\"\",\"items\":[]}]}");

            Assert.IsNotNull(equipment.GetGrid(ContainerCategory.Pocket).Shape, "the pocket has a default shape and no equipped container to remove");
        }

        [Test]
        public void InventorySave_RoundTripsSlots()
        {
            var coin = MakeItem("coin", 1, 1, 50);
            var key = MakeItem("key");
            var database = MakeDatabase(coin, key);
            var inventory = AddComponent<Inventory>();
            Invoke(inventory, "Awake");
            var provider = AddComponent<InventorySaveProvider>();
            Set(provider, "inventory", inventory);
            Set(provider, "database", database);

            inventory.SetSlotStack(2, new ItemStack(coin, 12));
            inventory.SetSlotStack(7, new ItemStack(key, 1));
            string json = JsonUtility.ToJson(provider.CaptureState());

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                inventory.SetSlotStack(i, null);
            }
            provider.RestoreState(json);

            Assert.AreEqual(12, inventory.GetSlot(2).Stack.Quantity);
            Assert.AreSame(key, inventory.GetSlot(7).Stack.Item);
            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
        }

        [Test]
        public void InventorySave_Restore_DropsUnknownItemsAndOutOfRangeSlots()
        {
            var database = MakeDatabase(MakeItem("coin", 1, 1, 50));
            var inventory = AddComponent<Inventory>();
            Invoke(inventory, "Awake");
            var provider = AddComponent<InventorySaveProvider>();
            Set(provider, "inventory", inventory);
            Set(provider, "database", database);

            LogAssert.Expect(LogType.Warning, new Regex("out of range"));
            LogAssert.Expect(LogType.Warning, new Regex("unknown item id 'deleted'"));
            provider.RestoreState("{\"slots\":[{\"index\":99,\"itemId\":\"coin\",\"quantity\":1},{\"index\":0,\"itemId\":\"deleted\",\"quantity\":1},{\"index\":1,\"itemId\":\"coin\",\"quantity\":3}]}");

            Assert.IsTrue(inventory.GetSlot(0).IsEmpty);
            Assert.AreEqual(3, inventory.GetSlot(1).Stack.Quantity);
        }
    }
}

namespace Game.Tests
{
    public class DropPlacementGroundTests : TestBase
    {
        private GameObject Box(string name, Vector3 center, Vector3 size)
        {
            var box = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            box.name = name;
            box.transform.position = center;
            box.transform.localScale = size;
            return box;
        }

        private GameObject Floor() => Box("floor", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f));   // top at y = 0

        private bool Probe(out float groundY)
        {
            Physics.SyncTransforms();
            return DropPlacement.TryFindGround(new Vector3(0f, 0f, 0f), 3f, 5f, ~0, out groundY);
        }

        [Test]
        public void TryFindGround_FindsTheSurfaceBelow()
        {
            Floor();

            Assert.IsTrue(Probe(out float groundY));
            Assert.AreEqual(0f, groundY, 0.001f);
        }

        [Test]
        public void TryFindGround_NothingBelow_ReturnsFalse()
        {
            Assert.IsFalse(Probe(out _));
        }

        [Test]
        public void TryFindGround_SkipsTriggerColliders()
        {
            Floor();
            Box("trigger", new Vector3(0f, 0.5f, 0f), Vector3.one).GetComponent<Collider>().isTrigger = true;

            Probe(out float groundY);

            Assert.AreEqual(0f, groundY, 0.001f, "a trigger volume is not something to land on");
        }

        [Test]
        public void TryFindGround_SkipsRigidbodies()
        {
            Floor();
            Box("character", new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 1f)).AddComponent<Rigidbody>();

            Probe(out float groundY);

            Assert.AreEqual(0f, groundY, 0.001f, "an item must not land on a character's head");
        }

        [Test]
        public void TryFindGround_SkipsInteractablesEvenWithoutATrigger()
        {
            Floor();
            var pickup = Box("old pickup", new Vector3(0f, 0.5f, 0f), Vector3.one);
            pickup.AddComponent<WorldItem>();   // a solid collider, but it is a pickup

            Probe(out float groundY);

            Assert.AreEqual(0f, groundY, 0.001f, "an item must not stack on another pickup");
        }

        [Test]
        public void RestOnGround_LowersTheObjectSoItsBottomTouchesTheGround()
        {
            var cube = Box("cube", new Vector3(0f, 5f, 0f), Vector3.one);

            DropPlacement.RestOnGround(cube, 0f);

            Assert.AreEqual(0.5f, cube.transform.position.y, 0.001f);
        }
    }
}
