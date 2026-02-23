using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Data;

public class SaveSlotTests
{
    [Test]
    public void Constructor_SetsSlotId()
    {
        var slot = new SaveSlot("slot1");

        Assert.AreEqual("slot1", slot.SlotId);
    }

    [Test]
    public void Constructor_NullSlotId_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveSlot(null));
    }

    [Test]
    public void SetEntry_StoresData()
    {
        var slot = new SaveSlot("slot1");
        var data = new byte[] { 1, 2, 3 };

        slot.SetEntry("player", data);

        Assert.IsTrue(slot.HasEntry("player"));
        Assert.AreEqual(data, slot.GetEntry("player"));
    }

    [Test]
    public void SetEntry_OverwritesExisting()
    {
        var slot = new SaveSlot("slot1");
        slot.SetEntry("player", new byte[] { 1 });
        slot.SetEntry("player", new byte[] { 2, 3 });

        Assert.AreEqual(new byte[] { 2, 3 }, slot.GetEntry("player"));
        Assert.AreEqual(1, slot.EntryCount);
    }

    [Test]
    public void SetEntry_NullKey_Throws()
    {
        var slot = new SaveSlot("slot1");

        Assert.Throws<ArgumentException>(() => slot.SetEntry(null, new byte[] { 1 }));
    }

    [Test]
    public void SetEntry_EmptyKey_Throws()
    {
        var slot = new SaveSlot("slot1");

        Assert.Throws<ArgumentException>(() => slot.SetEntry("", new byte[] { 1 }));
    }

    [Test]
    public void GetEntry_NotFound_ReturnsNull()
    {
        var slot = new SaveSlot("slot1");

        Assert.IsNull(slot.GetEntry("nonexistent"));
    }

    [Test]
    public void HasEntry_True()
    {
        var slot = new SaveSlot("slot1");
        slot.SetEntry("key", new byte[] { 1 });

        Assert.IsTrue(slot.HasEntry("key"));
    }

    [Test]
    public void HasEntry_False()
    {
        var slot = new SaveSlot("slot1");

        Assert.IsFalse(slot.HasEntry("key"));
    }

    [Test]
    public void RemoveEntry_RemovesData()
    {
        var slot = new SaveSlot("slot1");
        slot.SetEntry("key", new byte[] { 1 });

        slot.RemoveEntry("key");

        Assert.IsFalse(slot.HasEntry("key"));
        Assert.AreEqual(0, slot.EntryCount);
    }

    [Test]
    public void Clear_RemovesAll()
    {
        var slot = new SaveSlot("slot1");
        slot.SetEntry("a", new byte[] { 1 });
        slot.SetEntry("b", new byte[] { 2 });

        slot.Clear();

        Assert.AreEqual(0, slot.EntryCount);
    }

    [Test]
    public void CopyKeysTo()
    {
        var slot = new SaveSlot("slot1");
        slot.SetEntry("a", new byte[] { 1 });
        slot.SetEntry("b", new byte[] { 2 });

        var keys = new List<string>();
        slot.CopyKeysTo(keys);

        Assert.AreEqual(2, keys.Count);
        Assert.IsTrue(keys.Contains("a"));
        Assert.IsTrue(keys.Contains("b"));
    }

    [Test]
    public void CopyKeysTo_NullTarget_Throws()
    {
        var slot = new SaveSlot("slot1");

        Assert.Throws<ArgumentNullException>(() => slot.CopyKeysTo(null));
    }
}

public class SaveManagerTests
{
    private TestSerializer serializer;
    private TestStorage storage;
    private SaveManager manager;

    [SetUp]
    public void SetUp()
    {
        serializer = new TestSerializer();
        storage = new TestStorage();
        manager = new SaveManager(serializer, storage);
    }

    [Test]
    public void Constructor_NullSerializer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveManager(null, storage));
    }

    [Test]
    public void Constructor_NullStorage_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveManager(serializer, null));
    }

    [Test]
    public void Register_IncreasesCount()
    {
        manager.Register(new TestSaveable("player"));

        Assert.AreEqual(1, manager.RegisteredCount);
    }

    [Test]
    public void Register_NullSaveable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => manager.Register(null));
    }

    [Test]
    public void Register_EmptySaveKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => manager.Register(new TestSaveable("")));
    }

    [Test]
    public void Unregister_DecreasesCount()
    {
        var saveable = new TestSaveable("player");
        manager.Register(saveable);
        manager.Unregister(saveable);

        Assert.AreEqual(0, manager.RegisteredCount);
    }

    [Test]
    public void Save_WritesToStorage()
    {
        manager.Register(new TestSaveable("player", 42));

        manager.Save("slot1");

        Assert.IsTrue(storage.Exists("slot1"));
    }

    [Test]
    public void Save_NullSlotId_Throws()
    {
        Assert.Throws<ArgumentException>(() => manager.Save(null));
    }

    [Test]
    public void Save_EmptySlotId_Throws()
    {
        Assert.Throws<ArgumentException>(() => manager.Save(""));
    }

    [Test]
    public void Load_NonexistentSlot_ReturnsFalse()
    {
        bool loaded = manager.Load("nonexistent");

        Assert.IsFalse(loaded);
    }

    [Test]
    public void Load_NullSlotId_Throws()
    {
        Assert.Throws<ArgumentException>(() => manager.Load(null));
    }

    [Test]
    public void SlotExists_True()
    {
        storage.Write("slot1", new byte[] { 1 });

        Assert.IsTrue(manager.SlotExists("slot1"));
    }

    [Test]
    public void SlotExists_False()
    {
        Assert.IsFalse(manager.SlotExists("slot1"));
    }

    [Test]
    public void SlotExists_NullId_ReturnsFalse()
    {
        Assert.IsFalse(manager.SlotExists(null));
    }

    [Test]
    public void DeleteSlot_RemovesData()
    {
        storage.Write("slot1", new byte[] { 1 });

        manager.DeleteSlot("slot1");

        Assert.IsFalse(storage.Exists("slot1"));
    }

    [Test]
    public void DeleteSlot_NullId_Throws()
    {
        Assert.Throws<ArgumentException>(() => manager.DeleteSlot(null));
    }

    [Test]
    public void Save_CapturesStateFromSaveables()
    {
        var saveable = new TestSaveable("player", 42);
        manager.Register(saveable);

        manager.Save("slot1");

        Assert.IsTrue(saveable.CaptureCalled);
    }

    // --- Test Helpers ---

    private class TestSaveable : ISaveable
    {
        public string SaveKey { get; }
        public object State;
        public bool CaptureCalled;
        public bool RestoreCalled;

        public TestSaveable(string saveKey, object initialState = null)
        {
            SaveKey = saveKey;
            State = initialState;
        }

        public object CaptureState()
        {
            CaptureCalled = true;
            return State;
        }

        public void RestoreState(object state)
        {
            RestoreCalled = true;
            State = state;
        }
    }

    private class TestSerializer : ISaveSerializer
    {
        public byte[] Serialize<T>(T value)
        {
            // Simple test serializer - just return a marker byte
            return new byte[] { 1 };
        }

        public T Deserialize<T>(byte[] data)
        {
            return default;
        }
    }

    private class TestStorage : ISaveStorage
    {
        private readonly Dictionary<string, byte[]> store = new();

        public bool Exists(string slotId)
        {
            return store.ContainsKey(slotId);
        }

        public void Write(string slotId, byte[] data)
        {
            store[slotId] = data;
        }

        public byte[] Read(string slotId)
        {
            store.TryGetValue(slotId, out byte[] data);
            return data;
        }

        public void Delete(string slotId)
        {
            store.Remove(slotId);
        }
    }
}
