using UnityEngine;

namespace Game.HUD.Markers
{
    /// <summary>Minimal contract for anything the compass/minimap can display (documents/hud-system.md).</summary>
    public interface IWorldMarker
    {
        Vector3 WorldPosition { get; }
        Sprite Icon { get; }
        string Label { get; }
        MarkerCategory Category { get; }
    }
}
