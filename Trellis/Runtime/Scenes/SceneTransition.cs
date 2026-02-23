using System;

namespace Trellis.Scenes
{
    /// <summary>
    /// Describes a scene transition: which scenes to unload and which to load.
    /// Used by <see cref="SceneLoader"/> to execute multi-scene transitions.
    /// </summary>
    public class SceneTransition
    {
        /// <summary>
        /// Scenes to unload before loading new scenes. Can be empty.
        /// </summary>
        public string[] ScenesToUnload { get; }

        /// <summary>
        /// Scenes to load additively. At least one scene must be specified.
        /// </summary>
        public string[] ScenesToLoad { get; }

        public SceneTransition(string[] scenesToLoad, string[] scenesToUnload = null)
        {
            ScenesToLoad = scenesToLoad ?? throw new ArgumentNullException(nameof(scenesToLoad));
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
        }

        /// <summary>
        /// Creates a simple transition that loads a single scene with no unloads.
        /// </summary>
        public static SceneTransition Load(string sceneName)
        {
            return new SceneTransition(new[] { sceneName });
        }

        /// <summary>
        /// Creates a transition that unloads one scene and loads another.
        /// </summary>
        public static SceneTransition Switch(string unload, string load)
        {
            return new SceneTransition(new[] { load }, new[] { unload });
        }
    }
}
