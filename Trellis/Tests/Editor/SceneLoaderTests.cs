using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Events;
using Trellis.Scenes;

public class SceneTransitionTests
{
    [Test]
    public void Constructor_SetsScenesToLoad()
    {
        var transition = new SceneTransition(new[] { "Gameplay" });

        Assert.AreEqual(1, transition.ScenesToLoad.Length);
        Assert.AreEqual("Gameplay", transition.ScenesToLoad[0]);
    }

    [Test]
    public void Constructor_NullScenesToLoad_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SceneTransition(null));
    }

    [Test]
    public void Constructor_NullUnload_DefaultsToEmpty()
    {
        var transition = new SceneTransition(new[] { "Gameplay" });

        Assert.AreEqual(0, transition.ScenesToUnload.Length);
    }

    [Test]
    public void Constructor_WithUnload()
    {
        var transition = new SceneTransition(
            new[] { "Gameplay" },
            new[] { "Menu" }
        );

        Assert.AreEqual(1, transition.ScenesToUnload.Length);
        Assert.AreEqual("Menu", transition.ScenesToUnload[0]);
    }

    [Test]
    public void Load_CreatesLoadOnlyTransition()
    {
        var transition = SceneTransition.Load("Gameplay");

        Assert.AreEqual(1, transition.ScenesToLoad.Length);
        Assert.AreEqual("Gameplay", transition.ScenesToLoad[0]);
        Assert.AreEqual(0, transition.ScenesToUnload.Length);
    }

    [Test]
    public void Switch_CreatesUnloadAndLoadTransition()
    {
        var transition = SceneTransition.Switch("Menu", "Gameplay");

        Assert.AreEqual(1, transition.ScenesToLoad.Length);
        Assert.AreEqual("Gameplay", transition.ScenesToLoad[0]);
        Assert.AreEqual(1, transition.ScenesToUnload.Length);
        Assert.AreEqual("Menu", transition.ScenesToUnload[0]);
    }
}

public class SceneLoaderTests
{
    private MockSceneLoadProvider provider;
    private EventBus eventBus;
    private SceneLoader loader;

    [SetUp]
    public void SetUp()
    {
        provider = new MockSceneLoadProvider();
        eventBus = new EventBus();
        loader = new SceneLoader(provider, eventBus);
    }

    // --- Constructor ---

    [Test]
    public void Constructor_NullProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SceneLoader(null, eventBus));
    }

    [Test]
    public void Constructor_NullEventBus_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SceneLoader(provider, null));
    }

    // --- LoadScene ---

    [Test]
    public void LoadScene_CallsProvider()
    {
        loader.LoadScene("Gameplay");

        Assert.AreEqual("Gameplay", provider.LastLoadedScene);
    }

    [Test]
    public void LoadScene_SetsIsLoading()
    {
        provider.CompleteImmediately = false;
        loader.LoadScene("Gameplay");

        Assert.IsTrue(loader.IsLoading);
    }

    [Test]
    public void LoadScene_CompletedSetsIsLoadingFalse()
    {
        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        Assert.IsFalse(loader.IsLoading);
    }

    [Test]
    public void LoadScene_NullName_Throws()
    {
        Assert.Throws<ArgumentException>(() => loader.LoadScene(null));
    }

    [Test]
    public void LoadScene_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => loader.LoadScene(""));
    }

    [Test]
    public void LoadScene_WhileLoading_Throws()
    {
        provider.CompleteImmediately = false;
        loader.LoadScene("Gameplay");

        Assert.Throws<InvalidOperationException>(() => loader.LoadScene("Menu"));
    }

    [Test]
    public void LoadScene_PublishesSceneLoadedEvent()
    {
        string loadedScene = null;
        eventBus.Subscribe<SceneLoadedEvent>(e => loadedScene = e.SceneName);

        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        Assert.AreEqual("Gameplay", loadedScene);
    }

    [Test]
    public void LoadScene_TracksLoadedScene()
    {
        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        Assert.IsTrue(loader.IsSceneLoaded("Gameplay"));
        Assert.AreEqual(1, loader.LoadedSceneCount);
    }

    [Test]
    public void LoadScene_ProgressReported()
    {
        provider.CompleteImmediately = false;
        loader.LoadScene("Gameplay");

        float progress = -1f;
        loader.LoadProgress.Subscribe(p => progress = p);

        provider.ReportProgress(0.5f);

        Assert.AreEqual(0.5f, progress, 0.001f);
    }

    [Test]
    public void LoadScene_NotifiesHandlers()
    {
        var handler = new TestSceneHandler();
        loader.AddHandler(handler);

        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        Assert.AreEqual("Gameplay", handler.LastLoaded);
    }

    // --- UnloadScene ---

    [Test]
    public void UnloadScene_CallsProvider()
    {
        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        loader.UnloadScene("Gameplay");

        Assert.AreEqual("Gameplay", provider.LastUnloadedScene);
    }

    [Test]
    public void UnloadScene_PublishesEvent()
    {
        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        string unloaded = null;
        eventBus.Subscribe<SceneUnloadedEvent>(e => unloaded = e.SceneName);

        loader.UnloadScene("Gameplay");

        Assert.AreEqual("Gameplay", unloaded);
    }

    [Test]
    public void UnloadScene_RemovesFromTracked()
    {
        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        loader.UnloadScene("Gameplay");

        Assert.IsFalse(loader.IsSceneLoaded("Gameplay"));
    }

    [Test]
    public void UnloadScene_NotifiesHandlersBeforeUnload()
    {
        var handler = new TestSceneHandler();
        loader.AddHandler(handler);

        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        loader.UnloadScene("Gameplay");

        Assert.AreEqual("Gameplay", handler.LastUnloading);
    }

    [Test]
    public void UnloadScene_NullName_Throws()
    {
        Assert.Throws<ArgumentException>(() => loader.UnloadScene(null));
    }

    // --- ExecuteTransition ---

    [Test]
    public void ExecuteTransition_LoadOnly()
    {
        provider.CompleteImmediately = true;
        var transition = SceneTransition.Load("Gameplay");

        loader.ExecuteTransition(transition);

        Assert.IsTrue(loader.IsSceneLoaded("Gameplay"));
        Assert.IsFalse(loader.IsLoading);
    }

    [Test]
    public void ExecuteTransition_Switch()
    {
        provider.CompleteImmediately = true;
        loader.LoadScene("Menu");

        var transition = SceneTransition.Switch("Menu", "Gameplay");
        loader.ExecuteTransition(transition);

        Assert.IsFalse(loader.IsSceneLoaded("Menu"));
        Assert.IsTrue(loader.IsSceneLoaded("Gameplay"));
    }

    [Test]
    public void ExecuteTransition_NullTransition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => loader.ExecuteTransition(null));
    }

    [Test]
    public void ExecuteTransition_WhileLoading_Throws()
    {
        provider.CompleteImmediately = false;
        loader.LoadScene("Gameplay");

        Assert.Throws<InvalidOperationException>(() =>
            loader.ExecuteTransition(SceneTransition.Load("Menu")));
    }

    // --- Handler Management ---

    [Test]
    public void AddHandler_NullHandler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => loader.AddHandler(null));
    }

    [Test]
    public void RemoveHandler_StopsNotifications()
    {
        var handler = new TestSceneHandler();
        loader.AddHandler(handler);
        loader.RemoveHandler(handler);

        provider.CompleteImmediately = true;
        loader.LoadScene("Gameplay");

        Assert.IsNull(handler.LastLoaded);
    }

    // --- Test Helpers ---

    private class MockSceneLoadProvider : ISceneLoadProvider
    {
        public string LastLoadedScene;
        public string LastUnloadedScene;
        public bool CompleteImmediately = true;
        private Action<float> pendingProgressCallback;
        private Action pendingCompleteCallback;

        public void LoadSceneAsync(string sceneName, Action<float> onProgress, Action onComplete)
        {
            LastLoadedScene = sceneName;

            if (CompleteImmediately)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke();
            }
            else
            {
                pendingProgressCallback = onProgress;
                pendingCompleteCallback = onComplete;
            }
        }

        public void UnloadSceneAsync(string sceneName, Action onComplete)
        {
            LastUnloadedScene = sceneName;
            onComplete?.Invoke();
        }

        public bool IsSceneLoaded(string sceneName)
        {
            return false;
        }

        public void ReportProgress(float progress)
        {
            pendingProgressCallback?.Invoke(progress);
        }

        public void Complete()
        {
            pendingCompleteCallback?.Invoke();
        }
    }

    private class TestSceneHandler : ISceneLoadHandler
    {
        public string LastLoaded;
        public string LastUnloading;

        public void OnSceneLoaded(string sceneName)
        {
            LastLoaded = sceneName;
        }

        public void OnSceneUnloading(string sceneName)
        {
            LastUnloading = sceneName;
        }
    }
}
