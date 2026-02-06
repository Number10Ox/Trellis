using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trellis.Logging
{
    /// <summary>
    /// Tagged, filterable logging system. Filters are applied before string formatting
    /// to avoid allocations when logs are filtered out.
    /// </summary>
    public class TrellisLogger
    {
        private const int MAX_TAG_VALUE = 256;

        private readonly List<ILogSink> sinks = new();
        private readonly LogLevel[] tagFilters = new LogLevel[MAX_TAG_VALUE];
        private LogLevel globalMinLevel;

        public TrellisLogger() : this(LogLevel.Info)
        {
        }

        public TrellisLogger(LogLevel defaultLevel)
        {
            globalMinLevel = defaultLevel;

            for (int i = 0; i < MAX_TAG_VALUE; i++)
            {
                tagFilters[i] = defaultLevel;
            }
        }

        /// <summary>
        /// Adds a log sink. Multiple sinks can be registered.
        /// </summary>
        public void AddSink(ILogSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            sinks.Add(sink);
        }

        /// <summary>
        /// Removes a log sink.
        /// </summary>
        public void RemoveSink(ILogSink sink)
        {
            sinks.Remove(sink);
        }

        /// <summary>
        /// Sets the minimum log level for a specific tag.
        /// </summary>
        public void SetFilter(LogTag tag, LogLevel minLevel)
        {
            int index = (int)tag;
            if (index >= 0 && index < MAX_TAG_VALUE)
            {
                tagFilters[index] = minLevel;
            }
        }

        /// <summary>
        /// Gets the minimum log level for a specific tag.
        /// </summary>
        public LogLevel GetFilter(LogTag tag)
        {
            int index = (int)tag;
            if (index >= 0 && index < MAX_TAG_VALUE)
            {
                return tagFilters[index];
            }

            return globalMinLevel;
        }

        /// <summary>
        /// Sets the global minimum log level for all tags.
        /// </summary>
        public void SetGlobalFilter(LogLevel minLevel)
        {
            globalMinLevel = minLevel;

            for (int i = 0; i < MAX_TAG_VALUE; i++)
            {
                tagFilters[i] = minLevel;
            }
        }

        /// <summary>
        /// Checks if a log with the given tag and level would be emitted.
        /// Use this before expensive string formatting.
        /// </summary>
        public bool IsEnabled(LogTag tag, LogLevel level)
        {
            int index = (int)tag;
            if (index >= 0 && index < MAX_TAG_VALUE)
            {
                return level >= tagFilters[index];
            }

            return level >= globalMinLevel;
        }

        /// <summary>
        /// Logs a message with the specified tag and level.
        /// </summary>
        public void Log(LogTag tag, LogLevel level, string message)
        {
            if (!IsEnabled(tag, level))
            {
                return;
            }

            for (int i = 0; i < sinks.Count; i++)
            {
                sinks[i].Log(tag, level, message);
            }
        }

        /// <summary>
        /// Logs a Trace level message.
        /// </summary>
        public void Trace(LogTag tag, string message)
        {
            Log(tag, LogLevel.Trace, message);
        }

        /// <summary>
        /// Logs a Debug level message.
        /// </summary>
        public void Debug(LogTag tag, string message)
        {
            Log(tag, LogLevel.Debug, message);
        }

        /// <summary>
        /// Logs an Info level message.
        /// </summary>
        public void Info(LogTag tag, string message)
        {
            Log(tag, LogLevel.Info, message);
        }

        /// <summary>
        /// Logs a Warning level message.
        /// </summary>
        public void Warn(LogTag tag, string message)
        {
            Log(tag, LogLevel.Warning, message);
        }

        /// <summary>
        /// Logs an Error level message.
        /// </summary>
        public void Error(LogTag tag, string message)
        {
            Log(tag, LogLevel.Error, message);
        }
    }
}
