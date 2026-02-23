namespace Trellis.UI
{
    /// <summary>
    /// Interface for panels that participate in routing.
    /// The router calls these lifecycle methods during navigation.
    /// </summary>
    public interface IRoutable
    {
        /// <summary>
        /// Called when the route navigates to this panel.
        /// </summary>
        void OnRouteEnter(RouteContext context);

        /// <summary>
        /// Called when the route navigates away from this panel.
        /// </summary>
        void OnRouteExit();
    }
}
