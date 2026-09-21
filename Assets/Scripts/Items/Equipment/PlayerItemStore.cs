using UnityEngine;
using Game.Items.Grid;

namespace Game.Items.Equipment
{
    /// <summary>
    /// The player's carried items as one IItemStore: Pocket, Rig and Backpack
    /// grids, then the flat inventory - the same order WorldItem pickups fill
    /// them in, so spending and picking up stay symmetrical.
    /// </summary>
    public class PlayerItemStore : MonoBehaviour, IItemStore
    {
        private static readonly ContainerCategory[] Order =
        {
            ContainerCategory.Pocket, ContainerCategory.Rig, ContainerCategory.Backpack,
        };

        [SerializeField] private ContainerEquipmentController equipment;

        [Tooltip("Optional. Found on this GameObject when left empty.")]
        [SerializeField] private Inventory flatInventory;

        private void Awake()
        {
            if (flatInventory == null)
            {
                TryGetComponent(out flatInventory);
            }
        }

        public int CountOf(ItemData item)
        {
            if (item == null)
            {
                return 0;
            }

            int total = flatInventory != null ? flatInventory.GetQuantity(item) : 0;
            foreach (var category in Order)
            {
                var grid = equipment != null ? equipment.GetGrid(category) : null;
                total += grid != null ? grid.CountOf(item) : 0;
            }
            return total;
        }

        public bool TryConsume(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return item != null;
            }

            if (CountOf(item) < quantity)
            {
                return false;
            }

            int remaining = quantity;
            foreach (var category in Order)
            {
                var grid = equipment != null ? equipment.GetGrid(category) : null;
                if (grid != null && remaining > 0)
                {
                    remaining -= grid.RemoveQuantity(item, remaining);
                }
            }

            if (remaining > 0 && flatInventory != null)
            {
                flatInventory.RemoveItem(item, remaining);
                remaining = 0;
            }

            return remaining == 0;
        }

        public int Add(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                return 0;
            }

            int left = equipment != null ? equipment.AddToContainers(item, quantity) : quantity;
            if (left > 0 && flatInventory != null)
            {
                left = flatInventory.AddItem(item, left);
            }
            return left;
        }
    }
}
