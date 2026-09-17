using System;

namespace Game.UI.Windows
{
    /// <summary>
    /// Anything WindowManager can show/hide — a full-screen screen (inventory,
    /// settings) or a popup (puzzle, event prompt). The manager only ever
    /// talks to this; it never knows what a window actually contains.
    /// </summary>
    public interface IWindow
    {
        bool IsVisible { get; }

        /// <summary>Raised when the window itself wants to close (close button, Escape, puzzle solved, ...).</summary>
        event Action CloseRequested;

        void Show();
        void Hide();
    }
}
