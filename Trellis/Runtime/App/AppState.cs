namespace Trellis.App
{
    /// <summary>
    /// Application-level state values surfaced by <see cref="AppLifecycleManager"/>.
    /// </summary>
    public enum AppState
    {
        /// <summary>
        /// Application is running normally.
        /// </summary>
        Active,

        /// <summary>
        /// Application is paused (e.g., moved to background on mobile).
        /// </summary>
        Paused,

        /// <summary>
        /// Application has lost focus but is not paused.
        /// </summary>
        Unfocused,

        /// <summary>
        /// Application is shutting down.
        /// </summary>
        Quitting
    }
}
