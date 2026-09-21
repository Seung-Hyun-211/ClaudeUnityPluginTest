using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.ActionMode;
using Game.Building;
using Game.Items;

namespace Game.Tests
{
    /// <summary>The extension points of the building code: what makes a new piece kind an addition instead of a change.</summary>
    public class BuildingDesignTests : TestBase
    {
        // ---- the graph delegates to per-kind rules ----

        private sealed class RefusingRule : IPieceRule
        {
            public bool CanDemolish => true;
            public PlacementFailure Check(StructureGraph graph, PieceKey key, Func<int, int, bool> groundSupport) => PlacementFailure.Unsupported;
            public IEnumerable<PieceKey> Companions(PieceKey placed) => Array.Empty<PieceKey>();
            public IEnumerable<PieceKey> Dependents(PieceKey removed) => Array.Empty<PieceKey>();
            public bool IsSupported(StructureGraph graph, PieceKey key) => true;
            public IEnumerable<PieceKey> Collateral(StructureGraph graph, PieceKey destroyed) => Array.Empty<PieceKey>();
        }

        /// <summary>A floor that also brings a neighbour along and takes another with it when destroyed.</summary>
        private sealed class ClusterFloorRule : IPieceRule
        {
            public bool CanDemolish => true;
            public PlacementFailure Check(StructureGraph graph, PieceKey key, Func<int, int, bool> groundSupport) =>
                graph.Contains(key) ? PlacementFailure.Occupied : PlacementFailure.None;
            public IEnumerable<PieceKey> Companions(PieceKey placed) { yield return PieceKey.Floor(placed.X + 1, placed.Z, placed.Level); }
            public IEnumerable<PieceKey> Dependents(PieceKey removed) => Array.Empty<PieceKey>();
            public bool IsSupported(StructureGraph graph, PieceKey key) => true;
            public IEnumerable<PieceKey> Collateral(StructureGraph graph, PieceKey destroyed) { yield return PieceKey.Floor(destroyed.X + 1, destroyed.Z, destroyed.Level); }
        }

        [Test]
        public void Graph_AsksTheRuleOfTheKind_NotItsOwnSwitch()
        {
            var rules = PieceRules.CreateDefault();
            rules[PieceKind.Wall] = new RefusingRule();
            var graph = new StructureGraph(rules);
            graph.TryPlace(PieceKey.Floor(0, 0, 0));

            Assert.AreEqual(PlacementFailure.Unsupported, graph.TryPlace(PieceKey.Wall(0, 0, Axis.X, 0)).Failure);
            Assert.AreEqual(PlacementFailure.None, graph.Check(PieceKey.Floor(4, 4, 0)), "other kinds keep their own rules");
        }

        [Test]
        public void Graph_CompanionsAndCollateralComeFromTheRule()
        {
            var rules = PieceRules.CreateDefault();
            rules[PieceKind.Floor] = new ClusterFloorRule();
            var graph = new StructureGraph(rules);

            var placed = graph.TryPlace(PieceKey.Floor(0, 0, 0));
            CollectionAssert.AreEquivalent(new[] { PieceKey.Floor(0, 0, 0), PieceKey.Floor(1, 0, 0) }, placed.Added);

            var destroyed = graph.Destroy(PieceKey.Floor(0, 0, 0));
            CollectionAssert.AreEquivalent(new[] { PieceKey.Floor(0, 0, 0), PieceKey.Floor(1, 0, 0) }, destroyed);
            Assert.AreEqual(0, graph.Count);
        }

        [Test]
        public void Graph_KindWithoutARule_CannotBePlaced()
        {
            var graph = new StructureGraph(new Dictionary<PieceKind, IPieceRule>());

            Assert.AreEqual(PlacementFailure.NotPlaceable, graph.Check(PieceKey.Floor(0, 0, 0)));
        }

        [Test]
        public void Destroy_OfAnOrdinaryPiece_IsLikeDemolishing_ButWorksOnPillarsToo()
        {
            var graph = new StructureGraph();
            graph.TryPlace(PieceKey.Floor(0, 0, 0));
            graph.TryPlace(PieceKey.Wall(0, 0, Axis.X, 0));

            CollectionAssert.IsNotEmpty(graph.Destroy(PieceKey.Wall(0, 0, Axis.X, 0)));
            Assert.AreEqual(1, graph.Count, "the wall and its pillars are gone, the floor stays");
        }

        // ---- geometry per kind ----

        private sealed class TallGeometry : IPieceGeometry
        {
            public Vector3 Size => new(9f, 9f, 9f);
            public PieceKey KeyAt(PieceKind kind, float cellX, float cellZ, int level) => new(kind, 7, 7, level);
            public Vector3 Center(PieceKey key, Vector3 origin) => origin + Vector3.up * 42f;
            public Quaternion Rotation(PieceKey key) => Quaternion.Euler(0f, 180f, 0f);
        }

        [Test]
        public void GeometryRegistry_ReturnsWhatWasRegistered_AndKeepsTheDefaultsApart()
        {
            var custom = PieceGeometries.CreateDefault();
            var tall = new TallGeometry();
            custom.Register(PieceKind.Pillar, tall);

            Assert.AreSame(tall, custom.For(PieceKind.Pillar));
            Assert.AreNotSame(tall, PieceGeometries.Default.For(PieceKind.Pillar), "a private registry does not touch the shared one");
        }

        [Test]
        public void GeometryRegistry_UnknownKind_IsAnError()
        {
            Assert.Throws<ArgumentException>(() => new PieceGeometries().For(PieceKind.Floor));
        }

        [Test]
        public void Geometry_WallAndDoorShareTheEdgeShape()
        {
            Assert.AreSame(PieceGeometries.Default.For(PieceKind.Wall), PieceGeometries.Default.For(PieceKind.Door));
        }

        // ---- catalog is data ----

        private BuildPieceData MakeData(string id, PieceKind kind)
        {
            var data = NewAsset<BuildPieceData>();
            Set(data, "pieceId", id);
            Set(data, "kind", kind);
            return data;
        }

        [Test]
        public void Catalog_CategoriesWithoutAnEntryAreUnavailable_AndKindsResolveThroughTheData()
        {
            var catalog = NewAsset<BuildCatalog>();
            var wall = MakeData("wall", PieceKind.Wall);
            var door = MakeData("door", PieceKind.Door);
            var pillar = MakeData("pillar", PieceKind.Pillar);
            Set(catalog, "entries", new[]
            {
                new BuildCatalog.CategoryEntry { category = BuildCategory.Wall, data = wall },
                new BuildCatalog.CategoryEntry { category = BuildCategory.Door, data = door },
            });
            Set(catalog, "pillar", pillar);

            Assert.IsTrue(catalog.IsAvailable(BuildCategory.Wall));
            Assert.IsFalse(catalog.IsAvailable(BuildCategory.Floor));
            Assert.IsFalse(catalog.IsAvailable(BuildCategory.Stairs));
            Assert.AreSame(wall, catalog.ForKind(PieceKind.Wall));
            Assert.AreSame(door, catalog.ForKind(PieceKind.Door));
            Assert.AreSame(pillar, catalog.ForKind(PieceKind.Pillar));
            Assert.IsNull(catalog.ForKind(PieceKind.Floor));
        }

        [Test]
        public void Catalog_FindsPiecesById_IncludingThePillar()
        {
            var catalog = NewAsset<BuildCatalog>();
            var door = MakeData("door", PieceKind.Door);
            var pillar = MakeData("pillar", PieceKind.Pillar);
            Set(catalog, "entries", new[] { new BuildCatalog.CategoryEntry { category = BuildCategory.Door, data = door } });
            Set(catalog, "pillar", pillar);

            Assert.AreSame(door, catalog.Find("door"));
            Assert.AreSame(pillar, catalog.Find("pillar"));
            Assert.IsNull(catalog.Find("nope"));
        }

        [Test]
        public void Catalog_ANewCategoryIsJustAnotherEntry()
        {
            var catalog = NewAsset<BuildCatalog>();
            var stairs = MakeData("stairs", PieceKind.Floor); // stand-in kind: stage 2 will add a real one
            Set(catalog, "entries", new[] { new BuildCatalog.CategoryEntry { category = BuildCategory.Stairs, data = stairs } });

            Assert.IsTrue(catalog.IsAvailable(BuildCategory.Stairs));
            Assert.AreSame(stairs, catalog.Get(BuildCategory.Stairs));
        }

        // ---- economy ----

        private sealed class FakeStore : IItemStore
        {
            public readonly Dictionary<ItemData, int> Held = new();
            public int Capacity = int.MaxValue;

            public int CountOf(ItemData item) => Held.TryGetValue(item, out var n) ? n : 0;

            public bool TryConsume(ItemData item, int quantity)
            {
                if (CountOf(item) < quantity)
                {
                    return false;
                }

                Held[item] = CountOf(item) - quantity;
                return true;
            }

            public int Add(ItemData item, int quantity)
            {
                int fits = Math.Min(quantity, Capacity - CountOf(item));
                Held[item] = CountOf(item) + fits;
                return quantity - fits;
            }
        }

        private sealed class RecordingDropper : IItemDropper
        {
            public readonly List<(ItemData item, int quantity, Vector3 position)> Drops = new();
            public void Drop(ItemData item, int quantity, Vector3 position) => Drops.Add((item, quantity, position));
        }

        private BuildPieceData MakePriced(ItemData wood, int woodCount, ItemData metal, int metalCount, float refund = 0.5f)
        {
            var data = NewAsset<BuildPieceData>();
            Set(data, "cost", new[]
            {
                new BuildCost { item = wood, count = woodCount },
                new BuildCost { item = metal, count = metalCount },
            });
            Set(data, "refundRatio", refund);
            return data;
        }

        [Test]
        public void Economy_PaysAllOrNothing()
        {
            var wood = MakeItem("wood");
            var metal = MakeItem("metal");
            var store = new FakeStore();
            store.Held[wood] = 10;
            store.Held[metal] = 0;
            var economy = new BuildEconomy(store, new RecordingDropper(), () => Vector3.zero);
            var data = MakePriced(wood, 3, metal, 1);

            Assert.IsFalse(economy.CanAfford(data));
            Assert.IsFalse(economy.TryPay(data));
            Assert.AreEqual(10, store.CountOf(wood), "no material is spent when one is short");

            store.Held[metal] = 2;
            Assert.IsTrue(economy.TryPay(data));
            Assert.AreEqual(7, store.CountOf(wood));
            Assert.AreEqual(1, store.CountOf(metal));
        }

        [Test]
        public void Economy_RefundReturnsAShare_AndDropsWhatDoesNotFit()
        {
            var wood = MakeItem("wood");
            var metal = MakeItem("metal");
            var store = new FakeStore { Capacity = 4 };
            var dropper = new RecordingDropper();
            var here = new Vector3(1f, 2f, 3f);
            var economy = new BuildEconomy(store, dropper, () => here);
            var data = MakePriced(wood, 10, metal, 1); // refund 5 wood and 0 metal (rounded down)

            economy.Refund(data);

            Assert.AreEqual(4, store.CountOf(wood));
            Assert.AreEqual(1, dropper.Drops.Count);
            Assert.AreEqual((wood, 1, here), dropper.Drops[0]);
        }

        [Test]
        public void Economy_RefundOfNothing_DoesNothing()
        {
            var economy = new BuildEconomy(new FakeStore(), new RecordingDropper(), () => Vector3.zero);

            Assert.DoesNotThrow(() => economy.Refund(null));
        }

        [Test]
        public void Economy_RefundAll_GivesTheWholeCostBack_ForAPlacementThatDidNotHappen()
        {
            var wood = MakeItem("wood");
            var metal = MakeItem("metal");
            var store = new FakeStore();
            store.Held[wood] = 7;
            store.Held[metal] = 2;
            var economy = new BuildEconomy(store, new RecordingDropper(), () => Vector3.zero);
            var data = MakePriced(wood, 3, metal, 1);

            Assert.IsTrue(economy.TryPay(data));
            economy.RefundAll(data);

            Assert.AreEqual(7, store.CountOf(wood));
            Assert.AreEqual(2, store.CountOf(metal));
        }

        // ---- catalog configuration ----

        private BuildPieceData MakeDataWithPrefab(string id, PieceKind kind)
        {
            var data = MakeData(id, kind);
            Set(data, "prefab", NewGameObject(id + " prefab"));
            return data;
        }

        [Test]
        public void Catalog_ACorrectConfiguration_HasNoProblems()
        {
            var catalog = NewAsset<BuildCatalog>();
            Set(catalog, "entries", new[]
            {
                new BuildCatalog.CategoryEntry { category = BuildCategory.Wall, data = MakeDataWithPrefab("wall", PieceKind.Wall) },
                new BuildCatalog.CategoryEntry { category = BuildCategory.Door, data = MakeDataWithPrefab("door", PieceKind.Door) },
            });
            Set(catalog, "pillar", MakeDataWithPrefab("pillar", PieceKind.Pillar));

            Assert.IsEmpty(catalog.FindProblems());
        }

        [Test]
        public void Catalog_ReportsTwoEntriesOfOneKind_BecauseVariantsAreNotSupported()
        {
            var catalog = NewAsset<BuildCatalog>();
            Set(catalog, "entries", new[]
            {
                new BuildCatalog.CategoryEntry { category = BuildCategory.Wall, data = MakeDataWithPrefab("wood wall", PieceKind.Wall) },
                new BuildCatalog.CategoryEntry { category = BuildCategory.Door, data = MakeDataWithPrefab("metal wall", PieceKind.Wall) },
            });

            Assert.IsNotEmpty(catalog.FindProblems());
        }

        [Test]
        public void Catalog_ReportsMissingDataPrefabAndMisplacedPillars()
        {
            var catalog = NewAsset<BuildCatalog>();
            Set(catalog, "entries", new[]
            {
                new BuildCatalog.CategoryEntry { category = BuildCategory.Wall, data = null },
                new BuildCatalog.CategoryEntry { category = BuildCategory.Floor, data = MakeData("floor", PieceKind.Floor) }, // no prefab
                new BuildCatalog.CategoryEntry { category = BuildCategory.Door, data = MakeDataWithPrefab("post", PieceKind.Pillar) },
            });
            Set(catalog, "pillar", MakeDataWithPrefab("not a pillar", PieceKind.Wall));

            Assert.GreaterOrEqual(catalog.FindProblems().Count, 4);
        }

        // ---- action mode ----

        [Test]
        public void ActionMode_NoSource_MeansCombat_SoGatingIsOptional()
        {
            IPlayerActionMode none = null;

            Assert.IsTrue(none.IsCombat());
        }

        [Test]
        public void ActionMode_Consumers_SeeOnlyTheReadSide()
        {
            var component = AddComponent<PlayerActionModeSwitch>();
            IPlayerActionMode reader = component;
            IPlayerActionModeSetter writer = component;

            writer.Set(PlayerActionMode.Build);

            Assert.IsFalse(reader.IsCombat());
            writer.Toggle();
            Assert.IsTrue(reader.IsCombat());
        }
    }
}
