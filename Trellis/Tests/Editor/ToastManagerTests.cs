using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.UI;

public class ToastManagerTests
{
    private ToastManager manager;

    [SetUp]
    public void SetUp()
    {
        manager = new ToastManager(3);
    }

    // --- Show ---

    [Test]
    public void Show_IncreasesVisibleCount()
    {
        manager.Show(new ToastRequest("Hello"));

        Assert.AreEqual(1, manager.VisibleCount);
    }

    [Test]
    public void Show_NullRequest_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => manager.Show(null));
    }

    [Test]
    public void Show_InvokesOnShowToast()
    {
        string shownMessage = null;
        manager.OnShowToast = (req, id) => shownMessage = req.Message;

        manager.Show(new ToastRequest("Hello"));

        Assert.AreEqual("Hello", shownMessage);
    }

    [Test]
    public void Show_AssignsUniqueIds()
    {
        var ids = new List<int>();
        manager.OnShowToast = (req, id) => ids.Add(id);

        manager.Show(new ToastRequest("A"));
        manager.Show(new ToastRequest("B"));

        Assert.AreEqual(2, ids.Count);
        Assert.AreNotEqual(ids[0], ids[1]);
    }

    // --- Max Visible ---

    [Test]
    public void Show_ExceedsMaxVisible_QueuedInstead()
    {
        manager.MaxVisible = 2;

        manager.Show(new ToastRequest("A"));
        manager.Show(new ToastRequest("B"));
        manager.Show(new ToastRequest("C"));

        Assert.AreEqual(2, manager.VisibleCount);
        Assert.AreEqual(1, manager.QueuedCount);
    }

    [Test]
    public void MaxVisible_SetToZero_BecomesOne()
    {
        manager.MaxVisible = 0;

        Assert.AreEqual(1, manager.MaxVisible);
    }

    // --- Tick and Auto-Dismiss ---

    [Test]
    public void Tick_DismissesExpiredToast()
    {
        manager.Show(new ToastRequest("Hello", duration: 1f));

        manager.Tick(1.1f);

        Assert.AreEqual(0, manager.VisibleCount);
    }

    [Test]
    public void Tick_DoesNotDismissUnexpiredToast()
    {
        manager.Show(new ToastRequest("Hello", duration: 2f));

        manager.Tick(1f);

        Assert.AreEqual(1, manager.VisibleCount);
    }

    [Test]
    public void Tick_DismissedToast_InvokesOnHideToast()
    {
        int hiddenId = -1;
        manager.OnHideToast = id => hiddenId = id;

        int shownId = -1;
        manager.OnShowToast = (req, id) => shownId = id;

        manager.Show(new ToastRequest("Hello", duration: 1f));
        manager.Tick(1.1f);

        Assert.AreEqual(shownId, hiddenId);
    }

    [Test]
    public void Tick_ExpiredToast_ShowsQueued()
    {
        manager.MaxVisible = 1;

        var messages = new List<string>();
        manager.OnShowToast = (req, id) => messages.Add(req.Message);

        manager.Show(new ToastRequest("A", duration: 1f));
        manager.Show(new ToastRequest("B", duration: 1f));

        Assert.AreEqual(1, messages.Count);

        manager.Tick(1.1f);

        Assert.AreEqual(2, messages.Count);
        Assert.AreEqual("B", messages[1]);
    }

    [Test]
    public void Tick_MultipleExpired()
    {
        manager.Show(new ToastRequest("A", duration: 1f));
        manager.Show(new ToastRequest("B", duration: 1f));
        manager.Show(new ToastRequest("C", duration: 2f));

        manager.Tick(1.1f);

        Assert.AreEqual(1, manager.VisibleCount);
    }

    // --- Manual Dismiss ---

    [Test]
    public void Dismiss_RemovesToast()
    {
        int shownId = -1;
        manager.OnShowToast = (req, id) => shownId = id;

        manager.Show(new ToastRequest("Hello"));
        manager.Dismiss(shownId);

        Assert.AreEqual(0, manager.VisibleCount);
    }

    [Test]
    public void Dismiss_ShowsQueuedToast()
    {
        manager.MaxVisible = 1;

        var ids = new List<int>();
        manager.OnShowToast = (req, id) => ids.Add(id);

        manager.Show(new ToastRequest("A"));
        manager.Show(new ToastRequest("B"));

        manager.Dismiss(ids[0]);

        Assert.AreEqual(1, manager.VisibleCount);
        Assert.AreEqual(0, manager.QueuedCount);
    }

    [Test]
    public void Dismiss_InvalidId_NoOp()
    {
        manager.Show(new ToastRequest("Hello"));

        // Should not throw
        manager.Dismiss(999);

        Assert.AreEqual(1, manager.VisibleCount);
    }

    // --- DismissAll ---

    [Test]
    public void DismissAll_ClearsAll()
    {
        manager.Show(new ToastRequest("A"));
        manager.Show(new ToastRequest("B"));
        manager.Show(new ToastRequest("C"));

        manager.DismissAll();

        Assert.AreEqual(0, manager.VisibleCount);
        Assert.AreEqual(0, manager.QueuedCount);
    }

    [Test]
    public void DismissAll_InvokesOnHideForAll()
    {
        int hideCount = 0;
        manager.OnHideToast = id => hideCount++;

        manager.Show(new ToastRequest("A"));
        manager.Show(new ToastRequest("B"));

        manager.DismissAll();

        Assert.AreEqual(2, hideCount);
    }

    // --- ToastRequest ---

    [Test]
    public void ToastRequest_DefaultValues()
    {
        var request = new ToastRequest("Hello");

        Assert.AreEqual("Hello", request.Message);
        Assert.AreEqual(3f, request.Duration);
        Assert.AreEqual(ToastPosition.Bottom, request.Position);
    }

    [Test]
    public void ToastRequest_CustomValues()
    {
        var request = new ToastRequest("Saved", 5f, ToastPosition.Top);

        Assert.AreEqual("Saved", request.Message);
        Assert.AreEqual(5f, request.Duration);
        Assert.AreEqual(ToastPosition.Top, request.Position);
    }

    [Test]
    public void ToastRequest_NullMessage_BecomesEmpty()
    {
        var request = new ToastRequest(null);

        Assert.AreEqual(string.Empty, request.Message);
    }

    [Test]
    public void ToastRequest_NegativeDuration_DefaultsTo3()
    {
        var request = new ToastRequest("Hello", -1f);

        Assert.AreEqual(3f, request.Duration);
    }
}
