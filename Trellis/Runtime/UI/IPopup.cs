namespace Trellis.UI
{
    /// <summary>
    /// Interface for popup implementations managed by <see cref="PopupManager"/>.
    /// </summary>
    public interface IPopup
    {
        /// <summary>
        /// Shows the popup.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the popup.
        /// </summary>
        void Hide();

        /// <summary>
        /// Returns true if the popup is currently visible.
        /// </summary>
        bool IsVisible { get; }
    }
}
