using UnityEngine;

namespace Game.HUD.Markers
{
    /// <summary>Drop onto any scene object to make it appear on the compass/minimap — no code required.</summary>
    public class WorldMarkerSource : MonoBehaviour, IWorldMarker
    {
        [SerializeField] private WorldMarkerRegistry registry;
        [SerializeField] private Sprite icon;
        [SerializeField] private string label;
        [SerializeField] private MarkerCategory category;

        public Vector3 WorldPosition => transform.position;
        public Sprite Icon => icon;
        public string Label => label;
        public MarkerCategory Category => category;

        private void OnEnable()
        {
            registry.Register(this);
        }

        private void OnDisable()
        {
            registry.Unregister(this);
        }
    }
}
