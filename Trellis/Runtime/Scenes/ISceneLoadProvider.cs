using System;

namespace Trellis.Scenes
{
    /// <summary>
    /// Abstraction over the actual scene loading mechanism.
    /// Default implementation wraps Unity's SceneManager. Consumers can provide
    /// test doubles or custom loaders (e.g., Addressables-based scene loading).
    /// </summary>
    public interface ISceneLoadProvider
    {
        /// <summary>
        /// Begins loading a scene additively.
        /// Progress is reported via the onProgress callback (0.0 to 1.0).
        /// onComplete is called when the scene is fully loaded.
        /// </summary>
        void LoadSceneAsync(string sceneName, Action<float> onProgress, Action onComplete);

        /// <summary>
        /// Begins unloading a scene.
        /// onComplete is called when the scene is fully unloaded.
        /// </summary>
        void UnloadSceneAsync(string sceneName, Action onComplete);

        /// <summary>
        /// Returns true if the scene is currently loaded.
        /// </summary>
        bool IsSceneLoaded(string sceneName);
    }
}
