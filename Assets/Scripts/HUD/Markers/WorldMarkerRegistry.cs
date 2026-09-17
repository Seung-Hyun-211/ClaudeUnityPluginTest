using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.HUD.Markers
{
    /// <summary>
    /// Scene-wide registry shared by CompassUIView and MinimapUIView so both
    /// draw the same set of markers (documents/hud-system.md). A plain
    /// MonoBehaviour wired in via SerializeField — the same
    /// dependency-injection idiom WindowManager uses elsewhere — rather than a
    /// static singleton, so callers never rely on hidden global state.
    /// </summary>
    public class WorldMarkerRegistry : MonoBehaviour
    {
        private readonly List<IWorldMarker> markers = new();

        public IReadOnlyList<IWorldMarker> Markers => markers;
        public event Action Changed;

        public void Register(IWorldMarker marker)
        {
            if (markers.Contains(marker))
            {
                return;
            }

            markers.Add(marker);
            Changed?.Invoke();
        }

        public void Unregister(IWorldMarker marker)
        {
            if (markers.Remove(marker))
            {
                Changed?.Invoke();
            }
        }
    }
}
