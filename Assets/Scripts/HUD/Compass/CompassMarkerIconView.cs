using UnityEngine;
using UnityEngine.UI;
using Game.HUD.Markers;

namespace Game.HUD.Compass
{
    /// <summary>Single marker icon positioned along the compass strip.</summary>
    public class CompassMarkerIconView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text label;
        [SerializeField] private Text distanceLabel;

        public RectTransform RectTransform => rectTransform != null ? rectTransform : (RectTransform)transform;

        public void Bind(IWorldMarker marker)
        {
            iconImage.sprite = marker.Icon;
            label.text = marker.Label;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetStripPosition(float stripX, float distance)
        {
            var position = RectTransform.anchoredPosition;
            position.x = stripX;
            RectTransform.anchoredPosition = position;
            distanceLabel.text = $"{distance:0}m";
        }
    }
}
