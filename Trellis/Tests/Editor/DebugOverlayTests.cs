using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Debugging;

public class DebugOverlayTests
{
    private DebugOverlay overlay;

    [SetUp]
    public void SetUp()
    {
        overlay = new DebugOverlay();
    }

    // --- Visibility ---

    [Test]
    public void InitialState_NotVisible()
    {
        Assert.IsFalse(overlay.IsVisible);
    }

    [Test]
    public void Toggle_MakesVisible()
    {
        overlay.Toggle();

        Assert.IsTrue(overlay.IsVisible);
    }

    [Test]
    public void Toggle_Twice_Hides()
    {
        overlay.Toggle();
        overlay.Toggle();

        Assert.IsFalse(overlay.IsVisible);
    }

    [Test]
    public void Toggle_InvokesCallback()
    {
        bool callbackValue = false;
        overlay.OnVisibilityChanged = v => callbackValue = v;

        overlay.Toggle();

        Assert.IsTrue(callbackValue);
    }

    [Test]
    public void SetVisible_True()
    {
        overlay.SetVisible(true);

        Assert.IsTrue(overlay.IsVisible);
    }

    [Test]
    public void SetVisible_SameValue_NoCallback()
    {
        int callCount = 0;
        overlay.OnVisibilityChanged = v => callCount++;

        overlay.SetVisible(false); // Already false

        Assert.AreEqual(0, callCount);
    }

    // --- Sections ---

    [Test]
    public void AddSection_IncreasesCount()
    {
        overlay.AddSection(new TestSection("Stats"));

        Assert.AreEqual(1, overlay.SectionCount);
    }

    [Test]
    public void AddSection_NullSection_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => overlay.AddSection(null));
    }

    [Test]
    public void RemoveSection_DecreasesCount()
    {
        var section = new TestSection("Stats");
        overlay.AddSection(section);

        overlay.RemoveSection(section);

        Assert.AreEqual(0, overlay.SectionCount);
    }

    [Test]
    public void GetSection_ByIndex()
    {
        var section = new TestSection("Stats");
        overlay.AddSection(section);

        Assert.AreSame(section, overlay.GetSection(0));
    }

    [Test]
    public void GetSection_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => overlay.GetSection(0));
    }

    [Test]
    public void CopyActiveSectionsTo_FiltersInactive()
    {
        overlay.AddSection(new TestSection("Active", active: true));
        overlay.AddSection(new TestSection("Inactive", active: false));
        overlay.AddSection(new TestSection("Also Active", active: true));

        var active = new List<IDebugSection>();
        overlay.CopyActiveSectionsTo(active);

        Assert.AreEqual(2, active.Count);
        Assert.AreEqual("Active", active[0].Title);
        Assert.AreEqual("Also Active", active[1].Title);
    }

    [Test]
    public void CopyActiveSectionsTo_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => overlay.CopyActiveSectionsTo(null));
    }

    // --- Commands ---

    [Test]
    public void RegisterCommand_IncreasesCount()
    {
        overlay.RegisterCommand("test", "A test command", args => "ok");

        Assert.AreEqual(1, overlay.CommandCount);
    }

    [Test]
    public void RegisterCommand_NullCommand_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => overlay.RegisterCommand(null));
    }

    [Test]
    public void ExecuteCommand_ValidCommand()
    {
        overlay.RegisterCommand("echo", "Echoes input", args => string.Join(" ", args));

        string result = overlay.ExecuteCommand("echo hello world");

        Assert.AreEqual("hello world", result);
    }

    [Test]
    public void ExecuteCommand_NoArgs()
    {
        overlay.RegisterCommand("status", "Shows status", args => "all good");

        string result = overlay.ExecuteCommand("status");

        Assert.AreEqual("all good", result);
    }

    [Test]
    public void ExecuteCommand_UnknownCommand_ReturnsError()
    {
        string result = overlay.ExecuteCommand("unknown");

        Assert.IsTrue(result.Contains("Unknown command"));
    }

    [Test]
    public void ExecuteCommand_CaseInsensitive()
    {
        overlay.RegisterCommand("test", "Test", args => "ok");

        string result = overlay.ExecuteCommand("TEST");

        Assert.AreEqual("ok", result);
    }

    [Test]
    public void ExecuteCommand_NullInput_ReturnsNull()
    {
        Assert.IsNull(overlay.ExecuteCommand(null));
    }

    [Test]
    public void ExecuteCommand_EmptyInput_ReturnsNull()
    {
        Assert.IsNull(overlay.ExecuteCommand(""));
    }

    [Test]
    public void ExecuteCommand_WhitespaceInput_ReturnsNull()
    {
        Assert.IsNull(overlay.ExecuteCommand("   "));
    }

    // --- Command Log ---

    [Test]
    public void ExecuteCommand_LogsInput()
    {
        overlay.RegisterCommand("test", "Test", args => "ok");

        overlay.ExecuteCommand("test arg1");

        Assert.AreEqual(2, overlay.LogEntryCount);
        Assert.AreEqual("> test arg1", overlay.GetLogEntry(0));
        Assert.AreEqual("ok", overlay.GetLogEntry(1));
    }

    [Test]
    public void ExecuteCommand_NullResult_NotLogged()
    {
        overlay.RegisterCommand("quiet", "Silent", args => null);

        overlay.ExecuteCommand("quiet");

        Assert.AreEqual(1, overlay.LogEntryCount); // Only the input line
    }

    [Test]
    public void ClearLog_EmptiesLog()
    {
        overlay.RegisterCommand("test", "Test", args => "ok");
        overlay.ExecuteCommand("test");

        overlay.ClearLog();

        Assert.AreEqual(0, overlay.LogEntryCount);
    }

    [Test]
    public void GetLogEntry_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => overlay.GetLogEntry(0));
    }

    // --- CopyCommandInfoTo ---

    [Test]
    public void CopyCommandInfoTo_ListsCommands()
    {
        overlay.RegisterCommand("echo", "Echoes input", args => "");
        overlay.RegisterCommand("set", "Sets a value", args => "");

        var info = new List<string>();
        overlay.CopyCommandInfoTo(info);

        Assert.AreEqual(2, info.Count);
    }

    [Test]
    public void CopyCommandInfoTo_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => overlay.CopyCommandInfoTo(null));
    }

    // --- Test Helpers ---

    private class TestSection : IDebugSection
    {
        public string Title { get; }
        public bool IsActive { get; }
        private readonly string content;

        public TestSection(string title, bool active = true, string content = "test content")
        {
            Title = title;
            IsActive = active;
            this.content = content;
        }

        public string Content()
        {
            return content;
        }
    }
}

public class DebugCommandTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var cmd = new DebugCommand("test", "A test", args => "ok");

        Assert.AreEqual("test", cmd.Name);
        Assert.AreEqual("A test", cmd.Description);
        Assert.IsNotNull(cmd.Handler);
    }

    [Test]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DebugCommand(null, "desc", args => "ok"));
    }

    [Test]
    public void Constructor_NullHandler_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DebugCommand("test", "desc", null));
    }

    [Test]
    public void Constructor_NullDescription_BecomesEmpty()
    {
        var cmd = new DebugCommand("test", null, args => "ok");

        Assert.AreEqual(string.Empty, cmd.Description);
    }
}
