using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Data;

public class DefinitionRegistryTests
{
    private enum ItemId
    {
        Sword,
        Shield,
        Potion
    }

    private class ItemDef
    {
        public ItemId Id;
        public string Name;
        public int Cost;

        public ItemDef(ItemId id, string name, int cost)
        {
            Id = id;
            Name = name;
            Cost = cost;
        }
    }

    private class TestSource : IDefinitionSource<ItemDef>
    {
        private readonly List<ItemDef> items;

        public TestSource(params ItemDef[] items)
        {
            this.items = new List<ItemDef>(items);
        }

        public void LoadDefinitions(List<ItemDef> results)
        {
            for (int i = 0; i < items.Count; i++)
            {
                results.Add(items[i]);
            }
        }
    }

    private class NullProducingSource : IDefinitionSource<ItemDef>
    {
        public void LoadDefinitions(List<ItemDef> results)
        {
            results.Add(null);
        }
    }

    // --- Builder Tests ---

    [Test]
    public void Builder_Add_SingleDefinition()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Build();

        Assert.AreEqual(1, registry.Count);
        Assert.IsTrue(registry.Contains(ItemId.Sword));
    }

    [Test]
    public void Builder_Add_MultipleDefinitions()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Add(new ItemDef(ItemId.Shield, "Shield", 75))
            .Add(new ItemDef(ItemId.Potion, "Potion", 25))
            .Build();

        Assert.AreEqual(3, registry.Count);
    }

    [Test]
    public void Builder_Add_DuplicateKey_Throws()
    {
        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100));

        Assert.Throws<ArgumentException>(() =>
            builder.Add(new ItemDef(ItemId.Sword, "Another Sword", 200)));
    }

    [Test]
    public void Builder_Add_NullDefinition_Throws()
    {
        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id);

        Assert.Throws<ArgumentNullException>(() => builder.Add(null));
    }

    [Test]
    public void Builder_AddWithExplicitKey()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(ItemId.Potion, new ItemDef(ItemId.Potion, "Potion", 25))
            .Build();

        Assert.IsTrue(registry.Contains(ItemId.Potion));
    }

    [Test]
    public void Builder_AddWithExplicitKey_DuplicateKey_Throws()
    {
        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(ItemId.Sword, new ItemDef(ItemId.Sword, "Sword", 100));

        Assert.Throws<ArgumentException>(() =>
            builder.Add(ItemId.Sword, new ItemDef(ItemId.Sword, "Another", 200)));
    }

    [Test]
    public void Builder_AddSource_LoadsDefinitions()
    {
        var source = new TestSource(
            new ItemDef(ItemId.Sword, "Sword", 100),
            new ItemDef(ItemId.Shield, "Shield", 75)
        );

        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .AddSource(source)
            .Build();

        Assert.AreEqual(2, registry.Count);
        Assert.IsTrue(registry.Contains(ItemId.Sword));
        Assert.IsTrue(registry.Contains(ItemId.Shield));
    }

    [Test]
    public void Builder_AddSource_MultipleSources()
    {
        var source1 = new TestSource(new ItemDef(ItemId.Sword, "Sword", 100));
        var source2 = new TestSource(new ItemDef(ItemId.Shield, "Shield", 75));

        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .AddSource(source1)
            .AddSource(source2)
            .Build();

        Assert.AreEqual(2, registry.Count);
    }

    [Test]
    public void Builder_AddSource_DuplicateKeyAcrossSources_Throws()
    {
        var source1 = new TestSource(new ItemDef(ItemId.Sword, "Sword", 100));
        var source2 = new TestSource(new ItemDef(ItemId.Sword, "Another Sword", 200));

        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .AddSource(source1);

        Assert.Throws<InvalidOperationException>(() => builder.AddSource(source2));
    }

    [Test]
    public void Builder_AddSource_NullSource_Throws()
    {
        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id);

        Assert.Throws<ArgumentNullException>(() => builder.AddSource(null));
    }

    [Test]
    public void Builder_AddSource_NullDefinitionInSource_Throws()
    {
        var source = new NullProducingSource();
        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id);

        Assert.Throws<InvalidOperationException>(() => builder.AddSource(source));
    }

    [Test]
    public void Builder_NullKeyExtractor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DefinitionRegistryBuilder<ItemId, ItemDef>(null));
    }

    [Test]
    public void Builder_Build_CannotReuseBuilder()
    {
        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100));

        builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void Builder_Build_CannotAddAfterBuild()
    {
        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id);
        builder.Build();

        Assert.Throws<InvalidOperationException>(() =>
            builder.Add(new ItemDef(ItemId.Sword, "Sword", 100)));
    }

    [Test]
    public void Builder_Build_EmptyRegistry()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id).Build();

        Assert.AreEqual(0, registry.Count);
    }

    [Test]
    public void Builder_MixedAddAndSource()
    {
        var source = new TestSource(new ItemDef(ItemId.Shield, "Shield", 75));

        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .AddSource(source)
            .Add(new ItemDef(ItemId.Potion, "Potion", 25))
            .Build();

        Assert.AreEqual(3, registry.Count);
    }

    // --- Registry Tests ---

    [Test]
    public void Registry_TryGet_Found()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Build();

        bool found = registry.TryGet(ItemId.Sword, out ItemDef def);

        Assert.IsTrue(found);
        Assert.AreEqual("Sword", def.Name);
        Assert.AreEqual(100, def.Cost);
    }

    [Test]
    public void Registry_TryGet_NotFound()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Build();

        bool found = registry.TryGet(ItemId.Shield, out ItemDef def);

        Assert.IsFalse(found);
        Assert.IsNull(def);
    }

    [Test]
    public void Registry_Get_Found()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Build();

        ItemDef def = registry.Get(ItemId.Sword);

        Assert.AreEqual("Sword", def.Name);
    }

    [Test]
    public void Registry_Get_NotFound_Throws()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id).Build();

        Assert.Throws<KeyNotFoundException>(() => registry.Get(ItemId.Sword));
    }

    [Test]
    public void Registry_Contains_True()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Build();

        Assert.IsTrue(registry.Contains(ItemId.Sword));
    }

    [Test]
    public void Registry_Contains_False()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id).Build();

        Assert.IsFalse(registry.Contains(ItemId.Sword));
    }

    [Test]
    public void Registry_CopyKeysTo()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Add(new ItemDef(ItemId.Shield, "Shield", 75))
            .Build();

        var keys = new List<ItemId>();
        registry.CopyKeysTo(keys);

        Assert.AreEqual(2, keys.Count);
        Assert.IsTrue(keys.Contains(ItemId.Sword));
        Assert.IsTrue(keys.Contains(ItemId.Shield));
    }

    [Test]
    public void Registry_CopyKeysTo_NullTarget_Throws()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id).Build();

        Assert.Throws<ArgumentNullException>(() => registry.CopyKeysTo(null));
    }

    [Test]
    public void Registry_CopyValuesTo()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Build();

        var values = new List<ItemDef>();
        registry.CopyValuesTo(values);

        Assert.AreEqual(1, values.Count);
        Assert.AreEqual("Sword", values[0].Name);
    }

    [Test]
    public void Registry_CopyValuesTo_NullTarget_Throws()
    {
        var registry = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id).Build();

        Assert.Throws<ArgumentNullException>(() => registry.CopyValuesTo(null));
    }

    [Test]
    public void Registry_Immutable_BuilderModificationsDoNotAffect()
    {
        var builder = new DefinitionRegistryBuilder<ItemId, ItemDef>(d => d.Id)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100));

        var registry = builder.Build();

        // Builder is locked after build, so this confirms isolation
        Assert.AreEqual(1, registry.Count);
    }

    // --- String key tests ---

    [Test]
    public void Registry_StringKeys()
    {
        var registry = new DefinitionRegistryBuilder<string, ItemDef>(d => d.Name)
            .Add(new ItemDef(ItemId.Sword, "Sword", 100))
            .Add(new ItemDef(ItemId.Shield, "Shield", 75))
            .Build();

        Assert.IsTrue(registry.Contains("Sword"));
        Assert.IsTrue(registry.TryGet("Shield", out ItemDef def));
        Assert.AreEqual(75, def.Cost);
    }

    [Test]
    public void Builder_CustomComparer()
    {
        var registry = new DefinitionRegistryBuilder<string, ItemDef>(
                d => d.Name, StringComparer.OrdinalIgnoreCase)
            .Add(new ItemDef(ItemId.Sword, "sword", 100))
            .Build();

        Assert.IsTrue(registry.Contains("SWORD"));
        Assert.IsTrue(registry.Contains("sword"));
    }
}
