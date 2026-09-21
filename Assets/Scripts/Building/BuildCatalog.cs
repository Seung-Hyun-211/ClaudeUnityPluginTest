using UnityEngine;

namespace Game.Building
{
    /// <summary>Which BuildPieceData each category and piece kind uses. Categories without data (stairs, ladder in stage 1) are unavailable.</summary>
    [CreateAssetMenu(menuName = "Game/Building/Catalog", fileName = "BuildCatalog")]
    public class BuildCatalog : ScriptableObject
    {
        [SerializeField] private BuildPieceData wall;
        [SerializeField] private BuildPieceData floor;
        [SerializeField] private BuildPieceData door;

        [Tooltip("The corner post made automatically where walls meet.")]
        [SerializeField] private BuildPieceData pillar;

        /// <summary>The piece for a category, or null when that category is not available yet.</summary>
        public BuildPieceData Get(BuildCategory category)
        {
            switch (category)
            {
                case BuildCategory.Wall:
                    return wall;
                case BuildCategory.Floor:
                    return floor;
                case BuildCategory.Door:
                    return door;
                default:
                    return null;
            }
        }

        public bool IsAvailable(BuildCategory category) => Get(category) != null;

        public BuildPieceData ForKind(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Wall:
                    return wall;
                case PieceKind.Floor:
                    return floor;
                case PieceKind.Door:
                    return door;
                default:
                    return pillar;
            }
        }

        public static PieceKind KindOf(BuildCategory category)
        {
            switch (category)
            {
                case BuildCategory.Floor:
                    return PieceKind.Floor;
                case BuildCategory.Door:
                    return PieceKind.Door;
                default:
                    return PieceKind.Wall;
            }
        }

        public BuildPieceData Find(string pieceId)
        {
            foreach (var data in new[] { wall, floor, door, pillar })
            {
                if (data != null && data.PieceId == pieceId)
                {
                    return data;
                }
            }

            return null;
        }
    }
}
