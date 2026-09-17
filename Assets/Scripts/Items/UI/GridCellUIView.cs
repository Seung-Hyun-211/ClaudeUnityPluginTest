using UnityEngine;

namespace Game.Items.UI
{
    /// <summary>Visual background for a single usable grid cell.</summary>
    public class GridCellUIView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        public RectTransform RectTransform => rectTransform != null ? rectTransform : (RectTransform)transform;
    }
}
