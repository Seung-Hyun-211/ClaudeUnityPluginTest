using System.Collections.Generic;
using UnityEngine;
using Game.HUD.Markers;

namespace Game.HUD.Compass
{
    /// <summary>
    /// Horizontal strip showing N/E/S/W and nearby WorldMarkerRegistry entries,
    /// positioned by followTarget's yaw rather than the camera so it stays
    /// correct across first/third-person camera switches. followTarget is a
    /// plain Transform (not a character controller type) so this view stays
    /// decoupled from the in-progress Characters track — drop the player's
    /// transform in once it exists (documents/hud-system.md).
    /// </summary>
    public class CompassUIView : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private WorldMarkerRegistry markerRegistry;
        [SerializeField] private RectTransform stripContent;
        [SerializeField] private CompassMarkerIconView markerIconPrefab;
        [SerializeField] private RectTransform northLabel;
        [SerializeField] private RectTransform eastLabel;
        [SerializeField] private RectTransform southLabel;
        [SerializeField] private RectTransform westLabel;
        [SerializeField] private float fieldOfViewDegrees = 90f;
        [SerializeField] private float pixelsPerDegree = 8f;

        private readonly List<CompassMarkerIconView> spawnedMarkerViews = new();
        private (RectTransform rect, float worldAngle)[] cardinals;

        private void Awake()
        {
            cardinals = new[]
            {
                (northLabel, 0f),
                (eastLabel, 90f),
                (southLabel, 180f),
                (westLabel, 270f)
            };
        }

        private void OnEnable()
        {
            markerRegistry.Changed += RebuildMarkerViews;
            RebuildMarkerViews();
        }

        private void OnDisable()
        {
            markerRegistry.Changed -= RebuildMarkerViews;
        }

        private void Update()
        {
            float yaw = followTarget.eulerAngles.y;

            foreach (var cardinal in cardinals)
            {
                PositionOnStrip(cardinal.rect, cardinal.worldAngle, yaw);
            }

            Vector3 origin = followTarget.position;
            var markers = markerRegistry.Markers;
            for (int i = 0; i < markers.Count; i++)
            {
                Vector3 offset = markers[i].WorldPosition - origin;
                float markerAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                float distance = new Vector2(offset.x, offset.z).magnitude;
                PositionMarker(spawnedMarkerViews[i], markerAngle, yaw, distance);
            }
        }

        private void PositionOnStrip(RectTransform rect, float worldAngle, float yaw)
        {
            float relative = Mathf.DeltaAngle(yaw, worldAngle);
            bool visible = Mathf.Abs(relative) <= fieldOfViewDegrees * 0.5f;
            rect.gameObject.SetActive(visible);
            if (visible)
            {
                var position = rect.anchoredPosition;
                position.x = relative * pixelsPerDegree;
                rect.anchoredPosition = position;
            }
        }

        private void PositionMarker(CompassMarkerIconView view, float markerAngle, float yaw, float distance)
        {
            float relative = Mathf.DeltaAngle(yaw, markerAngle);
            bool visible = Mathf.Abs(relative) <= fieldOfViewDegrees * 0.5f;
            view.SetVisible(visible);
            if (visible)
            {
                view.SetStripPosition(relative * pixelsPerDegree, distance);
            }
        }

        private void RebuildMarkerViews()
        {
            foreach (var view in spawnedMarkerViews)
            {
                Destroy(view.gameObject);
            }
            spawnedMarkerViews.Clear();

            foreach (var marker in markerRegistry.Markers)
            {
                var view = Instantiate(markerIconPrefab, stripContent);
                view.Bind(marker);
                spawnedMarkerViews.Add(view);
            }
        }
    }
}
