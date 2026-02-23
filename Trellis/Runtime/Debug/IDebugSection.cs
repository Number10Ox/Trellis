namespace Trellis.Debugging
{
    /// <summary>
    /// Interface for systems that provide a section in the debug overlay.
    /// Each section provides a title and content string for display.
    /// </summary>
    public interface IDebugSection
    {
        /// <summary>
        /// Display name for this section (used as tab/header label).
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Returns the current debug content as a formatted string.
        /// Called each time the debug overlay refreshes.
        /// </summary>
        string Content();

        /// <summary>
        /// Returns true if this section has content to display.
        /// Sections with no content can be hidden from the overlay.
        /// </summary>
        bool IsActive { get; }
    }
}
