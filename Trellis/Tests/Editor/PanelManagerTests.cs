using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.UI;

public class PanelManagerTests
{
    private PanelManager manager;

    [SetUp]
    public void SetUp()
    {
        manager = new PanelManager();
    }

    // --- Registration ---

    [Test]
    public void Register_IncreasesCount()
    {
        var panel = new TestPanel("p1");
        manager.Register(panel, new PanelDescriptor("p1", LayoutZone.Center));

        Assert.AreEqual(1, manager.PanelCount);
    }

    [Test]
    public void Register_NullPanel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            manager.Register(null, new PanelDescriptor("p1", LayoutZone.Center)));
    }

    [Test]
    public void Register_NullDescriptor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            manager.Register(new TestPanel("p1"), null));
    }

    [Test]
    public void Register_MismatchedIds_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            manager.Register(new TestPanel("p1"), new PanelDescriptor("p2", LayoutZone.Center)));
    }

    [Test]
    public void Register_DuplicateId_Throws()
    {
        manager.Register(new TestPanel("p1"), new PanelDescriptor("p1", LayoutZone.Center));

        Assert.Throws<ArgumentException>(() =>
            manager.Register(new TestPanel("p1"), new PanelDescriptor("p1", LayoutZone.Center)));
    }

    // --- Unregister ---

    [Test]
    public void Unregister_RemovesPanel()
    {
        manager.Register(new TestPanel("p1"), new PanelDescriptor("p1", LayoutZone.Center));

        manager.Unregister("p1");

        Assert.AreEqual(0, manager.PanelCount);
        Assert.IsFalse(manager.HasPanel("p1"));
    }

    [Test]
    public void Unregister_HidesVisiblePanel()
    {
        var panel = new TestPanel("p1");
        manager.Register(panel, new PanelDescriptor("p1", LayoutZone.Center));
        manager.ShowPanel("p1");

        manager.Unregister("p1");

        Assert.IsFalse(panel.IsVisible);
    }

    // --- Show / Hide ---

    [Test]
    public void ShowPanel_MakesVisible()
    {
        var panel = new TestPanel("p1");
        manager.Register(panel, new PanelDescriptor("p1", LayoutZone.Center));

        manager.ShowPanel("p1");

        Assert.IsTrue(panel.IsVisible);
    }

    [Test]
    public void HidePanel_MakesInvisible()
    {
        var panel = new TestPanel("p1");
        manager.Register(panel, new PanelDescriptor("p1", LayoutZone.Center));
        manager.ShowPanel("p1");

        manager.HidePanel("p1");

        Assert.IsFalse(panel.IsVisible);
    }

    [Test]
    public void ShowPanels_Multiple()
    {
        var p1 = new TestPanel("p1");
        var p2 = new TestPanel("p2");
        manager.Register(p1, new PanelDescriptor("p1", LayoutZone.Center));
        manager.Register(p2, new PanelDescriptor("p2", LayoutZone.Top));

        manager.ShowPanels(new[] { "p1", "p2" });

        Assert.IsTrue(p1.IsVisible);
        Assert.IsTrue(p2.IsVisible);
    }

    [Test]
    public void HidePanels_Multiple()
    {
        var p1 = new TestPanel("p1");
        var p2 = new TestPanel("p2");
        manager.Register(p1, new PanelDescriptor("p1", LayoutZone.Center));
        manager.Register(p2, new PanelDescriptor("p2", LayoutZone.Top));
        manager.ShowPanels(new[] { "p1", "p2" });

        manager.HidePanels(new[] { "p1", "p2" });

        Assert.IsFalse(p1.IsVisible);
        Assert.IsFalse(p2.IsVisible);
    }

    [Test]
    public void HideAll_HidesEverything()
    {
        var p1 = new TestPanel("p1");
        var p2 = new TestPanel("p2");
        manager.Register(p1, new PanelDescriptor("p1", LayoutZone.Center));
        manager.Register(p2, new PanelDescriptor("p2", LayoutZone.Top));
        manager.ShowPanels(new[] { "p1", "p2" });

        manager.HideAll();

        Assert.IsFalse(p1.IsVisible);
        Assert.IsFalse(p2.IsVisible);
    }

    // --- Zone Management ---

    [Test]
    public void PanelCountInZone_CorrectPerZone()
    {
        manager.Register(new TestPanel("p1"), new PanelDescriptor("p1", LayoutZone.Center));
        manager.Register(new TestPanel("p2"), new PanelDescriptor("p2", LayoutZone.Center));
        manager.Register(new TestPanel("p3"), new PanelDescriptor("p3", LayoutZone.Top));

        Assert.AreEqual(2, manager.PanelCountInZone(LayoutZone.Center));
        Assert.AreEqual(1, manager.PanelCountInZone(LayoutZone.Top));
        Assert.AreEqual(0, manager.PanelCountInZone(LayoutZone.Bottom));
    }

    [Test]
    public void CopyPanelIdsInZone_SortedBySortOrder()
    {
        manager.Register(new TestPanel("high"), new PanelDescriptor("high", LayoutZone.Center, 10));
        manager.Register(new TestPanel("low"), new PanelDescriptor("low", LayoutZone.Center, 1));
        manager.Register(new TestPanel("mid"), new PanelDescriptor("mid", LayoutZone.Center, 5));

        var ids = new List<string>();
        manager.CopyPanelIdsInZone(LayoutZone.Center, ids);

        Assert.AreEqual(3, ids.Count);
        Assert.AreEqual("low", ids[0]);
        Assert.AreEqual("mid", ids[1]);
        Assert.AreEqual("high", ids[2]);
    }

    [Test]
    public void CopyPanelIdsInZone_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            manager.CopyPanelIdsInZone(LayoutZone.Center, null));
    }

    // --- Lookup ---

    [Test]
    public void HasPanel_True()
    {
        manager.Register(new TestPanel("p1"), new PanelDescriptor("p1", LayoutZone.Center));

        Assert.IsTrue(manager.HasPanel("p1"));
    }

    [Test]
    public void HasPanel_False()
    {
        Assert.IsFalse(manager.HasPanel("p1"));
    }

    [Test]
    public void GetPanel_Found()
    {
        var panel = new TestPanel("p1");
        manager.Register(panel, new PanelDescriptor("p1", LayoutZone.Center));

        Assert.AreSame(panel, manager.GetPanel("p1"));
    }

    [Test]
    public void GetPanel_NotFound_ReturnsNull()
    {
        Assert.IsNull(manager.GetPanel("nonexistent"));
    }

    [Test]
    public void GetDescriptor_Found()
    {
        manager.Register(new TestPanel("p1"), new PanelDescriptor("p1", LayoutZone.Top, 5));

        var desc = manager.GetDescriptor("p1");

        Assert.AreEqual(LayoutZone.Top, desc.Zone);
        Assert.AreEqual(5, desc.SortOrder);
    }

    // --- Test Helpers ---

    private class TestPanel : IPanel
    {
        public string PanelId { get; }
        public bool IsVisible { get; private set; }

        public TestPanel(string panelId)
        {
            PanelId = panelId;
        }

        public void Show()
        {
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }
    }
}
