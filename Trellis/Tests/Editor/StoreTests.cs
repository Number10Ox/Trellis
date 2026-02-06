using System;
using NUnit.Framework;
using Trellis.Stores;

public class StoreTests
{
    private struct TestState
    {
        public int Count;
        public string Name;

        public TestState(int count, string name)
        {
            Count = count;
            Name = name;
        }
    }

    private class TestActions : StoreActions<TestState>
    {
        public TestActions(Store<TestState> store) : base(store)
        {
        }

        public void Increment()
        {
            UpdateState(s => new TestState(s.Count + 1, s.Name));
        }

        public void SetName(string name)
        {
            UpdateState(s => new TestState(s.Count, name));
        }

        public void SetAll(int count, string name)
        {
            SetState(new TestState(count, name));
        }

        public TestState Peek()
        {
            return GetState();
        }
    }

    [Test]
    public void Store_DefaultConstructor_DefaultValue()
    {
        var store = new Store<int>();

        Assert.AreEqual(0, store.CurrentValue);
    }

    [Test]
    public void Store_InitialValue_ReturnsInitialValue()
    {
        var store = new Store<int>(42);

        Assert.AreEqual(42, store.CurrentValue);
    }

    [Test]
    public void Store_State_ReturnsReadOnlyObservable()
    {
        var store = new Store<int>(42);

        Assert.IsNotNull(store.State);
        Assert.AreEqual(42, store.State.Value);
    }

    [Test]
    public void Store_Reset_RestoresInitialValue()
    {
        var store = new Store<TestState>(new TestState(0, "initial"));
        var actions = new TestActions(store);

        actions.SetAll(100, "modified");
        Assert.AreEqual(100, store.CurrentValue.Count);

        store.Reset();

        Assert.AreEqual(0, store.CurrentValue.Count);
        Assert.AreEqual("initial", store.CurrentValue.Name);
    }

    [Test]
    public void Store_Reset_NotifiesSubscribers()
    {
        var store = new Store<int>(0);
        var actions = new TestIntActions(store);
        int notified = -1;

        store.State.Subscribe(v => notified = v);

        actions.Set(100);
        Assert.AreEqual(100, notified);

        store.Reset();

        Assert.AreEqual(0, notified);
    }

    [Test]
    public void StoreActions_UpdateState_ModifiesStore()
    {
        var store = new Store<TestState>(new TestState(0, "test"));
        var actions = new TestActions(store);

        actions.Increment();

        Assert.AreEqual(1, store.CurrentValue.Count);
    }

    [Test]
    public void StoreActions_UpdateState_TriggersNotification()
    {
        var store = new Store<TestState>(new TestState(0, "test"));
        var actions = new TestActions(store);
        int notifiedCount = 0;

        store.State.Subscribe(s => notifiedCount = s.Count);

        actions.Increment();

        Assert.AreEqual(1, notifiedCount);
    }

    [Test]
    public void StoreActions_SetState_ModifiesStore()
    {
        var store = new Store<TestState>(new TestState(0, ""));
        var actions = new TestActions(store);

        actions.SetAll(42, "hello");

        Assert.AreEqual(42, store.CurrentValue.Count);
        Assert.AreEqual("hello", store.CurrentValue.Name);
    }

    [Test]
    public void StoreActions_GetState_ReturnsCurrent()
    {
        var store = new Store<TestState>(new TestState(42, "test"));
        var actions = new TestActions(store);

        var state = actions.Peek();

        Assert.AreEqual(42, state.Count);
        Assert.AreEqual("test", state.Name);
    }

    [Test]
    public void StoreActions_NullStore_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new TestActions(null));
    }

    [Test]
    public void StoreActions_NullUpdater_ThrowsArgumentNull()
    {
        var store = new Store<TestState>();
        var actions = new TestNullCheckActions(store);

        Assert.Throws<ArgumentNullException>(() => actions.UpdateWithNull());
    }

    private class TestIntActions : StoreActions<int>
    {
        public TestIntActions(Store<int> store) : base(store)
        {
        }

        public void Set(int value)
        {
            SetState(value);
        }
    }

    private class TestNullCheckActions : StoreActions<TestState>
    {
        public TestNullCheckActions(Store<TestState> store) : base(store)
        {
        }

        public void UpdateWithNull()
        {
            UpdateState(null);
        }
    }
}

public class LoadObjectTests
{
    [Test]
    public void WithValue_SetsValueAndNoneState()
    {
        var obj = LoadObject<int>.WithValue(42);

        Assert.AreEqual(42, obj.Value);
        Assert.AreEqual(LoadState.None, obj.State);
        Assert.IsTrue(obj.HasValue);
        Assert.IsFalse(obj.IsLoading);
        Assert.IsFalse(obj.HasError);
        Assert.IsNull(obj.ErrorMessage);
    }

    [Test]
    public void Reading_SetsReadingState()
    {
        var obj = LoadObject<int>.Reading();

        Assert.AreEqual(0, obj.Value);
        Assert.AreEqual(LoadState.Reading, obj.State);
        Assert.IsFalse(obj.HasValue);
        Assert.IsTrue(obj.IsLoading);
        Assert.IsFalse(obj.HasError);
    }

    [Test]
    public void Writing_SetsWritingState()
    {
        var obj = LoadObject<int>.Writing();

        Assert.AreEqual(0, obj.Value);
        Assert.AreEqual(LoadState.Writing, obj.State);
        Assert.IsFalse(obj.HasValue);
        Assert.IsTrue(obj.IsLoading);
        Assert.IsFalse(obj.HasError);
    }

    [Test]
    public void WithError_SetsErrorState()
    {
        var obj = LoadObject<int>.WithError("Something went wrong");

        Assert.AreEqual(0, obj.Value);
        Assert.AreEqual(LoadState.Error, obj.State);
        Assert.IsFalse(obj.HasValue);
        Assert.IsFalse(obj.IsLoading);
        Assert.IsTrue(obj.HasError);
        Assert.AreEqual("Something went wrong", obj.ErrorMessage);
    }

    [Test]
    public void WithError_NullMessage_EmptyString()
    {
        var obj = LoadObject<int>.WithError(null);

        Assert.AreEqual(string.Empty, obj.ErrorMessage);
    }

    [Test]
    public void Empty_DefaultValueAndNoneState()
    {
        var obj = LoadObject<string>.Empty();

        Assert.IsNull(obj.Value);
        Assert.AreEqual(LoadState.None, obj.State);
        Assert.IsTrue(obj.HasValue);
    }

    [Test]
    public void ReferenceType_WithValue()
    {
        var obj = LoadObject<string>.WithValue("hello");

        Assert.AreEqual("hello", obj.Value);
        Assert.IsTrue(obj.HasValue);
    }
}
