namespace Trellis.Logging
{
    /// <summary>
    /// Output destination for log messages. Implement to route logs to
    /// custom destinations (file, network, UI overlay, etc.).
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Called for each log message that passes the level filter.
        /// </summary>
        void Log(LogTag tag, LogLevel level, string message);
    }
}
