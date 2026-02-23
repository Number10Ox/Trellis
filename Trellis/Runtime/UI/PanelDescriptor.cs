using System;

namespace Trellis.UI
{
    /// <summary>
    /// Describes a panel's placement in the UI layout.
    /// Each panel declares its target zone and sort order within that zone.
    /// </summary>
    public class PanelDescriptor
    {
        /// <summary>
        /// Unique panel identifier. Must match <see cref="IPanel.PanelId"/>.
        /// </summary>
        public string PanelId { get; }

        /// <summary>
        /// The layout zone this panel occupies.
        /// </summary>
        public LayoutZone Zone { get; }

        /// <summary>
        /// Sort order within the zone. Higher values render on top.
        /// </summary>
        public int SortOrder { get; }

        public PanelDescriptor(string panelId, LayoutZone zone, int sortOrder = 0)
        {
            PanelId = panelId ?? throw new ArgumentNullException(nameof(panelId));
            Zone = zone;
            SortOrder = sortOrder;
        }
    }
}
