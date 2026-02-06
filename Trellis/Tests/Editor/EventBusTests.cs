using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Events;

public class EventBusTests
{
    private struct TestEvent
    {
        public int Value;

        public TestEvent(int value)
        {
            Value = value;
        }
    }

    private struct OtherEvent
    {
        public string Message;

        public OtherEvent(string message)
        {
            Message = message;
        }
    }

    private EventBus bus;

    [SetUp]
    public void SetUp()
    {
        bus = new EventBus();
    }

    [Test]
    public void Publish_NoSubscribers_NoError()
    {
        // Should not throw
        bus.Publish(new TestEvent(42));
    }

    [Test]
    public void Subscribe_ReceivesPublishedEvents()
    {
        int receivedValue = 0;
        bus.Subscribe<TestEvent>(e => receivedValue = e.Value);

        bus.Publish(new TestEvent(42));

        Assert.AreEqual(42, receivedValue);
    }

    [Test]
    public void Subscribe_MultipleSubscribers_AllReceive()
    {
        int count = 0;
        bus.Subscribe<TestEvent>(e => count++);
        bus.Subscribe<TestEvent>(e => count++);
        bus.Subscribe<TestEvent>(e => count++);

        bus.Publish(new TestEvent(1));

        Assert.AreEqual(3, count);
    }

    [Test]
    public void Subscribe_FIFOOrder()
    {
        var order = new List<int>();
        bus.Subscribe<TestEvent>(e => order.Add(1));
        bus.Subscribe<TestEvent>(e => order.Add(2));
        bus.Subscribe<TestEvent>(e => order.Add(3));

        bus.Publish(new TestEvent(0));

        Assert.AreEqual(new List<int> { 1, 2, 3 }, order);
    }

    [Test]
    public void Subscribe_DifferentEventTypes_Isolated()
    {
        int testEventCount = 0;
        int otherEventCount = 0;

        bus.Subscribe<TestEvent>(e => testEventCount++);
        bus.Subscribe<OtherEvent>(e => otherEventCount++);

        bus.Publish(new TestEvent(1));

        Assert.AreEqual(1, testEventCount);
        Assert.AreEqual(0, otherEventCount);

        bus.Publish(new OtherEvent("hello"));

        Assert.AreEqual(1, testEventCount);
        Assert.AreEqual(1, otherEventCount);
    }

    [Test]
    public void Dispose_StopsReceivingEvents()
    {
        int receivedValue = 0;
        var subscription = bus.Subscribe<TestEvent>(e => receivedValue = e.Value);

        bus.Publish(new TestEvent(42));
        Assert.AreEqual(42, receivedValue);

        subscription.Dispose();

        bus.Publish(new TestEvent(100));
        Assert.AreEqual(42, receivedValue); // Should not have changed
    }

    [Test]
    public void Dispose_CalledTwice_NoError()
    {
        var subscription = bus.Subscribe<TestEvent>(e => { });

        subscription.Dispose();
        subscription.Dispose(); // Should not throw
    }

    [Test]
    public void Dispose_DuringDispatch_SafelyDeferred()
    {
        IEventSubscription subscription = null;
        int callCount = 0;

        subscription = bus.Subscribe<TestEvent>(e =>
        {
            callCount++;
            subscription.Dispose(); // Unsubscribe during dispatch
        });

        bus.Subscribe<TestEvent>(e => callCount++);

        bus.Publish(new TestEvent(1));

        // Both handlers should have been called (deferred removal)
        Assert.AreEqual(2, callCount);

        // Now the first subscription should be removed
        callCount = 0;
        bus.Publish(new TestEvent(2));

        Assert.AreEqual(1, callCount);
    }

    [Test]
    public void SubscriberCount_ReturnsCorrectCount()
    {
        Assert.AreEqual(0, bus.SubscriberCount<TestEvent>());

        var sub1 = bus.Subscribe<TestEvent>(e => { });
        Assert.AreEqual(1, bus.SubscriberCount<TestEvent>());

        var sub2 = bus.Subscribe<TestEvent>(e => { });
        Assert.AreEqual(2, bus.SubscriberCount<TestEvent>());

        sub1.Dispose();
        Assert.AreEqual(1, bus.SubscriberCount<TestEvent>());

        sub2.Dispose();
        Assert.AreEqual(0, bus.SubscriberCount<TestEvent>());
    }

    [Test]
    public void SubscriberCount_DifferentTypes_Separate()
    {
        bus.Subscribe<TestEvent>(e => { });
        bus.Subscribe<TestEvent>(e => { });
        bus.Subscribe<OtherEvent>(e => { });

        Assert.AreEqual(2, bus.SubscriberCount<TestEvent>());
        Assert.AreEqual(1, bus.SubscriberCount<OtherEvent>());
    }

    [Test]
    public void Subscribe_NullHandler_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => bus.Subscribe<TestEvent>(null));
    }

    [Test]
    public void Publish_MultipleEventsInSequence()
    {
        var received = new List<int>();
        bus.Subscribe<TestEvent>(e => received.Add(e.Value));

        bus.Publish(new TestEvent(1));
        bus.Publish(new TestEvent(2));
        bus.Publish(new TestEvent(3));

        Assert.AreEqual(new List<int> { 1, 2, 3 }, received);
    }

    [Test]
    public void Handler_ThrowsException_OtherHandlersNotCalled()
    {
        // Note: This documents current behavior - exceptions propagate
        // and stop subsequent handlers. This is intentional for debugging.
        var order = new List<int>();

        bus.Subscribe<TestEvent>(e => order.Add(1));
        bus.Subscribe<TestEvent>(e => throw new InvalidOperationException("test"));
        bus.Subscribe<TestEvent>(e => order.Add(3));

        Assert.Throws<InvalidOperationException>(() => bus.Publish(new TestEvent(0)));

        // First handler was called before the exception
        Assert.AreEqual(new List<int> { 1 }, order);
    }
}
