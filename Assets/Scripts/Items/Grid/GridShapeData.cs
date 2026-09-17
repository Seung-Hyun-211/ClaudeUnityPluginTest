using UnityEngine;

namespace Game.Items.Grid
{
    /// <summary>
    /// Defines the usable cells of a container (pocket/rig/backpack) as data,
    /// so new shapes are authored as assets instead of code. A plain rectangle
    /// (e.g. the default 4x2 pocket) just leaves every cell usable.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Grid Shape", fileName = "New Grid Shape")]
    public class GridShapeData : ScriptableObject
    {
        [SerializeField, Min(1)] private int width = 4;
        [SerializeField, Min(1)] private int height = 2;
        [SerializeField] private bool[] cellMask;

        public int Width => width;
        public int Height => height;
        public int CellCount => width * height;

        public bool IsCellUsable(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            int index = y * width + x;
            if (cellMask == null || cellMask.Length != CellCount)
            {
                return true;
            }

            return cellMask[index];
        }

        private void OnValidate()
        {
            int required = CellCount;
            if (cellMask != null && cellMask.Length == required)
            {
                return;
            }

            var resized = new bool[required];
            for (int i = 0; i < required; i++)
            {
                resized[i] = cellMask == null || i >= cellMask.Length || cellMask[i];
            }
            cellMask = resized;
        }
    }
}
