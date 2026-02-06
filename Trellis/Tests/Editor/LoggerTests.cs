using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Logging;

public class LoggerTests
{
    private class RecordingSink : ILogSink
    {
        public List<(LogTag Tag, LogLevel Level, string Message)> Logs = new();

        public void Log(LogTag tag, LogLevel level, string message)
        {
            Logs.Add((tag, level, message));
        }

        public void Clear()
        {
            Logs.Clear();
        }
    }

    private TrellisLogger logger;
    private RecordingSink sink;

    [SetUp]
    public void SetUp()
    {
        logger = new TrellisLogger(LogLevel.Trace); // Allow all levels
        sink = new RecordingSink();
        logger.AddSink(sink);
    }

    [Test]
    public void Log_MessagePassedToSink()
    {
        logger.Log(LogTag.Core, LogLevel.Info, "test message");

        Assert.AreEqual(1, sink.Logs.Count);
        Assert.AreEqual(LogTag.Core, sink.Logs[0].Tag);
        Assert.AreEqual(LogLevel.Info, sink.Logs[0].Level);
        Assert.AreEqual("test message", sink.Logs[0].Message);
    }

    [Test]
    public void Log_MultipleSinks_AllReceive()
    {
        var sink2 = new RecordingSink();
        logger.AddSink(sink2);

        logger.Info(LogTag.Core, "test");

        Assert.AreEqual(1, sink.Logs.Count);
        Assert.AreEqual(1, sink2.Logs.Count);
    }

    [Test]
    public void RemoveSink_StopsReceiving()
    {
        logger.RemoveSink(sink);

        logger.Info(LogTag.Core, "test");

        Assert.AreEqual(0, sink.Logs.Count);
    }

    [Test]
    public void AddSink_NullSink_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => logger.AddSink(null));
    }

    [Test]
    public void SetFilter_FiltersByLevel()
    {
        logger.SetFilter(LogTag.Core, LogLevel.Warning);

        logger.Debug(LogTag.Core, "debug");
        logger.Info(LogTag.Core, "info");
        logger.Warn(LogTag.Core, "warning");
        logger.Error(LogTag.Core, "error");

        Assert.AreEqual(2, sink.Logs.Count);
        Assert.AreEqual(LogLevel.Warning, sink.Logs[0].Level);
        Assert.AreEqual(LogLevel.Error, sink.Logs[1].Level);
    }

    [Test]
    public void SetFilter_PerTagFiltering()
    {
        logger.SetFilter(LogTag.Core, LogLevel.Warning);
        logger.SetFilter(LogTag.UI, LogLevel.Debug);

        logger.Debug(LogTag.Core, "core debug");
        logger.Debug(LogTag.UI, "ui debug");

        Assert.AreEqual(1, sink.Logs.Count);
        Assert.AreEqual(LogTag.UI, sink.Logs[0].Tag);
    }

    [Test]
    public void GetFilter_ReturnsCurrentFilter()
    {
        logger.SetFilter(LogTag.Core, LogLevel.Warning);

        Assert.AreEqual(LogLevel.Warning, logger.GetFilter(LogTag.Core));
    }

    [Test]
    public void SetGlobalFilter_AffectsAllTags()
    {
        logger.SetGlobalFilter(LogLevel.Error);

        logger.Warn(LogTag.Core, "warning");
        logger.Warn(LogTag.UI, "warning");
        logger.Error(LogTag.Core, "error");

        Assert.AreEqual(1, sink.Logs.Count);
        Assert.AreEqual(LogLevel.Error, sink.Logs[0].Level);
    }

    [Test]
    public void IsEnabled_ReturnsTrue_WhenLevelMeetsFilter()
    {
        logger.SetFilter(LogTag.Core, LogLevel.Warning);

        Assert.IsFalse(logger.IsEnabled(LogTag.Core, LogLevel.Debug));
        Assert.IsFalse(logger.IsEnabled(LogTag.Core, LogLevel.Info));
        Assert.IsTrue(logger.IsEnabled(LogTag.Core, LogLevel.Warning));
        Assert.IsTrue(logger.IsEnabled(LogTag.Core, LogLevel.Error));
    }

    [Test]
    public void IsEnabled_UsedForZeroAllocationPattern()
    {
        logger.SetFilter(LogTag.Core, LogLevel.Error);

        // This pattern avoids string allocation when filtered
        if (logger.IsEnabled(LogTag.Core, LogLevel.Debug))
        {
            logger.Debug(LogTag.Core, $"Expensive string: {ComputeExpensiveString()}");
        }

        Assert.AreEqual(0, sink.Logs.Count);
    }

    [Test]
    public void Trace_LogsAtTraceLevel()
    {
        logger.Trace(LogTag.Core, "trace");

        Assert.AreEqual(LogLevel.Trace, sink.Logs[0].Level);
    }

    [Test]
    public void Debug_LogsAtDebugLevel()
    {
        logger.Debug(LogTag.Core, "debug");

        Assert.AreEqual(LogLevel.Debug, sink.Logs[0].Level);
    }

    [Test]
    public void Info_LogsAtInfoLevel()
    {
        logger.Info(LogTag.Core, "info");

        Assert.AreEqual(LogLevel.Info, sink.Logs[0].Level);
    }

    [Test]
    public void Warn_LogsAtWarningLevel()
    {
        logger.Warn(LogTag.Core, "warning");

        Assert.AreEqual(LogLevel.Warning, sink.Logs[0].Level);
    }

    [Test]
    public void Error_LogsAtErrorLevel()
    {
        logger.Error(LogTag.Core, "error");

        Assert.AreEqual(LogLevel.Error, sink.Logs[0].Level);
    }

    [Test]
    public void DefaultConstructor_SetsInfoLevel()
    {
        var defaultLogger = new TrellisLogger();
        var testSink = new RecordingSink();
        defaultLogger.AddSink(testSink);

        defaultLogger.Debug(LogTag.Core, "debug");
        defaultLogger.Info(LogTag.Core, "info");

        Assert.AreEqual(1, testSink.Logs.Count);
        Assert.AreEqual(LogLevel.Info, testSink.Logs[0].Level);
    }

    [Test]
    public void NoSinks_NoError()
    {
        var emptyLogger = new TrellisLogger();

        // Should not throw
        emptyLogger.Info(LogTag.Core, "message");
    }

    [Test]
    public void LogLevel_OrderCorrect()
    {
        Assert.IsTrue(LogLevel.Trace < LogLevel.Debug);
        Assert.IsTrue(LogLevel.Debug < LogLevel.Info);
        Assert.IsTrue(LogLevel.Info < LogLevel.Warning);
        Assert.IsTrue(LogLevel.Warning < LogLevel.Error);
    }

    private string ComputeExpensiveString()
    {
        // In a real scenario, this would be expensive
        return "expensive";
    }
}
