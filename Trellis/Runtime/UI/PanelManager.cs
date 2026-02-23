using System;
using System.Collections.Generic;

namespace Trellis.UI
{
    /// <summary>
    /// Manages the lifecycle and layering of UI panels within layout zones.
    /// Panels are registered with descriptors that define zone placement and sort order.
    /// The PanelManager handles show/hide operations and zone-based queries.
    /// </summary>
    public class PanelManager
    {
        private readonly Dictionary<string, IPanel> panels = new();
        private readonly Dictionary<string, PanelDescriptor> descriptors = new();
        private readonly Dictionary<LayoutZone, List<string>> zonePanels = new();

        public PanelManager()
        {
            // Initialize zone lists
            var zones = (LayoutZone[])Enum.GetValues(typeof(LayoutZone));
            for (int i = 0; i < zones.Length; i++)
            {
                zonePanels[zones[i]] = new List<string>();
            }
        }

        /// <summary>
        /// Number of registered panels.
        /// </summary>
        public int PanelCount => panels.Count;

        /// <summary>
        /// Registers a panel with its descriptor.
        /// </summary>
        public void Register(IPanel panel, PanelDescriptor descriptor)
        {
            if (panel == null)
            {
                throw new ArgumentNullException(nameof(panel));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (panel.PanelId != descriptor.PanelId)
            {
                throw new ArgumentException(
                    $"Panel ID mismatch: panel '{panel.PanelId}' != descriptor '{descriptor.PanelId}'.");
            }

            if (panels.ContainsKey(panel.PanelId))
            {
                throw new ArgumentException($"Panel '{panel.PanelId}' is already registered.");
            }

            panels[panel.PanelId] = panel;
            descriptors[panel.PanelId] = descriptor;
            InsertIntoZone(descriptor);
        }

        /// <summary>
        /// Unregisters a panel by ID.
        /// </summary>
        public void Unregister(string panelId)
        {
            if (panels.TryGetValue(panelId, out IPanel panel))
            {
                if (panel.IsVisible)
                {
                    panel.Hide();
                }

                panels.Remove(panelId);

                if (descriptors.TryGetValue(panelId, out PanelDescriptor desc))
                {
                    zonePanels[desc.Zone].Remove(panelId);
                    descriptors.Remove(panelId);
                }
            }
        }

        /// <summary>
        /// Shows a panel by ID.
        /// </summary>
        public void ShowPanel(string panelId)
        {
            if (panels.TryGetValue(panelId, out IPanel panel))
            {
                panel.Show();
            }
        }

        /// <summary>
        /// Hides a panel by ID.
        /// </summary>
        public void HidePanel(string panelId)
        {
            if (panels.TryGetValue(panelId, out IPanel panel))
            {
                panel.Hide();
            }
        }

        /// <summary>
        /// Shows multiple panels by ID.
        /// </summary>
        public void ShowPanels(string[] panelIds)
        {
            if (panelIds == null) return;

            for (int i = 0; i < panelIds.Length; i++)
            {
                ShowPanel(panelIds[i]);
            }
        }

        /// <summary>
        /// Hides multiple panels by ID.
        /// </summary>
        public void HidePanels(string[] panelIds)
        {
            if (panelIds == null) return;

            for (int i = 0; i < panelIds.Length; i++)
            {
                HidePanel(panelIds[i]);
            }
        }

        /// <summary>
        /// Hides all visible panels.
        /// </summary>
        public void HideAll()
        {
            foreach (var panel in panels.Values)
            {
                if (panel.IsVisible)
                {
                    panel.Hide();
                }
            }
        }

        /// <summary>
        /// Returns true if a panel is registered with the given ID.
        /// </summary>
        public bool HasPanel(string panelId)
        {
            return panels.ContainsKey(panelId);
        }

        /// <summary>
        /// Gets a panel by ID. Returns null if not found.
        /// </summary>
        public IPanel GetPanel(string panelId)
        {
            panels.TryGetValue(panelId, out IPanel panel);
            return panel;
        }

        /// <summary>
        /// Gets the descriptor for a panel. Returns null if not found.
        /// </summary>
        public PanelDescriptor GetDescriptor(string panelId)
        {
            descriptors.TryGetValue(panelId, out PanelDescriptor desc);
            return desc;
        }

        /// <summary>
        /// Gets the number of panels in a specific zone.
        /// </summary>
        public int PanelCountInZone(LayoutZone zone)
        {
            return zonePanels[zone].Count;
        }

        /// <summary>
        /// Copies panel IDs in the given zone into the provided list, sorted by sort order.
        /// </summary>
        public void CopyPanelIdsInZone(LayoutZone zone, List<string> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var zoneList = zonePanels[zone];
            for (int i = 0; i < zoneList.Count; i++)
            {
                target.Add(zoneList[i]);
            }
        }

        private void InsertIntoZone(PanelDescriptor descriptor)
        {
            var zoneList = zonePanels[descriptor.Zone];
            int insertIndex = zoneList.Count;

            // Insert sorted by sort order (ascending)
            for (int i = 0; i < zoneList.Count; i++)
            {
                if (descriptors.TryGetValue(zoneList[i], out PanelDescriptor existing))
                {
                    if (descriptor.SortOrder < existing.SortOrder)
                    {
                        insertIndex = i;
                        break;
                    }
                }
            }

            zoneList.Insert(insertIndex, descriptor.PanelId);
        }
    }
}
