using NUnit.Framework;
using UnityEngine;
using Game.Items;
using Game.Items.Grid;

namespace Game.Tests
{
    public class GridItemDragMoverTests : TestBase
    {
        private static int TotalOf(ItemData item, params GridInventory[] grids)
        {
            int total = 0;
            foreach (var grid in grids)
            {
                foreach (var placed in grid.PlacedItems)
                {
                    if (placed.Stack.Item == item)
                    {
                        total += placed.Stack.Quantity;
                    }
                }
            }
            return total;
        }

        [Test]
        public void Move_ToFreeCellOfAnotherGrid_MovesTheItem()
        {
            var source = MakeGrid(4, 2);
            var target = MakeGrid(4, 2);
            var key = MakeItem("key");
            source.TryPlaceAt(key, 1, Vector2Int.zero, out _);

            GridItemDragMover.Move(source, source.PlacedItems[0], target, new Vector2Int(2, 1), false);

            Assert.AreEqual(0, source.PlacedItems.Count);
            Assert.AreEqual(new Vector2Int(2, 1), target.PlacedItems[0].Origin);
        }

        [Test]
        public void Move_AppliesTheChosenRotation()
        {
            var source = MakeGrid(4, 2);
            var target = MakeGrid(3, 3);
            var knife = MakeItem("knife", 1, 2);
            source.TryPlaceAt(knife, 1, Vector2Int.zero, out _);

            GridItemDragMover.Move(source, source.PlacedItems[0], target, Vector2Int.zero, true);

            Assert.IsTrue(target.PlacedItems[0].IsRotated);
            Assert.AreEqual(new Vector2Int(2, 1), target.PlacedItems[0].FootprintSize);
        }

        [Test]
        public void Move_ToOccupiedCell_FallsBackToAutoPlacement()
        {
            var source = MakeGrid(4, 2);
            var target = MakeGrid(4, 2);
            var key = MakeItem("key");
            var blocker = MakeItem("blocker");
            source.TryPlaceAt(key, 1, Vector2Int.zero, out _);
            target.TryPlaceAt(blocker, 1, Vector2Int.zero, out _);

            GridItemDragMover.Move(source, source.PlacedItems[0], target, Vector2Int.zero, false);

            Assert.AreEqual(0, source.PlacedItems.Count);
            Assert.AreEqual(2, target.PlacedItems.Count);
            Assert.AreEqual(1, TotalOf(key, target));
        }

        [Test]
        public void Move_TargetFull_RestoresTheItemToItsOriginalCellAndOrientation()
        {
            var source = MakeGrid(4, 2);
            var target = MakeGrid(1, 1);
            var plank = MakeItem("plank", 1, 3);
            target.TryAddItem(MakeItem("blocker"), 1);
            source.TryPlaceAt(plank, 1, new Vector2Int(1, 0), out _, rotated: true);

            GridItemDragMover.Move(source, source.PlacedItems[0], target, Vector2Int.zero, false);

            Assert.AreEqual(1, source.PlacedItems.Count);
            Assert.AreEqual(new Vector2Int(1, 0), source.PlacedItems[0].Origin);
            Assert.IsTrue(source.PlacedItems[0].IsRotated);
            Assert.AreEqual(1, target.PlacedItems.Count);
        }

        [Test]
        public void Move_TargetHasNoShape_RestoresTheItem()
        {
            var source = MakeGrid(4, 2);
            var target = AddComponent<GridInventory>();
            var key = MakeItem("key");
            source.TryPlaceAt(key, 1, new Vector2Int(2, 1), out _);

            GridItemDragMover.Move(source, source.PlacedItems[0], target, Vector2Int.zero, false);

            Assert.AreEqual(new Vector2Int(2, 1), source.PlacedItems[0].Origin);
        }

        [Test]
        public void Move_OntoAStackOfTheSameItem_MergesWithoutLosingOrDuplicating()
        {
            var source = MakeGrid(4, 2);
            var target = MakeGrid(4, 2);
            var coin = MakeItem("coin", 1, 1, maxStack: 10);
            source.TryPlaceAt(coin, 4, Vector2Int.zero, out _);
            target.TryPlaceAt(coin, 3, Vector2Int.zero, out _);

            GridItemDragMover.Move(source, source.PlacedItems[0], target, Vector2Int.zero, false);

            Assert.AreEqual(0, source.PlacedItems.Count);
            Assert.AreEqual(7, TotalOf(coin, target));
            Assert.AreEqual(1, target.PlacedItems.Count, "merged into the existing stack");
        }

        [Test]
        public void Move_WithinTheSameGrid_OntoItsOwnCell_KeepsTheItem()
        {
            var grid = MakeGrid(4, 2);
            var key = MakeItem("key");
            grid.TryPlaceAt(key, 1, new Vector2Int(1, 1), out _);

            GridItemDragMover.Move(grid, grid.PlacedItems[0], grid, new Vector2Int(1, 1), false);

            Assert.AreEqual(1, grid.PlacedItems.Count);
            Assert.AreEqual(new Vector2Int(1, 1), grid.PlacedItems[0].Origin);
        }

        [Test]
        public void Move_ItemNoLongerInTheSourceGrid_DoesNothing()
        {
            var source = MakeGrid(4, 2);
            var target = MakeGrid(4, 2);
            var key = MakeItem("key");
            source.TryPlaceAt(key, 1, Vector2Int.zero, out _);
            var stale = source.PlacedItems[0];
            source.RemoveItem(stale);

            GridItemDragMover.Move(source, stale, target, Vector2Int.zero, false);

            Assert.AreEqual(0, target.PlacedItems.Count, "a stale reference must not conjure an item");
        }
    }
}
