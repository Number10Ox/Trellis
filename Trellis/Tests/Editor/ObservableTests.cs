using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Reactive;

public class ObservableTests
{
    [Test]
    public void Value_DefaultConstructor_ReturnsDefault()
    {
        var observable = new Observable<int>();

        Assert.AreEqual(0, observable.Value);
    }

    [Test]
    public void Value_InitialValue_ReturnsInitialValue()
    {
        var observable = new Observable<int>(42);

        Assert.AreEqual(42, observable.Value);
    }

    [Test]
    public void Value_Set_UpdatesValue()
    {
        var observable = new Observable<int>(0);

        observable.Value = 42;

        Assert.AreEqual(42, observable.Value);
    }

    [Test]
    public void Subscribe_ValueChange_NotifiesSubscriber()
    {
        var observable = new Observable<int>(0);
        int received = -1;

        observable.Subscribe(v => received = v);
        observable.Value = 42;

        Assert.AreEqual(42, received);
    }

    [Test]
    public void Subscribe_DoesNotFireWithCurrentValue()
    {
        var observable = new Observable<int>(42);
        int callCount = 0;

        observable.Subscribe(v => callCount++);

        Assert.AreEqual(0, callCount);
    }

    [Test]
    public void Subscribe_MultipleSubscribers_AllNotified()
    {
        var observable = new Observable<int>(0);
        int count = 0;

        observable.Subscribe(v => count++);
        observable.Subscribe(v => count++);
        observable.Subscribe(v => count++);

        observable.Value = 42;

        Assert.AreEqual(3, count);
    }

    [Test]
    public void Value_SetSameValue_NoNotification()
    {
        var observable = new Observable<int>(42);
        int callCount = 0;

        observable.Subscribe(v => callCount++);
        observable.Value = 42;

        Assert.AreEqual(0, callCount);
    }

    [Test]
    public void Dispose_StopsReceivingNotifications()
    {
        var observable = new Observable<int>(0);
        int received = -1;

        var subscription = observable.Subscribe(v => received = v);
        subscription.Dispose();

        observable.Value = 42;

        Assert.AreEqual(-1, received);
    }

    [Test]
    public void Dispose_CalledTwice_NoError()
    {
        var observable = new Observable<int>(0);
        var subscription = observable.Subscribe(v => { });

        subscription.Dispose();
        subscription.Dispose(); // Should not throw
    }

    [Test]
    public void Dispose_DuringNotification_SafelyDeferred()
    {
        var observable = new Observable<int>(0);
        IDisposable subscription = null;
        int callCount = 0;

        subscription = observable.Subscribe(v =>
        {
            callCount++;
            subscription.Dispose();
        });

        observable.Subscribe(v => callCount++);

        observable.Value = 1;

        // Both handlers called during first notification
        Assert.AreEqual(2, callCount);

        // First subscription now removed
        callCount = 0;
        observable.Value = 2;

        Assert.AreEqual(1, callCount);
    }

    [Test]
    public void Subscribe_NullHandler_ThrowsArgumentNull()
    {
        var observable = new Observable<int>(0);

        Assert.Throws<ArgumentNullException>(() => observable.Subscribe(null));
    }

    [Test]
    public void SetValueSilent_DoesNotNotify()
    {
        var observable = new Observable<int>(0);
        int callCount = 0;

        observable.Subscribe(v => callCount++);
        observable.SetValueSilent(42);

        Assert.AreEqual(0, callCount);
        Assert.AreEqual(42, observable.Value);
    }

    [Test]
    public void ReentrancySafe_ValueSetDuringNotification()
    {
        var observable = new Observable<int>(0);
        var values = new List<int>();

        observable.Subscribe(v =>
        {
            values.Add(v);
            if (v == 1)
            {
                observable.Value = 2; // Set during notification
            }
        });

        observable.Value = 1;

        // Should see both values - 1 first, then 2 when queue processes
        Assert.AreEqual(new List<int> { 1, 2 }, values);
    }

    [Test]
    public void ReentrancySafe_MultipleNestedSets()
    {
        var observable = new Observable<int>(0);
        var values = new List<int>();

        observable.Subscribe(v =>
        {
            values.Add(v);
            if (v < 3)
            {
                observable.Value = v + 1;
            }
        });

        observable.Value = 1;

        // Each value triggers the next
        Assert.AreEqual(new List<int> { 1, 2, 3 }, values);
    }

    [Test]
    public void NotifyAll_ForcesNotification()
    {
        var observable = new Observable<int>(42);
        int callCount = 0;

        observable.Subscribe(v => callCount++);

        Assert.AreEqual(0, callCount);

        observable.NotifyAll();

        Assert.AreEqual(1, callCount);
    }

    [Test]
    public void ReadOnlyObservable_ExposesValue()
    {
        var observable = new Observable<int>(42);
        var readOnly = new ReadOnlyObservable<int>(observable);

        Assert.AreEqual(42, readOnly.Value);

        observable.Value = 100;

        Assert.AreEqual(100, readOnly.Value);
    }

    [Test]
    public void ReadOnlyObservable_Subscribe_ReceivesNotifications()
    {
        var observable = new Observable<int>(0);
        var readOnly = new ReadOnlyObservable<int>(observable);
        int received = -1;

        readOnly.Subscribe(v => received = v);
        observable.Value = 42;

        Assert.AreEqual(42, received);
    }

    [Test]
    public void ReadOnlyObservable_NullSource_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ReadOnlyObservable<int>(null));
    }

    [Test]
    public void ReferenceType_EqualityCheck()
    {
        var observable = new Observable<string>("hello");
        int callCount = 0;

        observable.Subscribe(v => callCount++);

        observable.Value = "hello"; // Same value
        Assert.AreEqual(0, callCount);

        observable.Value = "world"; // Different value
        Assert.AreEqual(1, callCount);
    }

    [Test]
    public void NullableReferenceType_NullHandling()
    {
        var observable = new Observable<string>(null);
        int callCount = 0;

        observable.Subscribe(v => callCount++);

        observable.Value = null; // Same (null)
        Assert.AreEqual(0, callCount);

        observable.Value = "hello";
        Assert.AreEqual(1, callCount);

        observable.Value = null;
        Assert.AreEqual(2, callCount);
    }
}
