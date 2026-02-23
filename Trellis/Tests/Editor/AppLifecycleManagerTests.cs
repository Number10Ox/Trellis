using NUnit.Framework;
using Trellis.App;
using Trellis.Events;

public class AppLifecycleManagerTests
{
    private EventBus eventBus;
    private AppLifecycleManager manager;

    [SetUp]
    public void SetUp()
    {
        eventBus = new EventBus();
        manager = new AppLifecycleManager(eventBus);
    }

    [TearDown]
    public void TearDown()
    {
        manager.Dispose();
    }

    // --- Initial State ---

    [Test]
    public void InitialState_IsActive()
    {
        Assert.AreEqual(AppState.Active, manager.CurrentState);
        Assert.AreEqual(AppState.Active, manager.State.Value);
    }

    // --- Pause / Resume ---

    [Test]
    public void NotifyPause_True_SetsPausedState()
    {
        manager.NotifyPause(true);

        Assert.AreEqual(AppState.Paused, manager.CurrentState);
    }

    [Test]
    public void NotifyPause_False_SetsActiveState()
    {
        manager.NotifyPause(true);
        manager.NotifyPause(false);

        Assert.AreEqual(AppState.Active, manager.CurrentState);
    }

    [Test]
    public void NotifyPause_True_PublishesAppPausedEvent()
    {
        bool received = false;
        eventBus.Subscribe<AppPausedEvent>(e => received = true);

        manager.NotifyPause(true);

        Assert.IsTrue(received);
    }

    [Test]
    public void NotifyPause_False_PublishesAppResumedEvent()
    {
        bool received = false;
        eventBus.Subscribe<AppResumedEvent>(e => received = true);

        manager.NotifyPause(true);
        manager.NotifyPause(false);

        Assert.IsTrue(received);
    }

    [Test]
    public void NotifyPause_True_NotifiesLifecycleAwareSystems()
    {
        var aware = new TestLifecycleAware();
        manager.Register(aware);

        manager.NotifyPause(true);

        Assert.IsTrue(aware.PauseCalled);
        Assert.IsFalse(aware.ResumeCalled);
    }

    [Test]
    public void NotifyPause_False_NotifiesLifecycleAwareSystems()
    {
        var aware = new TestLifecycleAware();
        manager.Register(aware);

        manager.NotifyPause(true);
        manager.NotifyPause(false);

        Assert.IsTrue(aware.ResumeCalled);
    }

    // --- Focus ---

    [Test]
    public void NotifyFocus_LostWhileActive_SetsUnfocusedState()
    {
        manager.NotifyFocus(false);

        Assert.AreEqual(AppState.Unfocused, manager.CurrentState);
    }

    [Test]
    public void NotifyFocus_GainedWhileUnfocused_SetsActiveState()
    {
        manager.NotifyFocus(false);
        manager.NotifyFocus(true);

        Assert.AreEqual(AppState.Active, manager.CurrentState);
    }

    [Test]
    public void NotifyFocus_Lost_PublishesFocusLostEvent()
    {
        bool received = false;
        eventBus.Subscribe<AppFocusLostEvent>(e => received = true);

        manager.NotifyFocus(false);

        Assert.IsTrue(received);
    }

    [Test]
    public void NotifyFocus_Gained_PublishesFocusGainedEvent()
    {
        bool received = false;
        eventBus.Subscribe<AppFocusGainedEvent>(e => received = true);

        manager.NotifyFocus(false);
        manager.NotifyFocus(true);

        Assert.IsTrue(received);
    }

    [Test]
    public void NotifyFocus_LostWhilePaused_DoesNotChangeState()
    {
        manager.NotifyPause(true);
        Assert.AreEqual(AppState.Paused, manager.CurrentState);

        manager.NotifyFocus(false);

        Assert.AreEqual(AppState.Paused, manager.CurrentState);
    }

    [Test]
    public void NotifyFocus_GainedWhileActive_NoEvent()
    {
        bool received = false;
        eventBus.Subscribe<AppFocusGainedEvent>(e => received = true);

        // Already active, gaining focus should be a no-op
        manager.NotifyFocus(true);

        Assert.IsFalse(received);
    }

    // --- Quit ---

    [Test]
    public void NotifyQuit_SetsQuittingState()
    {
        manager.NotifyQuit();

        Assert.AreEqual(AppState.Quitting, manager.CurrentState);
    }

    [Test]
    public void NotifyQuit_PublishesQuittingEvent()
    {
        bool received = false;
        eventBus.Subscribe<AppQuittingEvent>(e => received = true);

        manager.NotifyQuit();

        Assert.IsTrue(received);
    }

    // --- Observable Notification ---

    [Test]
    public void State_Observable_NotifiesOnChange()
    {
        AppState notified = AppState.Active;
        manager.State.Subscribe(s => notified = s);

        manager.NotifyPause(true);

        Assert.AreEqual(AppState.Paused, notified);
    }

    [Test]
    public void State_Observable_FullCycle()
    {
        var states = new System.Collections.Generic.List<AppState>();
        manager.State.Subscribe(s => states.Add(s));

        manager.NotifyPause(true);
        manager.NotifyPause(false);
        manager.NotifyFocus(false);
        manager.NotifyFocus(true);
        manager.NotifyQuit();

        Assert.AreEqual(5, states.Count);
        Assert.AreEqual(AppState.Paused, states[0]);
        Assert.AreEqual(AppState.Active, states[1]);
        Assert.AreEqual(AppState.Unfocused, states[2]);
        Assert.AreEqual(AppState.Active, states[3]);
        Assert.AreEqual(AppState.Quitting, states[4]);
    }

    // --- Register / Unregister ---

    [Test]
    public void Register_NullSystem_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => manager.Register(null));
    }

    [Test]
    public void Unregister_StopsNotifications()
    {
        var aware = new TestLifecycleAware();
        manager.Register(aware);
        manager.Unregister(aware);

        manager.NotifyPause(true);

        Assert.IsFalse(aware.PauseCalled);
    }

    // --- Dispose ---

    [Test]
    public void Dispose_StopsAllNotifications()
    {
        bool received = false;
        eventBus.Subscribe<AppPausedEvent>(e => received = true);

        manager.Dispose();
        manager.NotifyPause(true);

        Assert.IsFalse(received);
    }

    // --- Constructor Validation ---

    [Test]
    public void Constructor_NullEventBus_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => new AppLifecycleManager(null));
    }

    // --- Test Helpers ---

    private class TestLifecycleAware : IAppLifecycleAware
    {
        public bool PauseCalled;
        public bool ResumeCalled;

        public void OnAppPause()
        {
            PauseCalled = true;
        }

        public void OnAppResume()
        {
            ResumeCalled = true;
        }
    }
}
