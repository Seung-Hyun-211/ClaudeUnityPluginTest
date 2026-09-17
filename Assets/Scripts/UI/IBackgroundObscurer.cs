namespace Game.UI
{
    /// <summary>
    /// Hides whatever is behind a window. Game.UI.Windows.WindowManager only
    /// depends on this, so the placeholder darken panel can be swapped for a
    /// real blur render feature later with no change to window logic.
    /// </summary>
    public interface IBackgroundObscurer
    {
        void SetActive(bool active);
    }
}
