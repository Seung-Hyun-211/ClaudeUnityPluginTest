using UnityEngine;
using Game.Items.Grid;

namespace Game.Items.Equipment
{
    /// <summary>
    /// An equippable item that grants a grid inventory when worn. Different
    /// pockets/rigs/backpacks are just different assets pointing at different
    /// GridShapeData, so new sizes/shapes never require new code.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Container Item Data", fileName = "New Container")]
    public class ContainerItemData : ItemData
    {
        [SerializeField] private ContainerCategory category;
        [SerializeField] private GridShapeData shape;

        public ContainerCategory Category => category;
        public GridShapeData Shape => shape;
    }
}
