using UnityEngine;
using UnityEngine.UI;
using Game.HUD.Markers;

namespace Game.HUD.Minimap
{
    /// <summary>Single marker icon placed at a screen-space offset from the minimap center.</summary>
    public class MinimapMarkerIconView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image iconImage;

        public RectTransform RectTransform => rectTransform != null ? rectTransform : (RectTransform)transform;

        public void Bind(IWorldMarker marker)
        {
            iconImage.sprite = marker.Icon;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetAnchoredPosition(Vector2 position)
        {
            RectTransform.anchoredPosition = position;
        }
    }
}
