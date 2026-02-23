namespace Trellis.UI
{
    /// <summary>
    /// Describes a toast notification to display.
    /// </summary>
    public class ToastRequest
    {
        /// <summary>
        /// The message text to display.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Display duration in seconds before auto-dismiss.
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// Anchor position for the toast.
        /// </summary>
        public ToastPosition Position { get; }

        public ToastRequest(string message, float duration = 3f, ToastPosition position = ToastPosition.Bottom)
        {
            Message = message ?? string.Empty;
            Duration = duration > 0 ? duration : 3f;
            Position = position;
        }
    }

    /// <summary>
    /// Anchor positions for toast notifications.
    /// </summary>
    public enum ToastPosition
    {
        Top,
        Center,
        Bottom
    }
}
