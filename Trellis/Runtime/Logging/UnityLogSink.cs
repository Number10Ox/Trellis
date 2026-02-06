using UnityEngine;

namespace Trellis.Logging
{
    /// <summary>
    /// Default log sink that writes to Unity's Debug.Log/LogWarning/LogError.
    /// </summary>
    public class UnityLogSink : ILogSink
    {
        private readonly bool includeTag;
        private readonly bool includeLevel;

        public UnityLogSink() : this(true, true)
        {
        }

        public UnityLogSink(bool includeTag, bool includeLevel)
        {
            this.includeTag = includeTag;
            this.includeLevel = includeLevel;
        }

        public void Log(LogTag tag, LogLevel level, string message)
        {
            string formattedMessage = FormatMessage(tag, level, message);

            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;

                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;

                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage);
                    break;
            }
        }

        private string FormatMessage(LogTag tag, LogLevel level, string message)
        {
            if (!includeTag && !includeLevel)
            {
                return message;
            }

            if (includeTag && includeLevel)
            {
                return $"[{tag}] [{level}] {message}";
            }

            if (includeTag)
            {
                return $"[{tag}] {message}";
            }

            return $"[{level}] {message}";
        }
    }
}
