using UnityEngine;
using Game.Combat;

namespace Game.Building
{
    /// <summary>
    /// Marks a spawned building piece: which grid slot and data it is, and
    /// its HealthComponent. Anything that hits a collider looks for this
    /// with GetComponentInParent to know it hit a piece.
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public class BuildPiece : MonoBehaviour
    {
        public PieceKey Key { get; private set; }
        public BuildPieceData Data { get; private set; }
        public HealthComponent Health { get; private set; }

        public void Initialize(PieceKey key, BuildPieceData data)
        {
            Key = key;
            Data = data;
            Health = GetComponent<HealthComponent>();
        }
    }
}
