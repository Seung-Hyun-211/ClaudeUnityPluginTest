using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Temporary stand-in for "blur the background": a full-screen darken panel
    /// GameObject that this component just toggles on/off.
    /// </summary>
    public class DarkenOverlayObscurer : MonoBehaviour, IBackgroundObscurer
    {
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
