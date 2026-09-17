using System;

namespace Game.UI.Windows
{
    /// <summary>
    /// Whatever a PopupWindow displays — a puzzle, a story event, a plain
    /// confirmation prompt. The popup shell only needs this much to host it
    /// and knows nothing about which kind it is.
    /// </summary>
    public interface IPopupContent
    {
        /// <summary>Called once the popup becomes visible, to start/reset the content.</summary>
        void Present();

        /// <summary>Raised when the content is done (solved, dismissed, chosen) so the popup can close.</summary>
        event Action Finished;
    }
}
