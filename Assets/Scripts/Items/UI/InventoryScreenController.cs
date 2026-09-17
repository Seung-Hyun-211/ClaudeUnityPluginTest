using Game.UI.Windows;

namespace Game.Items.UI
{
    /// <summary>
    /// The inventory's full-screen panel. Exclusivity with other full-screen
    /// windows and the background obscurer are handled by WindowManager, not
    /// here — this class only knows how to show/hide its own root.
    /// </summary>
    public class InventoryScreenController : SimpleWindow
    {
    }
}
