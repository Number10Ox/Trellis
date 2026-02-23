namespace Trellis.UI
{
    /// <summary>
    /// Screen regions for panel placement. Each panel declares its target zone.
    /// Multiple panels can occupy the same zone (stacked by sort order).
    /// </summary>
    public enum LayoutZone
    {
        Top,
        Bottom,
        Left,
        Right,
        Center,
        Overlay
    }
}
