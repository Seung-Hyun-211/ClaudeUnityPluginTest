using UnityEngine;
using UnityEngine.UI;
using Game.UI.Windows;

namespace Game.HUD
{
    /// <summary>
    /// Bottom-center hotbar layout: one row during normal play, two rows
    /// while the inventory screen is open (more room for comfortably
    /// dragging items onto slots). The weapon slots (1/2/3,
    /// WeaponLoadoutBarUIView) and quickslot bank (4-9,0, QuickSlotBarUIView)
    /// are always visible here — unlike the rest of the HUD, this bar is not
    /// hidden by HudRootView when a full-screen window opens.
    /// </summary>
    public class HotbarLayoutController : MonoBehaviour
    {
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private string inventoryWindowId = "inventory";
        [SerializeField] private HorizontalLayoutGroup compactLayout;
        [SerializeField] private GridLayoutGroup expandedLayout;

        private void OnEnable()
        {
            windowManager.FullScreenChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            windowManager.FullScreenChanged -= Refresh;
        }

        private void Refresh()
        {
            bool expanded = windowManager.CurrentFullScreenId == inventoryWindowId;
            compactLayout.enabled = !expanded;
            expandedLayout.enabled = expanded;
        }
    }
}
