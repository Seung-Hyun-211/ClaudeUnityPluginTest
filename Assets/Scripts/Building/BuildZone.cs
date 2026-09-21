using UnityEngine;

namespace Game.Building
{
    /// <summary>
    /// An area where the player may build. The grid starts at this object's
    /// position (the corner with the lowest X and Z, at ground height) and
    /// extends <see cref="Width"/> x <see cref="Depth"/> cells along +X/+Z.
    /// The ground inside must be flat - floors are laid flush with it - and the
    /// zone is not rotated or scaled.
    /// </summary>
    public class BuildZone : MonoBehaviour
    {
        [SerializeField, Min(1)] private int width = 16;
        [SerializeField, Min(1)] private int depth = 16;

        [Tooltip("Highest level that may be built. 0 = ground floor only; raise it once stairs or ladders exist.")]
        [SerializeField, Min(0)] private int maxLevel;

        public Vector3 Origin => transform.position;
        public int Width => width;
        public int Depth => depth;
        public int MaxLevel => maxLevel;

        public bool ContainsCell(int x, int z) => x >= 0 && x < width && z >= 0 && z < depth;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.5f);
            Vector3 o = Origin;
            for (int x = 0; x <= width; x++)
            {
                Gizmos.DrawLine(o + new Vector3(x, 0.02f, 0f), o + new Vector3(x, 0.02f, depth));
            }

            for (int z = 0; z <= depth; z++)
            {
                Gizmos.DrawLine(o + new Vector3(0f, 0.02f, z), o + new Vector3(width, 0.02f, z));
            }
        }
    }
}
