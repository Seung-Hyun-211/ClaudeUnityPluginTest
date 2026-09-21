using UnityEngine;

namespace Game.Building
{
    /// <summary>Where the player is pointing when placing or demolishing. The camera/view system is not built yet, so this is swappable.</summary>
    public interface IAimSource
    {
        bool TryGetRay(out Ray ray);
    }
}
