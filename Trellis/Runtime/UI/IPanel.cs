namespace Trellis.UI
{
    /// <summary>
    /// Interface for UI panels managed by the <see cref="PanelManager"/>.
    /// Panels register with a zone and manage their own data binding.
    /// </summary>
    public interface IPanel
    {
        /// <summary>
        /// Unique identifier for this panel.
        /// </summary>
        string PanelId { get; }

        /// <summary>
        /// Shows the panel.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the panel.
        /// </summary>
        void Hide();

        /// <summary>
        /// Returns true if the panel is currently visible.
        /// </summary>
        bool IsVisible { get; }
    }
}
