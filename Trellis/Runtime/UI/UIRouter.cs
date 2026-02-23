using System;
using System.Collections.Generic;
using Trellis.Events;
using Trellis.Reactive;

namespace Trellis.UI
{
    /// <summary>
    /// Route-based UI navigation. Resolves string routes to panel configurations.
    /// Supports query parameters, history stack, and back navigation.
    /// Deep linking: any registered route is directly navigable.
    /// </summary>
    public class UIRouter
    {
        private readonly Dictionary<string, Route> routes = new();
        private readonly Stack<string> history = new();
        private readonly Observable<string> currentRoute;
        private readonly EventBus eventBus;
        private readonly List<string> activePanelIds = new();

        /// <summary>
        /// Callback invoked when panels should be shown. Arguments: panel IDs to show, route context.
        /// Set by the PanelManager or consuming code to handle panel activation.
        /// </summary>
        public Action<string[], RouteContext> OnShowPanels;

        /// <summary>
        /// Callback invoked when panels should be hidden. Arguments: panel IDs to hide.
        /// </summary>
        public Action<string[]> OnHidePanels;

        /// <summary>
        /// Callback invoked to notify a panel of route entry. Arguments: panel ID, route context.
        /// </summary>
        public Action<string, RouteContext> OnRouteEnterPanel;

        /// <summary>
        /// Callback invoked to notify a panel of route exit. Arguments: panel ID.
        /// </summary>
        public Action<string> OnRouteExitPanel;

        /// <summary>
        /// Observable current route path. Subscribers are notified on navigation.
        /// </summary>
        public ReadOnlyObservable<string> CurrentRoute { get; }

        /// <summary>
        /// Current route path.
        /// </summary>
        public string CurrentPath => currentRoute.Value;

        /// <summary>
        /// Number of entries in the history stack (not including current).
        /// </summary>
        public int HistoryCount => history.Count;

        /// <summary>
        /// True if back navigation is possible (history is not empty).
        /// </summary>
        public bool CanGoBack => history.Count > 0;

        /// <summary>
        /// IDs of panels currently active from the current route.
        /// </summary>
        public int ActivePanelCount => activePanelIds.Count;

        public UIRouter(EventBus eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            currentRoute = new Observable<string>(string.Empty);
            CurrentRoute = new ReadOnlyObservable<string>(currentRoute);
        }

        /// <summary>
        /// Registers a route. Duplicate paths are overwritten.
        /// </summary>
        public void RegisterRoute(Route route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            routes[route.Path] = route;
        }

        /// <summary>
        /// Registers a route with a path and panel IDs.
        /// </summary>
        public void RegisterRoute(string path, params string[] panelIds)
        {
            RegisterRoute(new Route(path, panelIds));
        }

        /// <summary>
        /// Navigates to the specified route. Pushes the current route onto the history stack.
        /// Route can include query parameters (e.g., "/inventory?itemId=42").
        /// </summary>
        public bool Navigate(string route)
        {
            if (string.IsNullOrEmpty(route))
            {
                return false;
            }

            var context = RouteContext.Parse(route);
            string path = context.Path;

            if (!routes.TryGetValue(path, out Route routeDef))
            {
                return false;
            }

            // Push current route to history (if we have one)
            if (!string.IsNullOrEmpty(currentRoute.Value))
            {
                history.Push(currentRoute.Value);
            }

            // Exit current panels
            ExitCurrentPanels();

            // Activate new panels
            activePanelIds.Clear();
            for (int i = 0; i < routeDef.PanelIds.Length; i++)
            {
                activePanelIds.Add(routeDef.PanelIds[i]);
            }

            currentRoute.Value = route;

            OnShowPanels?.Invoke(routeDef.PanelIds, context);

            for (int i = 0; i < routeDef.PanelIds.Length; i++)
            {
                OnRouteEnterPanel?.Invoke(routeDef.PanelIds[i], context);
            }

            eventBus.Publish(new RouteChangedEvent { Route = route, Path = path });

            return true;
        }

        /// <summary>
        /// Navigates back to the previous route in the history stack.
        /// Returns false if there is no history.
        /// </summary>
        public bool Back()
        {
            if (history.Count == 0)
            {
                return false;
            }

            string previousRoute = history.Pop();

            var context = RouteContext.Parse(previousRoute);
            string path = context.Path;

            if (!routes.TryGetValue(path, out Route routeDef))
            {
                return false;
            }

            // Exit current panels
            ExitCurrentPanels();

            // Activate previous panels
            activePanelIds.Clear();
            for (int i = 0; i < routeDef.PanelIds.Length; i++)
            {
                activePanelIds.Add(routeDef.PanelIds[i]);
            }

            currentRoute.Value = previousRoute;

            OnShowPanels?.Invoke(routeDef.PanelIds, context);

            for (int i = 0; i < routeDef.PanelIds.Length; i++)
            {
                OnRouteEnterPanel?.Invoke(routeDef.PanelIds[i], context);
            }

            eventBus.Publish(new RouteChangedEvent { Route = previousRoute, Path = path });

            return true;
        }

        /// <summary>
        /// Clears the navigation history stack.
        /// </summary>
        public void ClearHistory()
        {
            history.Clear();
        }

        /// <summary>
        /// Returns true if a route is registered for the given path.
        /// </summary>
        public bool HasRoute(string path)
        {
            return routes.ContainsKey(path);
        }

        /// <summary>
        /// Number of registered routes.
        /// </summary>
        public int RouteCount => routes.Count;

        private void ExitCurrentPanels()
        {
            if (activePanelIds.Count > 0)
            {
                var panelIds = new string[activePanelIds.Count];
                for (int i = 0; i < activePanelIds.Count; i++)
                {
                    panelIds[i] = activePanelIds[i];
                    OnRouteExitPanel?.Invoke(activePanelIds[i]);
                }

                OnHidePanels?.Invoke(panelIds);
            }
        }
    }

    /// <summary>
    /// Published when the current route changes.
    /// </summary>
    public struct RouteChangedEvent
    {
        public string Route;
        public string Path;
    }
}
