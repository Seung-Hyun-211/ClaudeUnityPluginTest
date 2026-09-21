using System;
using UnityEngine;
using Game.Items;

namespace Game.Building
{
    /// <summary>The five categories the player picks with keys 1-5 (stairs and ladders arrive in stage 2).</summary>
    public enum BuildCategory
    {
        Wall = 1,
        Floor = 2,
        Stairs = 3,
        Ladder = 4,
        Door = 5,
    }

    [Serializable]
    public struct BuildCost
    {
        public ItemData item;
        [Min(1)] public int count;
    }

    /// <summary>One kind of building piece: what it looks like, how tough it is and what it costs.</summary>
    [CreateAssetMenu(menuName = "Game/Building/Piece", fileName = "BuildPiece")]
    public class BuildPieceData : ScriptableObject
    {
        [SerializeField] private string pieceId;
        [SerializeField] private string displayName;
        [SerializeField] private PieceKind kind;

        [Tooltip("Authored at the piece's real size (see BuildGrid.Size), wall and door width along X, with a BuildPiece and a HealthComponent on the root.")]
        [SerializeField] private GameObject prefab;

        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private BuildCost[] cost = Array.Empty<BuildCost>();

        [Tooltip("Share of the cost handed back when the player demolishes it (0-1). Pieces that are destroyed give nothing back.")]
        [SerializeField, Range(0f, 1f)] private float refundRatio = 0.5f;

        public string PieceId => pieceId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public PieceKind Kind => kind;
        public GameObject Prefab => prefab;
        public float MaxHealth => maxHealth;
        public BuildCost[] Cost => cost;
        public float RefundRatio => refundRatio;

        /// <summary>How many of a cost entry come back on demolition (rounded down).</summary>
        public int RefundOf(BuildCost entry) => Mathf.FloorToInt(entry.count * refundRatio);
    }
}
