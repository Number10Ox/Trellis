namespace Trellis.Logging
{
    /// <summary>
    /// Log severity levels, ordered from most verbose to most severe.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Detailed diagnostic information for debugging specific issues.
        /// </summary>
        Trace = 0,

        /// <summary>
        /// General debugging information useful during development.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Informational messages about normal operation.
        /// </summary>
        Info = 2,

        /// <summary>
        /// Warnings about potential issues that don't prevent operation.
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Errors that indicate failures.
        /// </summary>
        Error = 4
    }
}
