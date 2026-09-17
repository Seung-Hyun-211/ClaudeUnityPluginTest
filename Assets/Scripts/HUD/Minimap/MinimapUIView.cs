using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.HUD.Markers;

namespace Game.HUD.Minimap
{
    /// <summary>
    /// Displays MinimapCameraRig's RenderTexture and overlays WorldMarkerRegistry
    /// icons as screen-space offsets around the always-centered player arrow.
    /// Reuses the same WorldMarkerRegistry as CompassUIView so a new marker
    /// appears on both without either view changing (documents/hud-system.md).
    /// </summary>
    public class MinimapUIView : MonoBehaviour
    {
        [SerializeField] private RawImage mapImage;
        [SerializeField] private MinimapCameraRig cameraRig;
        [SerializeField] private WorldMarkerRegistry markerRegistry;
        [SerializeField] private Transform followTarget;
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private RectTransform playerArrow;
        [SerializeField] private MinimapMarkerIconView markerIconPrefab;
        [SerializeField] private float worldUnitsToPixels = 4f;
        [SerializeField] private float mapRadiusWorldUnits = 40f;

        private readonly List<MinimapMarkerIconView> spawnedMarkerViews = new();

        private void OnEnable()
        {
            mapImage.texture = cameraRig.Output;
            markerRegistry.Changed += RebuildMarkerViews;
            RebuildMarkerViews();
        }

        private void OnDisable()
        {
            markerRegistry.Changed -= RebuildMarkerViews;
        }

        private void Update()
        {
            float cameraYaw = cameraRig.CurrentYaw;
            float headingDegrees = followTarget.eulerAngles.y - cameraYaw;
            playerArrow.localRotation = Quaternion.Euler(0f, 0f, -headingDegrees);

            Vector3 origin = followTarget.position;
            var markers = markerRegistry.Markers;
            float radiusPixels = mapRadiusWorldUnits * worldUnitsToPixels;

            for (int i = 0; i < markers.Count; i++)
            {
                Vector3 offset = markers[i].WorldPosition - origin;
                Vector3 rotated = Quaternion.Euler(0f, -cameraYaw, 0f) * offset;
                Vector2 screenOffset = new Vector2(rotated.x, rotated.z) * worldUnitsToPixels;

                bool visible = screenOffset.magnitude <= radiusPixels;
                spawnedMarkerViews[i].SetVisible(visible);
                if (visible)
                {
                    spawnedMarkerViews[i].SetAnchoredPosition(screenOffset);
                }
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
                var view = Instantiate(markerIconPrefab, iconContainer);
                view.Bind(marker);
                spawnedMarkerViews.Add(view);
            }
        }
    }
}
