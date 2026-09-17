using UnityEngine;

namespace Game.Items
{
    [CreateAssetMenu(menuName = "Items/Item Data", fileName = "New Item")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject worldPrefab;
        [SerializeField] private ItemType itemType;
        [SerializeField, Min(1)] private int maxStackSize = 1;
        [SerializeField, Min(1)] private int gridWidth = 1;
        [SerializeField, Min(1)] private int gridHeight = 1;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject WorldPrefab => worldPrefab;
        public ItemType ItemType => itemType;
        public int MaxStackSize => maxStackSize;
        public bool IsStackable => maxStackSize > 1;

        /// <summary>Footprint this item occupies in a shape-based grid inventory.</summary>
        public Vector2Int GridSize => new(gridWidth, gridHeight);
    }
}
