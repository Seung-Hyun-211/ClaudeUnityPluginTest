using System;
using NUnit.Framework;
using UnityEngine;
using Game.Building;

namespace Game.Tests
{
    public class BuildGridTests
    {
        [TestCase(0f, 0)]
        [TestCase(1.4f, 0)]
        [TestCase(2.4f, 0)]   // top of a level-0 wall
        [TestCase(2.6f, 1)]   // side of the level-1 slab
        [TestCase(3.0f, 1)]   // level-1 slab top
        [TestCase(5.4f, 1)]
        [TestCase(5.6f, 2)]
        public void LevelAt_BelongsToTheLevelWhoseSlabOrWallsAreThere(float y, int expected)
        {
            Assert.AreEqual(expected, BuildGrid.LevelAt(y, 0f));
        }

        [Test]
        public void LevelAt_IsRelativeToTheGroundHeight()
        {
            Assert.AreEqual(0, BuildGrid.LevelAt(10f, 10f));
            Assert.AreEqual(1, BuildGrid.LevelAt(13f, 10f));
            Assert.AreEqual(13f, BuildGrid.TopOf(1, 10f));
        }

        [Test]
        public void Floor_SnapsToTheCellContainingThePoint()
        {
            var key = BuildGrid.KeyAt(PieceKind.Floor, new Vector3(12.3f, 0f, 24.9f), new Vector3(10f, 0f, 20f));

            Assert.AreEqual(PieceKey.Floor(2, 4, 0), key);
        }

        [Test]
        public void Floor_SnapsCorrectlyForNegativeCoordinates()
        {
            var key = BuildGrid.KeyAt(PieceKind.Floor, new Vector3(-0.3f, 0f, -1.2f), Vector3.zero);

            Assert.AreEqual(PieceKey.Floor(-1, -2, 0), key);
        }

        [Test]
        public void Wall_SnapsToTheNearestGridLine()
        {
            // Close to the vertical line x = 2: an edge along Z.
            Assert.AreEqual(PieceKey.Wall(2, 4, Axis.Z, 0), BuildGrid.KeyAt(PieceKind.Wall, new Vector3(2.05f, 0f, 4.5f), Vector3.zero));

            // Close to the horizontal line z = 5: an edge along X.
            Assert.AreEqual(PieceKey.Wall(2, 5, Axis.X, 0), BuildGrid.KeyAt(PieceKind.Wall, new Vector3(2.5f, 0f, 4.9f), Vector3.zero));
        }

        [Test]
        public void Wall_TieBetweenTwoLines_PicksTheSameOneEveryTime()
        {
            var a = BuildGrid.KeyAt(PieceKind.Wall, new Vector3(0.5f, 0f, 0.5f), Vector3.zero);
            var b = BuildGrid.KeyAt(PieceKind.Wall, new Vector3(0.5f, 0f, 0.5f), Vector3.zero);

            Assert.AreEqual(a, b);
            Assert.AreEqual(Axis.Z, a.Axis);
        }

        [Test]
        public void Door_SnapsLikeAWall_ButKeepsItsKind()
        {
            var key = BuildGrid.KeyAt(PieceKind.Door, new Vector3(2.05f, 0f, 4.5f), Vector3.zero);

            Assert.AreEqual(PieceKind.Door, key.Kind);
            Assert.AreEqual(PieceKey.Wall(2, 4, Axis.Z, 0).Slot, key.Slot);
        }

        [Test]
        public void HigherPoint_MeansAHigherLevel()
        {
            Assert.AreEqual(1, BuildGrid.KeyAt(PieceKind.Floor, new Vector3(0.5f, 3f, 0.5f), Vector3.zero).Level);
        }

        [Test]
        public void PointBelowTheGround_ClampsToLevelZero()
        {
            Assert.AreEqual(0, BuildGrid.KeyAt(PieceKind.Floor, new Vector3(0.5f, -5f, 0.5f), Vector3.zero).Level);
        }

        [Test]
        public void Pillars_CannotBeAimedAt()
        {
            Assert.Throws<ArgumentException>(() => BuildGrid.KeyAt(PieceKind.Pillar, Vector3.zero, Vector3.zero));
        }

        [Test]
        public void Center_PutsEachPieceWhereTheLayoutSaysItGoes()
        {
            var origin = new Vector3(10f, 0f, 20f);

            AssertClose(new Vector3(11.5f, -0.25f, 22.5f), BuildGrid.Center(PieceKey.Floor(1, 2, 0), origin));
            AssertClose(new Vector3(11.5f, 1.25f, 22f), BuildGrid.Center(PieceKey.Wall(1, 2, Axis.X, 0), origin));
            AssertClose(new Vector3(11f, 1.25f, 22.5f), BuildGrid.Center(PieceKey.Wall(1, 2, Axis.Z, 0), origin));
            AssertClose(new Vector3(11f, 1.25f, 22f), BuildGrid.Center(PieceKey.Pillar(1, 2, 0), origin));
        }

        [Test]
        public void Center_HigherLevelsSitOneFloorHeightUp()
        {
            var origin = new Vector3(0f, 2f, 0f);

            AssertClose(new Vector3(0.5f, 4.75f, 0.5f), BuildGrid.Center(PieceKey.Floor(0, 0, 1), origin));
            AssertClose(new Vector3(0.5f, 6.25f, 0f), BuildGrid.Center(PieceKey.Wall(0, 0, Axis.X, 1), origin));
        }

        [Test]
        public void GroundFloor_IsFlushWithTheGround()
        {
            // The slab reaches from half a metre below the ground up to it: nothing to step onto.
            var center = BuildGrid.Center(PieceKey.Floor(0, 0, 0), Vector3.zero);
            float top = center.y + BuildGrid.Size(PieceKind.Floor).y * 0.5f;

            Assert.AreEqual(0f, top, 0.0001f);
        }

        [Test]
        public void Walls_StandOnTheFloorTopAndReachTheNextSlab()
        {
            var center = BuildGrid.Center(PieceKey.Wall(0, 0, Axis.X, 0), Vector3.zero);
            float bottom = center.y - BuildGrid.Size(PieceKind.Wall).y * 0.5f;
            float top = center.y + BuildGrid.Size(PieceKind.Wall).y * 0.5f;

            Assert.AreEqual(0f, bottom, 0.0001f);
            // The next level's slab occupies [top of wall, floor height].
            Assert.AreEqual(BuildGrid.FloorHeight - BuildGrid.SlabThickness, top, 0.0001f);
        }

        [Test]
        public void Rotation_TurnsOnlyEdgesAlongZ()
        {
            Assert.AreEqual(Quaternion.identity, BuildGrid.Rotation(PieceKey.Wall(0, 0, Axis.X, 0)));
            Assert.AreEqual(Quaternion.identity, BuildGrid.Rotation(PieceKey.Floor(0, 0, 0)));
            Assert.AreEqual(90f, BuildGrid.Rotation(PieceKey.Door(0, 0, Axis.Z, 0)).eulerAngles.y, 0.001f);
        }

        [Test]
        public void Size_MatchesTheDesign()
        {
            Assert.AreEqual(new Vector3(1f, 0.5f, 1f), BuildGrid.Size(PieceKind.Floor));
            Assert.AreEqual(new Vector3(1f, 2.5f, 0.5f), BuildGrid.Size(PieceKind.Wall));
            Assert.AreEqual(new Vector3(1f, 2.5f, 0.5f), BuildGrid.Size(PieceKind.Door));
            Assert.AreEqual(new Vector3(0.5f, 2.5f, 0.5f), BuildGrid.Size(PieceKind.Pillar));
        }

        private static void AssertClose(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f, "x");
            Assert.AreEqual(expected.y, actual.y, 0.0001f, "y");
            Assert.AreEqual(expected.z, actual.z, 0.0001f, "z");
        }
    }
}
