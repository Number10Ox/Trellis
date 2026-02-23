using System;

namespace Trellis.UI
{
    /// <summary>
    /// A registered route mapping a path pattern to a set of panel IDs.
    /// Routes are registered with the <see cref="UIRouter"/> to define navigation targets.
    /// </summary>
    public class Route
    {
        /// <summary>
        /// The route path pattern (e.g., "/menu", "/gameplay/inventory").
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// IDs of panels to show when this route is active.
        /// </summary>
        public string[] PanelIds { get; }

        public Route(string path, string[] panelIds)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            PanelIds = panelIds ?? throw new ArgumentNullException(nameof(panelIds));
        }

        /// <summary>
        /// Returns true if this route matches the given path.
        /// Matching is exact (no wildcards).
        /// </summary>
        public bool Matches(string path)
        {
            return string.Equals(Path, path, StringComparison.Ordinal);
        }
    }
}
