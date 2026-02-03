using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Scheduling;

public class SystemSchedulerTests
{
    private class RecordingSystem : ISystem
    {
        public string Name;
        public List<string> ExecutionLog;
        public float LastDeltaTime;
        public int TickCount;

        public RecordingSystem(string name, List<string> executionLog)
        {
            Name = name;
            ExecutionLog = executionLog;
        }

        public void Tick(float deltaTime)
        {
            TickCount++;
            LastDeltaTime = deltaTime;
            ExecutionLog.Add(Name);
        }
    }

    [Test]
    public void Tick_ExecutesSystemsInOrder()
    {
        var log = new List<string>();
        var systems = new ISystem[]
        {
            new RecordingSystem("First", log),
            new RecordingSystem("Second", log),
            new RecordingSystem("Third", log)
        };

        var scheduler = new SystemScheduler(systems);

        scheduler.Tick(0.016f);

        Assert.AreEqual(3, log.Count);
        Assert.AreEqual("First", log[0]);
        Assert.AreEqual("Second", log[1]);
        Assert.AreEqual("Third", log[2]);
    }

    [Test]
    public void Tick_PassesDeltaTimeToAllSystems()
    {
        var log = new List<string>();
        var systemA = new RecordingSystem("A", log);
        var systemB = new RecordingSystem("B", log);
        var scheduler = new SystemScheduler(new ISystem[] { systemA, systemB });

        scheduler.Tick(0.033f);

        Assert.AreEqual(0.033f, systemA.LastDeltaTime);
        Assert.AreEqual(0.033f, systemB.LastDeltaTime);
    }

    [Test]
    public void Tick_NullElementsSkipped()
    {
        var log = new List<string>();
        var systemA = new RecordingSystem("A", log);
        var systems = new ISystem[] { systemA, null, new RecordingSystem("C", log) };

        var scheduler = new SystemScheduler(systems);

        Assert.DoesNotThrow(() => scheduler.Tick(0.016f));
        Assert.AreEqual(2, log.Count);
        Assert.AreEqual("A", log[0]);
        Assert.AreEqual("C", log[1]);
    }

    [Test]
    public void Tick_EmptyArray_DoesNotThrow()
    {
        var scheduler = new SystemScheduler(System.Array.Empty<ISystem>());

        Assert.DoesNotThrow(() => scheduler.Tick(0.016f));
    }

    [Test]
    public void Tick_NullArray_DoesNotThrow()
    {
        var scheduler = new SystemScheduler(null);

        Assert.DoesNotThrow(() => scheduler.Tick(0.016f));
    }

    [Test]
    public void Tick_CalledMultipleTimes_TicksEachTime()
    {
        var log = new List<string>();
        var system = new RecordingSystem("A", log);
        var scheduler = new SystemScheduler(new ISystem[] { system });

        scheduler.Tick(0.016f);
        scheduler.Tick(0.016f);
        scheduler.Tick(0.016f);

        Assert.AreEqual(3, system.TickCount);
    }
}
