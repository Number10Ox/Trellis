using System;
using System.Collections.Generic;
using Trellis.Events;
using Trellis.Reactive;

namespace Trellis.Scenes
{
    /// <summary>
    /// Orchestrates scene loading with progress tracking and event dispatch.
    /// Actual scene loading is delegated to <see cref="ISceneLoadProvider"/>.
    /// Progress is tracked via an observable float (0.0 to 1.0).
    /// </summary>
    public class SceneLoader
    {
        private readonly ISceneLoadProvider provider;
        private readonly EventBus eventBus;
        private readonly List<ISceneLoadHandler> handlers = new();
        private readonly Observable<float> loadProgress;
        private readonly List<string> loadedScenes = new();
        private bool isLoading;

        /// <summary>
        /// Observable load progress (0.0 to 1.0). Resets to 0 on new load, reaches 1.0 on complete.
        /// </summary>
        public ReadOnlyObservable<float> LoadProgress { get; }

        /// <summary>
        /// True if a scene load/transition is currently in progress.
        /// </summary>
        public bool IsLoading => isLoading;

        /// <summary>
        /// Number of scenes currently tracked as loaded.
        /// </summary>
        public int LoadedSceneCount => loadedScenes.Count;

        public SceneLoader(ISceneLoadProvider provider, EventBus eventBus)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            loadProgress = new Observable<float>(0f);
            LoadProgress = new ReadOnlyObservable<float>(loadProgress);
        }

        /// <summary>
        /// Registers a handler to receive scene load/unload callbacks.
        /// </summary>
        public void AddHandler(ISceneLoadHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            handlers.Add(handler);
        }

        /// <summary>
        /// Unregisters a scene load handler.
        /// </summary>
        public void RemoveHandler(ISceneLoadHandler handler)
        {
            handlers.Remove(handler);
        }

        /// <summary>
        /// Loads a single scene additively.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new ArgumentException("Scene name cannot be null or empty.", nameof(sceneName));
            }

            if (isLoading)
            {
                throw new InvalidOperationException("A scene load operation is already in progress.");
            }

            isLoading = true;
            loadProgress.Value = 0f;

            provider.LoadSceneAsync(
                sceneName,
                progress => loadProgress.Value = progress,
                () => OnSceneLoaded(sceneName)
            );
        }

        /// <summary>
        /// Unloads a previously loaded scene.
        /// </summary>
        public void UnloadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new ArgumentException("Scene name cannot be null or empty.", nameof(sceneName));
            }

            NotifyHandlersUnloading(sceneName);

            provider.UnloadSceneAsync(
                sceneName,
                () => OnSceneUnloaded(sceneName)
            );
        }

        /// <summary>
        /// Executes a scene transition: unloads specified scenes, then loads new ones.
        /// </summary>
        public void ExecuteTransition(SceneTransition transition)
        {
            if (transition == null)
            {
                throw new ArgumentNullException(nameof(transition));
            }

            if (isLoading)
            {
                throw new InvalidOperationException("A scene load operation is already in progress.");
            }

            isLoading = true;
            loadProgress.Value = 0f;

            int unloadCount = transition.ScenesToUnload.Length;
            int unloadsCompleted = 0;

            if (unloadCount == 0)
            {
                BeginLoadPhase(transition);
                return;
            }

            for (int i = 0; i < unloadCount; i++)
            {
                string sceneName = transition.ScenesToUnload[i];
                NotifyHandlersUnloading(sceneName);

                provider.UnloadSceneAsync(sceneName, () =>
                {
                    OnSceneUnloaded(sceneName);
                    unloadsCompleted++;

                    if (unloadsCompleted >= unloadCount)
                    {
                        BeginLoadPhase(transition);
                    }
                });
            }
        }

        /// <summary>
        /// Returns true if the given scene is tracked as loaded.
        /// </summary>
        public bool IsSceneLoaded(string sceneName)
        {
            return loadedScenes.Contains(sceneName);
        }

        private void BeginLoadPhase(SceneTransition transition)
        {
            int loadCount = transition.ScenesToLoad.Length;
            if (loadCount == 0)
            {
                isLoading = false;
                loadProgress.Value = 1f;
                return;
            }

            int loadsCompleted = 0;
            float perSceneWeight = 1f / loadCount;

            for (int i = 0; i < loadCount; i++)
            {
                int sceneIndex = i;
                string sceneName = transition.ScenesToLoad[i];

                provider.LoadSceneAsync(
                    sceneName,
                    progress =>
                    {
                        float baseProgress = sceneIndex * perSceneWeight;
                        loadProgress.Value = baseProgress + (progress * perSceneWeight);
                    },
                    () =>
                    {
                        OnSceneLoaded(sceneName);
                        loadsCompleted++;

                        if (loadsCompleted >= loadCount)
                        {
                            isLoading = false;
                            loadProgress.Value = 1f;
                        }
                    }
                );
            }
        }

        private void OnSceneLoaded(string sceneName)
        {
            if (!loadedScenes.Contains(sceneName))
            {
                loadedScenes.Add(sceneName);
            }

            for (int i = 0; i < handlers.Count; i++)
            {
                handlers[i].OnSceneLoaded(sceneName);
            }

            eventBus.Publish(new SceneLoadedEvent { SceneName = sceneName });

            if (!isLoading || loadProgress.Value >= 1f)
            {
                isLoading = false;
                loadProgress.Value = 1f;
            }
        }

        private void OnSceneUnloaded(string sceneName)
        {
            loadedScenes.Remove(sceneName);

            eventBus.Publish(new SceneUnloadedEvent { SceneName = sceneName });
        }

        private void NotifyHandlersUnloading(string sceneName)
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                handlers[i].OnSceneUnloading(sceneName);
            }
        }
    }

    /// <summary>
    /// Published when a scene has been loaded.
    /// </summary>
    public struct SceneLoadedEvent
    {
        public string SceneName;
    }

    /// <summary>
    /// Published when a scene has been unloaded.
    /// </summary>
    public struct SceneUnloadedEvent
    {
        public string SceneName;
    }
}
