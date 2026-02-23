namespace Trellis.Scenes
{
    /// <summary>
    /// Interface for systems that need to react to scene load/unload events.
    /// Can be used for per-scene setup and teardown (e.g., initializing scene-specific systems).
    /// </summary>
    public interface ISceneLoadHandler
    {
        /// <summary>
        /// Called after a scene has been loaded.
        /// </summary>
        void OnSceneLoaded(string sceneName);

        /// <summary>
        /// Called before a scene is unloaded.
        /// </summary>
        void OnSceneUnloading(string sceneName);
    }
}
