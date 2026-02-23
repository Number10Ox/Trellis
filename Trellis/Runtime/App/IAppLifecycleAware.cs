namespace Trellis.App
{
    /// <summary>
    /// Interface for systems that need to respond to application lifecycle events.
    /// Implement this on systems that need pause/resume hooks.
    /// </summary>
    public interface IAppLifecycleAware
    {
        /// <summary>
        /// Called when the application is paused (background, lost focus).
        /// </summary>
        void OnAppPause();

        /// <summary>
        /// Called when the application resumes from a paused state.
        /// </summary>
        void OnAppResume();
    }
}
