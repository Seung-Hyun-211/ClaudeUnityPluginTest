using System.Linq;
using NUnit.Framework;
using Game.Building;

namespace Game.Tests
{
    public class StructureGraphTests
    {
        private static PieceKey Floor(int x, int z, int level = 0) => PieceKey.Floor(x, z, level);
        private static PieceKey WallX(int x, int z, int level = 0) => PieceKey.Wall(x, z, Axis.X, level);
        private static PieceKey WallZ(int x, int z, int level = 0) => PieceKey.Wall(x, z, Axis.Z, level);
        private static PieceKey DoorX(int x, int z, int level = 0) => PieceKey.Door(x, z, Axis.X, level);

        private static StructureGraph Placed(params PieceKey[] keys)
        {
            var graph = new StructureGraph();
            foreach (var key in keys)
            {
                var result = graph.TryPlace(key);
                Assert.IsTrue(result.Success, $"setup: {key} -> {result.Failure}");
            }
            return graph;
        }

        // ---- floors ----

        [Test]
        public void GroundFloor_NeedsGroundSupportFromTheCaller()
        {
            var graph = new StructureGraph();

            Assert.AreEqual(PlacementFailure.Unsupported, graph.TryPlace(Floor(0, 0), (x, z) => false).Failure);
            Assert.IsTrue(graph.TryPlace(Floor(0, 0), (x, z) => x == 0 && z == 0).Success);
        }

        [Test]
        public void GroundFloor_WithoutAGroundRule_MayGoAnywhere()
        {
            Assert.IsTrue(new StructureGraph().TryPlace(Floor(50, -3)).Success);
        }

        [Test]
        public void SameFloorTwice_IsOccupied()
        {
            var graph = Placed(Floor(0, 0));

            Assert.AreEqual(PlacementFailure.Occupied, graph.TryPlace(Floor(0, 0)).Failure);
        }

        [Test]
        public void PillarsAndNegativeLevels_CannotBePlacedByHand()
        {
            var graph = new StructureGraph();

            Assert.AreEqual(PlacementFailure.NotPlaceable, graph.TryPlace(PieceKey.Pillar(0, 0, 0)).Failure);
            Assert.AreEqual(PlacementFailure.NotPlaceable, graph.TryPlace(Floor(0, 0, -1)).Failure);
        }

        // ---- walls, doors and pillars ----

        [Test]
        public void Wall_NeedsAFloorNextToIt()
        {
            var graph = new StructureGraph();
            Assert.AreEqual(PlacementFailure.Unsupported, graph.TryPlace(WallX(0, 0)).Failure);

            graph.TryPlace(Floor(0, 0));
            Assert.IsTrue(graph.TryPlace(WallX(0, 0)).Success);
        }

        [Test]
        public void Wall_IsHeldByAFloorOnEitherSide()
        {
            // Edge X(0,1) separates cells (0,0) and (0,1).
            Assert.IsTrue(Placed(Floor(0, 0)).TryPlace(WallX(0, 1)).Success);
            Assert.IsTrue(Placed(Floor(0, 1)).TryPlace(WallX(0, 1)).Success);
            Assert.IsFalse(Placed(Floor(5, 5)).TryPlace(WallX(0, 1)).Success);
        }

        [Test]
        public void Wall_CreatesAPillarAtEachEnd()
        {
            var graph = Placed(Floor(0, 0));

            var result = graph.TryPlace(WallX(0, 0));

            CollectionAssert.AreEquivalent(
                new[] { WallX(0, 0), PieceKey.Pillar(0, 0, 0), PieceKey.Pillar(1, 0, 0) },
                result.Added);
            Assert.IsTrue(graph.HasPillar(0, 0, 0));
            Assert.IsTrue(graph.HasPillar(1, 0, 0));
        }

        [Test]
        public void TwoWallsMeetingAtACorner_ShareOnePillar()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0));

            var result = graph.TryPlace(WallZ(0, 0)); // shares vertex (0,0); adds only (0,1)

            CollectionAssert.AreEquivalent(new[] { WallZ(0, 0), PieceKey.Pillar(0, 1, 0) }, result.Added);
            Assert.AreEqual(6, graph.Count, "floor + two walls + three pillars");
        }

        [Test]
        public void DoorAndWall_ShareOneSlot_AndSwapInBothDirections()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0));

            var toDoor = graph.TryPlace(DoorX(0, 0));
            Assert.IsTrue(toDoor.Success);
            CollectionAssert.AreEqual(new[] { WallX(0, 0) }, toDoor.Replaced);
            CollectionAssert.AreEqual(new[] { DoorX(0, 0) }, toDoor.Added, "no new pillars: they are already there");
            Assert.IsTrue(graph.Contains(DoorX(0, 0)));
            Assert.IsFalse(graph.Contains(WallX(0, 0)));

            var toWall = graph.TryPlace(WallX(0, 0));
            Assert.IsTrue(toWall.Success);
            CollectionAssert.AreEqual(new[] { DoorX(0, 0) }, toWall.Replaced);
        }

        [Test]
        public void SameWallTwice_IsOccupied()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0));

            Assert.AreEqual(PlacementFailure.Occupied, graph.TryPlace(WallX(0, 0)).Failure);
            Assert.AreEqual(PlacementFailure.None, graph.Check(DoorX(0, 0)));
        }

        [Test]
        public void FailedPlacement_ChangesNothing()
        {
            var graph = Placed(Floor(0, 0));
            int before = graph.Count;

            graph.TryPlace(WallX(9, 9));

            Assert.AreEqual(before, graph.Count);
        }

        [Test]
        public void DemolishingAWall_RemovesItsPillars_ExceptSharedOnes()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0), WallZ(0, 0));

            var removed = graph.Remove(WallX(0, 0));

            CollectionAssert.AreEquivalent(new[] { WallX(0, 0), PieceKey.Pillar(1, 0, 0) }, removed);
            Assert.IsTrue(graph.HasPillar(0, 0, 0), "the other wall still touches vertex (0,0)");
            Assert.IsTrue(graph.HasPillar(0, 1, 0));
        }

        [Test]
        public void DemolishingTheLastWallAtAVertex_RemovesThePillar()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0));

            graph.Remove(WallX(0, 0));

            Assert.AreEqual(1, graph.Count, "only the floor remains");
        }

        [Test]
        public void Pillars_CannotBeDemolishedDirectly()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0));

            Assert.IsEmpty(graph.Remove(PieceKey.Pillar(0, 0, 0)));
            Assert.IsTrue(graph.HasPillar(0, 0, 0));
        }

        [Test]
        public void RemovingSomethingThatIsNotThere_IsANoOp()
        {
            var graph = Placed(Floor(0, 0));

            Assert.IsEmpty(graph.Remove(Floor(3, 3)));
            Assert.IsEmpty(graph.Remove(WallX(0, 0)));
            Assert.AreEqual(1, graph.Count);
        }

        // ---- collapse ----

        [Test]
        public void RemovingAFloor_DropsTheWallsThatOnlyStoodOnIt()
        {
            // Cells (0,0) and (1,0) with a wall between them (Z edge at x = 1) and one on the outside of (0,0).
            var graph = Placed(Floor(0, 0), Floor(1, 0), WallZ(1, 0), WallX(0, 0));

            var removed = graph.Remove(Floor(0, 0));

            Assert.IsTrue(removed.Contains(WallX(0, 0)), "held only by cell (0,0)");
            Assert.IsFalse(removed.Contains(WallZ(1, 0)), "cell (1,0) still holds the wall between them");
            Assert.IsTrue(graph.Contains(WallZ(1, 0)));
        }

        [Test]
        public void RemovingAFloor_AlsoCleansUpThePillarsOfTheWallsThatFell()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0));

            graph.Remove(Floor(0, 0));

            Assert.AreEqual(0, graph.Count);
        }

        [Test]
        public void UpperFloor_NeedsAWallOrPillarBelow()
        {
            var graph = Placed(Floor(0, 0));
            Assert.AreEqual(PlacementFailure.Unsupported, graph.Check(Floor(0, 0, 1)), "nothing under it");

            graph.TryPlace(WallX(0, 0));
            Assert.AreEqual(PlacementFailure.None, graph.Check(Floor(0, 0, 1)), "its edge has a wall");
        }

        [Test]
        public void UpperFloor_IsAlsoHeldByAPillarAtOneOfItsCorners()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0)); // pillars at (0,0) and (1,0)

            // Cell (1,0) shares no edge with that wall, but its corner (1,0) has a pillar.
            Assert.AreEqual(PlacementFailure.None, graph.Check(Floor(1, 0, 1)));
            Assert.AreEqual(PlacementFailure.Unsupported, graph.Check(Floor(5, 5, 1)));
        }

        [Test]
        public void UpperWall_NeedsAFloorOfItsOwnLevel()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0), Floor(0, 0, 1));
            Assert.AreEqual(PlacementFailure.Unsupported, graph.Check(WallX(3, 3, 1)));

            Assert.IsTrue(graph.TryPlace(WallX(0, 0, 1)).Success);
        }

        [Test]
        public void RemovingAWall_DropsTheFloorAboveItAndWhatStandsOnThatFloor()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0), Floor(0, 0, 1), WallX(0, 0, 1));

            var removed = graph.Remove(WallX(0, 0));

            Assert.IsTrue(removed.Contains(Floor(0, 0, 1)));
            Assert.IsTrue(removed.Contains(WallX(0, 0, 1)), "the wall on the fallen floor goes too");
            Assert.AreEqual(1, graph.Count, "only the ground floor is left");
        }

        [Test]
        public void UpperFloor_SurvivesWhileAnotherSupportRemains()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0), WallZ(0, 0), Floor(0, 0, 1));

            var removed = graph.Remove(WallX(0, 0));

            Assert.IsFalse(removed.Contains(Floor(0, 0, 1)), "the Z wall on its other edge still holds it");
            Assert.IsTrue(graph.Contains(Floor(0, 0, 1)));
        }

        [Test]
        public void RemovingAWall_DropsAFloorThatOnlyHungOnItsPillar()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0), Floor(1, 0, 1)); // held only by the pillar at (1,0)

            var removed = graph.Remove(WallX(0, 0));

            Assert.IsTrue(removed.Contains(Floor(1, 0, 1)));
        }

        // ---- damage ----

        [Test]
        public void DestroyingAPillar_TakesEveryWallAtItsVertexAlong()
        {
            // Walls X(0,0) and Z(0,0) meet at vertex (0,0); a third wall elsewhere is unrelated.
            var graph = Placed(Floor(0, 0), Floor(3, 0), WallX(0, 0), WallZ(0, 0), WallX(3, 0));

            var removed = graph.DestroyPillar(PieceKey.Pillar(0, 0, 0));

            Assert.IsTrue(removed.Contains(PieceKey.Pillar(0, 0, 0)));
            Assert.IsTrue(removed.Contains(WallX(0, 0)));
            Assert.IsTrue(removed.Contains(WallZ(0, 0)));
            Assert.IsFalse(graph.Contains(WallX(0, 0)));
            Assert.IsTrue(graph.Contains(WallX(3, 0)), "unrelated");
            Assert.IsFalse(graph.HasPillar(1, 0, 0), "its only wall fell, so its pillar went too");
            Assert.IsFalse(graph.HasPillar(0, 1, 0));
        }

        [Test]
        public void DestroyingAPillar_DropsTheFloorAboveThatItAndItsWallsHeld()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0), Floor(0, 0, 1));

            var removed = graph.DestroyPillar(PieceKey.Pillar(0, 0, 0));

            Assert.IsTrue(removed.Contains(Floor(0, 0, 1)));
        }

        [Test]
        public void DestroyingAPillarThatIsNotThere_IsANoOp()
        {
            var graph = Placed(Floor(0, 0));

            Assert.IsEmpty(graph.DestroyPillar(PieceKey.Pillar(0, 0, 0)));
            Assert.IsEmpty(graph.DestroyPillar(WallX(0, 0)), "not a pillar");
        }

        [Test]
        public void EverythingRemoved_IsReportedExactlyOnce()
        {
            var graph = Placed(Floor(0, 0), WallX(0, 0), WallZ(0, 0), Floor(0, 0, 1), WallX(0, 0, 1));

            var removed = graph.DestroyPillar(PieceKey.Pillar(0, 0, 0));

            Assert.AreEqual(removed.Count, removed.Distinct().Count());
            Assert.IsTrue(removed.All(key => !graph.Contains(key)));
        }

        [Test]
        public void All_ListsEveryPieceWithItsRealKind()
        {
            var graph = Placed(Floor(0, 0), DoorX(0, 0));

            var all = graph.All().ToList();

            Assert.IsTrue(all.Contains(DoorX(0, 0)));
            Assert.IsFalse(all.Contains(WallX(0, 0)));
            Assert.AreEqual(graph.Count, all.Count);
        }

        [Test]
        public void ADoor_HoldsUpAFloorAndFallsLikeAWall()
        {
            var graph = Placed(Floor(0, 0), DoorX(0, 0), Floor(0, 0, 1));

            var removed = graph.Remove(Floor(0, 0));

            Assert.IsTrue(removed.Contains(DoorX(0, 0)));
            Assert.IsTrue(removed.Contains(Floor(0, 0, 1)));
        }
    }
}
