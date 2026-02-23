using System;
using System.Collections.Generic;
using Trellis.Events;
using Trellis.Reactive;

namespace Trellis.App
{
    /// <summary>
    /// Application-level lifecycle events and state management.
    /// Wraps Unity callbacks (OnApplicationPause, OnApplicationFocus, OnApplicationQuit)
    /// and surfaces them as typed events on the EventBus and as Observable state.
    ///
    /// The MonoBehaviour that receives Unity callbacks should call the corresponding
    /// Notify methods on this class.
    /// </summary>
    public class AppLifecycleManager : IDisposable
    {
        private readonly EventBus eventBus;
        private readonly Observable<AppState> appState;
        private readonly List<IAppLifecycleAware> lifecycleAwareystems = new();
        private bool disposed;

        /// <summary>
        /// Observable application state.
        /// </summary>
        public ReadOnlyObservable<AppState> State { get; }

        /// <summary>
        /// Current application state value.
        /// </summary>
        public AppState CurrentState => appState.Value;

        public AppLifecycleManager(EventBus eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            appState = new Observable<AppState>(AppState.Active);
            State = new ReadOnlyObservable<AppState>(appState);
        }

        /// <summary>
        /// Registers a system to receive pause/resume callbacks.
        /// </summary>
        public void Register(IAppLifecycleAware system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            lifecycleAwareystems.Add(system);
        }

        /// <summary>
        /// Unregisters a system from pause/resume callbacks.
        /// </summary>
        public void Unregister(IAppLifecycleAware system)
        {
            lifecycleAwareystems.Remove(system);
        }

        /// <summary>
        /// Call from MonoBehaviour.OnApplicationPause.
        /// </summary>
        public void NotifyPause(bool paused)
        {
            if (disposed) return;

            if (paused)
            {
                appState.Value = AppState.Paused;
                eventBus.Publish(new AppPausedEvent());

                for (int i = 0; i < lifecycleAwareystems.Count; i++)
                {
                    lifecycleAwareystems[i].OnAppPause();
                }
            }
            else
            {
                appState.Value = AppState.Active;
                eventBus.Publish(new AppResumedEvent());

                for (int i = 0; i < lifecycleAwareystems.Count; i++)
                {
                    lifecycleAwareystems[i].OnAppResume();
                }
            }
        }

        /// <summary>
        /// Call from MonoBehaviour.OnApplicationFocus.
        /// </summary>
        public void NotifyFocus(bool hasFocus)
        {
            if (disposed) return;

            if (!hasFocus && appState.Value == AppState.Active)
            {
                appState.Value = AppState.Unfocused;
                eventBus.Publish(new AppFocusLostEvent());
            }
            else if (hasFocus && appState.Value == AppState.Unfocused)
            {
                appState.Value = AppState.Active;
                eventBus.Publish(new AppFocusGainedEvent());
            }
        }

        /// <summary>
        /// Call from MonoBehaviour.OnApplicationQuit.
        /// </summary>
        public void NotifyQuit()
        {
            if (disposed) return;

            appState.Value = AppState.Quitting;
            eventBus.Publish(new AppQuittingEvent());
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifecycleAwareystems.Clear();
        }
    }

    /// <summary>
    /// Published when the application is paused.
    /// </summary>
    public struct AppPausedEvent { }

    /// <summary>
    /// Published when the application resumes from a paused state.
    /// </summary>
    public struct AppResumedEvent { }

    /// <summary>
    /// Published when the application loses focus.
    /// </summary>
    public struct AppFocusLostEvent { }

    /// <summary>
    /// Published when the application gains focus.
    /// </summary>
    public struct AppFocusGainedEvent { }

    /// <summary>
    /// Published when the application is quitting.
    /// </summary>
    public struct AppQuittingEvent { }
}
