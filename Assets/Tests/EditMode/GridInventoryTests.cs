using NUnit.Framework;
using UnityEngine;
using Game.Items;
using Game.Items.Grid;

namespace Game.Tests
{
    public class GridInventoryTests : TestBase
    {
        [Test]
        public void TryPlaceAt_FreeCell_PlacesItem()
        {
            var grid = MakeGrid(4, 2);
            var item = MakeItem("knife", 1, 2);

            bool placed = grid.TryPlaceAt(item, 1, new Vector2Int(1, 0), out int leftover);

            Assert.IsTrue(placed);
            Assert.AreEqual(0, leftover);
            Assert.AreEqual(1, grid.PlacedItems.Count);
            Assert.IsNotNull(grid.GetItemAt(new Vector2Int(1, 1)), "a 1x2 item also occupies the cell below its origin");
        }

        [Test]
        public void TryPlaceAt_OccupiedCell_FailsAndKeepsQuantity()
        {
            var grid = MakeGrid(4, 2);
            var item = MakeItem("key");
            grid.TryPlaceAt(item, 1, Vector2Int.zero, out _);

            bool placed = grid.TryPlaceAt(item, 1, Vector2Int.zero, out int leftover);

            Assert.IsFalse(placed);
            Assert.AreEqual(1, leftover);
            Assert.AreEqual(1, grid.PlacedItems.Count);
        }

        [Test]
        public void TryPlaceAt_PartlyOutsideShape_Fails()
        {
            var grid = MakeGrid(4, 2);
            var crate = MakeItem("crate", 3, 3);

            Assert.IsFalse(grid.TryPlaceAt(crate, 1, Vector2Int.zero, out _));
            Assert.IsFalse(grid.TryPlaceAt(MakeItem("pistol", 2, 1), 1, new Vector2Int(3, 0), out _));
        }

        [Test]
        public void TryPlaceAt_Rotated_SwapsFootprint()
        {
            var grid = MakeGrid(3, 3);
            var plank = MakeItem("plank", 1, 3);

            grid.TryPlaceAt(plank, 1, Vector2Int.zero, out _, rotated: true);

            var placed = grid.PlacedItems[0];
            Assert.IsTrue(placed.IsRotated);
            Assert.AreEqual(new Vector2Int(3, 1), placed.FootprintSize);
            Assert.IsTrue(placed.Occupies(new Vector2Int(2, 0)));
            Assert.IsFalse(placed.Occupies(new Vector2Int(0, 2)));
        }

        [Test]
        public void TryAddItem_StackableItem_FillsExistingStackBeforeOpeningANewOne()
        {
            var grid = MakeGrid(4, 2);
            var coin = MakeItem("coin", 1, 1, maxStack: 10);

            grid.TryAddItem(coin, 6);
            int leftover = grid.TryAddItem(coin, 6);

            Assert.AreEqual(0, leftover);
            Assert.AreEqual(2, grid.PlacedItems.Count);
            Assert.AreEqual(10, grid.PlacedItems[0].Stack.Quantity);
            Assert.AreEqual(2, grid.PlacedItems[1].Stack.Quantity);
        }

        [Test]
        public void TryAddItem_RotatesOnlyWhenTheOriginalOrientationDoesNotFit()
        {
            var grid = MakeGrid(4, 2);
            var plank = MakeItem("plank", 1, 3);   // 3 tall cannot fit a 2-tall grid
            var pistol = MakeItem("pistol", 2, 1); // fits as authored

            grid.TryAddItem(plank, 1);
            grid.TryAddItem(pistol, 1);

            Assert.IsTrue(grid.PlacedItems[0].IsRotated);
            Assert.AreEqual(new Vector2Int(3, 1), grid.PlacedItems[0].FootprintSize);
            Assert.IsFalse(grid.PlacedItems[1].IsRotated);
        }

        [Test]
        public void TryAddItem_GridFull_ReturnsTheLeftover()
        {
            var grid = MakeGrid(1, 1);
            var key = MakeItem("key");

            int leftover = grid.TryAddItem(key, 2);

            Assert.AreEqual(1, leftover);
            Assert.AreEqual(1, grid.PlacedItems.Count);
        }

        [Test]
        public void TryAddItem_NoShape_ReturnsEverything()
        {
            var grid = AddComponent<GridInventory>();

            Assert.AreEqual(3, grid.TryAddItem(MakeItem("coin", 1, 1, 10), 3));
        }

        [Test]
        public void SetShape_SmallerShape_EvictsItemsThatNoLongerFit()
        {
            var grid = MakeGrid(4, 2);
            var key = MakeItem("key");
            grid.TryPlaceAt(key, 1, new Vector2Int(0, 0), out _);
            grid.TryPlaceAt(key, 1, new Vector2Int(3, 1), out _);

            var evicted = grid.SetShape(MakeShape(2, 2));

            Assert.AreEqual(1, evicted.Count);
            Assert.AreEqual(1, grid.PlacedItems.Count);
            Assert.AreEqual(Vector2Int.zero, grid.PlacedItems[0].Origin);
        }

        [Test]
        public void SetShape_Null_EvictsEverything()
        {
            var grid = MakeGrid(4, 2);
            grid.TryAddItem(MakeItem("key"), 1);

            var evicted = grid.SetShape(null);

            Assert.AreEqual(1, evicted.Count);
            Assert.AreEqual(0, grid.PlacedItems.Count);
        }

        [Test]
        public void Clear_RemovesEverythingAndRaisesGridChanged()
        {
            var grid = MakeGrid(4, 2);
            grid.TryAddItem(MakeItem("key"), 1);
            int changes = 0;
            grid.GridChanged += () => changes++;

            grid.Clear();

            Assert.AreEqual(0, grid.PlacedItems.Count);
            Assert.AreEqual(1, changes);

            grid.Clear();
            Assert.AreEqual(1, changes, "clearing an empty grid is not a change");
        }
    }
}
