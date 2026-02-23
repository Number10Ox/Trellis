using System;
using NUnit.Framework;
using Trellis.UI;

public class PopupManagerTests
{
    private PopupManager manager;

    [SetUp]
    public void SetUp()
    {
        manager = new PopupManager();
    }

    // --- Registration ---

    [Test]
    public void RegisterPopup_NullId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            manager.RegisterPopup(null, new TestPopup()));
    }

    [Test]
    public void RegisterPopup_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            manager.RegisterPopup("", new TestPopup()));
    }

    [Test]
    public void RegisterPopup_NullPopup_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            manager.RegisterPopup("confirm", null));
    }

    // --- Show ---

    [Test]
    public void Show_ShowsPopup()
    {
        var popup = new TestPopup();
        manager.RegisterPopup("confirm", popup);

        manager.Show(new PopupRequest("confirm"));

        Assert.IsTrue(popup.IsVisible);
        Assert.IsTrue(manager.HasActivePopup);
        Assert.AreEqual("confirm", manager.ActivePopupId);
    }

    [Test]
    public void Show_NullRequest_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => manager.Show(null));
    }

    [Test]
    public void Show_UnregisteredPopup_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            manager.Show(new PopupRequest("unknown")));
    }

    [Test]
    public void Show_Modal_SetsModalActive()
    {
        manager.RegisterPopup("confirm", new TestPopup());

        manager.Show(new PopupRequest("confirm", modal: true));

        Assert.IsTrue(manager.IsModalActive);
    }

    [Test]
    public void Show_NonModal_DoesNotSetModalActive()
    {
        manager.RegisterPopup("info", new TestPopup());

        manager.Show(new PopupRequest("info", modal: false));

        Assert.IsFalse(manager.IsModalActive);
    }

    [Test]
    public void Show_Modal_ShowsBackdrop()
    {
        manager.RegisterPopup("confirm", new TestPopup());
        bool backdropShown = false;
        manager.OnBackdropChanged = show => backdropShown = show;

        manager.Show(new PopupRequest("confirm", modal: true));

        Assert.IsTrue(backdropShown);
    }

    // --- Queue ---

    [Test]
    public void Show_WhileModalActive_QueuesPopup()
    {
        manager.RegisterPopup("confirm", new TestPopup());
        manager.RegisterPopup("reward", new TestPopup());

        manager.Show(new PopupRequest("confirm", modal: true));
        manager.Show(new PopupRequest("reward", modal: true));

        Assert.AreEqual(1, manager.QueueCount);
        Assert.AreEqual("confirm", manager.ActivePopupId);
    }

    [Test]
    public void Dismiss_ShowsNextInQueue()
    {
        var confirmPopup = new TestPopup();
        var rewardPopup = new TestPopup();
        manager.RegisterPopup("confirm", confirmPopup);
        manager.RegisterPopup("reward", rewardPopup);

        manager.Show(new PopupRequest("confirm", modal: true));
        manager.Show(new PopupRequest("reward", modal: true));

        manager.Dismiss(PopupResult.Confirm);

        Assert.IsFalse(confirmPopup.IsVisible);
        Assert.IsTrue(rewardPopup.IsVisible);
        Assert.AreEqual("reward", manager.ActivePopupId);
        Assert.AreEqual(0, manager.QueueCount);
    }

    // --- Dismiss ---

    [Test]
    public void Dismiss_HidesPopup()
    {
        var popup = new TestPopup();
        manager.RegisterPopup("confirm", popup);
        manager.Show(new PopupRequest("confirm"));

        manager.Dismiss(PopupResult.Confirm);

        Assert.IsFalse(popup.IsVisible);
        Assert.IsFalse(manager.HasActivePopup);
    }

    [Test]
    public void Dismiss_InvokesOnResult()
    {
        manager.RegisterPopup("confirm", new TestPopup());
        PopupResult receivedResult = null;

        manager.Show(new PopupRequest("confirm", onResult: r => receivedResult = r));
        manager.Dismiss(PopupResult.Confirm);

        Assert.IsNotNull(receivedResult);
        Assert.IsTrue(receivedResult.Confirmed);
    }

    [Test]
    public void Dismiss_NoActivePopup_NoOp()
    {
        // Should not throw
        manager.Dismiss(PopupResult.Cancel);
    }

    [Test]
    public void Dismiss_HidesBackdrop()
    {
        manager.RegisterPopup("confirm", new TestPopup());
        bool backdropVisible = false;
        manager.OnBackdropChanged = show => backdropVisible = show;

        manager.Show(new PopupRequest("confirm", modal: true));
        manager.Dismiss(PopupResult.Confirm);

        Assert.IsFalse(backdropVisible);
    }

    // --- DismissAll ---

    [Test]
    public void DismissAll_ClearsActiveAndQueue()
    {
        manager.RegisterPopup("confirm", new TestPopup());
        manager.RegisterPopup("reward", new TestPopup());

        manager.Show(new PopupRequest("confirm", modal: true));
        manager.Show(new PopupRequest("reward", modal: true));

        manager.DismissAll();

        Assert.IsFalse(manager.HasActivePopup);
        Assert.AreEqual(0, manager.QueueCount);
    }

    [Test]
    public void DismissAll_InvokesOnResultForAll()
    {
        manager.RegisterPopup("confirm", new TestPopup());
        manager.RegisterPopup("reward", new TestPopup());

        int cancelCount = 0;
        manager.Show(new PopupRequest("confirm", modal: true, onResult: r => cancelCount++));
        manager.Show(new PopupRequest("reward", modal: true, onResult: r => cancelCount++));

        manager.DismissAll();

        Assert.AreEqual(2, cancelCount);
    }

    // --- PopupResult ---

    [Test]
    public void PopupResult_Confirm_IsConfirmed()
    {
        Assert.IsTrue(PopupResult.Confirm.Confirmed);
    }

    [Test]
    public void PopupResult_Cancel_IsNotConfirmed()
    {
        Assert.IsFalse(PopupResult.Cancel.Confirmed);
    }

    [Test]
    public void PopupResult_WithData()
    {
        var result = new PopupResult(true, "selectedOption");

        Assert.IsTrue(result.Confirmed);
        Assert.AreEqual("selectedOption", result.Data);
    }

    // --- Test Helpers ---

    private class TestPopup : IPopup
    {
        public bool IsVisible { get; private set; }

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
