using UnityEngine;

namespace Game.UI.Windows
{
    /// <summary>
    /// Convenience entry point: instantiate a popup shell + a content prefab,
    /// wire them together, and push onto WindowManager. Works for any
    /// IPopupContent without change — puzzles and events use the same call.
    /// </summary>
    public class PopupLauncher : MonoBehaviour
    {
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private PopupWindow popupWindowPrefab;
        [SerializeField] private Transform popupLayer;

        public PopupWindow Launch<TContent>(TContent contentPrefab) where TContent : MonoBehaviour, IPopupContent
        {
            var popup = Instantiate(popupWindowPrefab, popupLayer);
            var content = Instantiate(contentPrefab, popup.ContentSlot);
            popup.SetContent(content);
            windowManager.PushPopup(popup);
            return popup;
        }
    }
}
