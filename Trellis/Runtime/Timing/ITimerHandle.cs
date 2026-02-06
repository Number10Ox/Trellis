namespace Trellis.Timing
{
    /// <summary>
    /// Handle for a scheduled timer. Use to cancel the timer before it fires.
    /// </summary>
    public interface ITimerHandle
    {
        /// <summary>
        /// True if the timer is still active (not yet fired or cancelled).
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Cancels the timer, preventing it from firing.
        /// </summary>
        void Cancel();
    }
}
