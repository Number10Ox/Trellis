using System.Collections.Generic;
using NUnit.Framework;
using Trellis.Events;
using Trellis.UI;

public class RouteContextTests
{
    [Test]
    public void Parse_PathOnly()
    {
        var ctx = RouteContext.Parse("/menu");

        Assert.AreEqual("/menu", ctx.Path);
        Assert.AreEqual(0, ctx.ParameterCount);
    }

    [Test]
    public void Parse_PathWithSingleParam()
    {
        var ctx = RouteContext.Parse("/profile?userId=42");

        Assert.AreEqual("/profile", ctx.Path);
        Assert.AreEqual("42", ctx.GetParam("userId"));
    }

    [Test]
    public void Parse_PathWithMultipleParams()
    {
        var ctx = RouteContext.Parse("/inventory?itemId=42&tab=weapons");

        Assert.AreEqual("/inventory", ctx.Path);
        Assert.AreEqual("42", ctx.GetParam("itemId"));
        Assert.AreEqual("weapons", ctx.GetParam("tab"));
        Assert.AreEqual(2, ctx.ParameterCount);
    }

    [Test]
    public void Parse_EmptyRoute_DefaultsToRoot()
    {
        var ctx = RouteContext.Parse("");

        Assert.AreEqual("/", ctx.Path);
    }

    [Test]
    public void Parse_NullRoute_DefaultsToRoot()
    {
        var ctx = RouteContext.Parse(null);

        Assert.AreEqual("/", ctx.Path);
    }

    [Test]
    public void GetParam_NotFound_ReturnsNull()
    {
        var ctx = RouteContext.Parse("/menu");

        Assert.IsNull(ctx.GetParam("nonexistent"));
    }

    [Test]
    public void GetParamInt_ValidInt()
    {
        var ctx = RouteContext.Parse("/profile?userId=42");

        Assert.AreEqual(42, ctx.GetParamInt("userId"));
    }

    [Test]
    public void GetParamInt_NotFound_ReturnsDefault()
    {
        var ctx = RouteContext.Parse("/menu");

        Assert.AreEqual(0, ctx.GetParamInt("userId"));
        Assert.AreEqual(-1, ctx.GetParamInt("userId", -1));
    }

    [Test]
    public void GetParamInt_NotParseable_ReturnsDefault()
    {
        var ctx = RouteContext.Parse("/menu?count=abc");

        Assert.AreEqual(0, ctx.GetParamInt("count"));
    }

    [Test]
    public void HasParam_True()
    {
        var ctx = RouteContext.Parse("/menu?key=value");

        Assert.IsTrue(ctx.HasParam("key"));
    }

    [Test]
    public void HasParam_False()
    {
        var ctx = RouteContext.Parse("/menu");

        Assert.IsFalse(ctx.HasParam("key"));
    }

    [Test]
    public void Parse_ParamWithoutValue()
    {
        var ctx = RouteContext.Parse("/menu?debug");

        Assert.IsTrue(ctx.HasParam("debug"));
        Assert.AreEqual(string.Empty, ctx.GetParam("debug"));
    }
}

public class RouteTests
{
    [Test]
    public void Matches_ExactPath()
    {
        var route = new Route("/menu", new[] { "menuPanel" });

        Assert.IsTrue(route.Matches("/menu"));
    }

    [Test]
    public void Matches_DifferentPath()
    {
        var route = new Route("/menu", new[] { "menuPanel" });

        Assert.IsFalse(route.Matches("/settings"));
    }

    [Test]
    public void Matches_CaseSensitive()
    {
        var route = new Route("/Menu", new[] { "menuPanel" });

        Assert.IsFalse(route.Matches("/menu"));
    }

    [Test]
    public void Constructor_NullPath_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => new Route(null, new[] { "p1" }));
    }

    [Test]
    public void Constructor_NullPanelIds_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => new Route("/menu", null));
    }
}

public class UIRouterTests
{
    private EventBus eventBus;
    private UIRouter router;

    [SetUp]
    public void SetUp()
    {
        eventBus = new EventBus();
        router = new UIRouter(eventBus);
    }

    [Test]
    public void Constructor_NullEventBus_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => new UIRouter(null));
    }

    [Test]
    public void RegisterRoute_IncreasesCount()
    {
        router.RegisterRoute("/menu", "menuPanel");

        Assert.AreEqual(1, router.RouteCount);
        Assert.IsTrue(router.HasRoute("/menu"));
    }

    [Test]
    public void RegisterRoute_NullRoute_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() => router.RegisterRoute(null));
    }

    [Test]
    public void Navigate_ValidRoute_ReturnsTrue()
    {
        router.RegisterRoute("/menu", "menuPanel");

        bool result = router.Navigate("/menu");

        Assert.IsTrue(result);
        Assert.AreEqual("/menu", router.CurrentPath);
    }

    [Test]
    public void Navigate_UnknownRoute_ReturnsFalse()
    {
        bool result = router.Navigate("/unknown");

        Assert.IsFalse(result);
    }

    [Test]
    public void Navigate_NullRoute_ReturnsFalse()
    {
        bool result = router.Navigate(null);

        Assert.IsFalse(result);
    }

    [Test]
    public void Navigate_UpdatesObservable()
    {
        router.RegisterRoute("/menu", "menuPanel");
        string notified = null;
        router.CurrentRoute.Subscribe(r => notified = r);

        router.Navigate("/menu");

        Assert.AreEqual("/menu", notified);
    }

    [Test]
    public void Navigate_PublishesRouteChangedEvent()
    {
        router.RegisterRoute("/menu", "menuPanel");
        string eventPath = null;
        eventBus.Subscribe<RouteChangedEvent>(e => eventPath = e.Path);

        router.Navigate("/menu");

        Assert.AreEqual("/menu", eventPath);
    }

    [Test]
    public void Navigate_WithParams_PreservesFullRoute()
    {
        router.RegisterRoute("/profile", "profilePanel");

        router.Navigate("/profile?userId=42");

        Assert.AreEqual("/profile?userId=42", router.CurrentPath);
    }

    [Test]
    public void Navigate_CallsShowPanels()
    {
        router.RegisterRoute("/menu", "menuPanel", "headerPanel");
        string[] shownPanels = null;
        router.OnShowPanels = (ids, ctx) => shownPanels = ids;

        router.Navigate("/menu");

        Assert.AreEqual(2, shownPanels.Length);
        Assert.AreEqual("menuPanel", shownPanels[0]);
        Assert.AreEqual("headerPanel", shownPanels[1]);
    }

    [Test]
    public void Navigate_CallsRouteEnterOnPanels()
    {
        router.RegisterRoute("/menu", "menuPanel");
        var enterCalls = new List<string>();
        router.OnRouteEnterPanel = (id, ctx) => enterCalls.Add(id);

        router.Navigate("/menu");

        Assert.AreEqual(1, enterCalls.Count);
        Assert.AreEqual("menuPanel", enterCalls[0]);
    }

    [Test]
    public void Navigate_ExitsPreviousPanels()
    {
        router.RegisterRoute("/menu", "menuPanel");
        router.RegisterRoute("/settings", "settingsPanel");

        var exitCalls = new List<string>();
        router.OnRouteExitPanel = id => exitCalls.Add(id);

        router.Navigate("/menu");
        router.Navigate("/settings");

        Assert.AreEqual(1, exitCalls.Count);
        Assert.AreEqual("menuPanel", exitCalls[0]);
    }

    [Test]
    public void Navigate_HidesPreviousPanels()
    {
        router.RegisterRoute("/menu", "menuPanel");
        router.RegisterRoute("/settings", "settingsPanel");

        string[] hiddenPanels = null;
        router.OnHidePanels = ids => hiddenPanels = ids;

        router.Navigate("/menu");
        router.Navigate("/settings");

        Assert.AreEqual(1, hiddenPanels.Length);
        Assert.AreEqual("menuPanel", hiddenPanels[0]);
    }

    // --- History and Back Navigation ---

    [Test]
    public void Navigate_PushesToHistory()
    {
        router.RegisterRoute("/menu", "menuPanel");
        router.RegisterRoute("/settings", "settingsPanel");

        router.Navigate("/menu");
        router.Navigate("/settings");

        Assert.AreEqual(1, router.HistoryCount);
        Assert.IsTrue(router.CanGoBack);
    }

    [Test]
    public void Back_ReturnsToPreviousRoute()
    {
        router.RegisterRoute("/menu", "menuPanel");
        router.RegisterRoute("/settings", "settingsPanel");

        router.Navigate("/menu");
        router.Navigate("/settings");
        router.Back();

        Assert.AreEqual("/menu", router.CurrentPath);
        Assert.AreEqual(0, router.HistoryCount);
    }

    [Test]
    public void Back_EmptyHistory_ReturnsFalse()
    {
        Assert.IsFalse(router.Back());
    }

    [Test]
    public void Back_ExitsCurrentPanels()
    {
        router.RegisterRoute("/menu", "menuPanel");
        router.RegisterRoute("/settings", "settingsPanel");

        var exitCalls = new List<string>();
        router.OnRouteExitPanel = id => exitCalls.Add(id);

        router.Navigate("/menu");
        router.Navigate("/settings");
        exitCalls.Clear();

        router.Back();

        Assert.AreEqual(1, exitCalls.Count);
        Assert.AreEqual("settingsPanel", exitCalls[0]);
    }

    [Test]
    public void ClearHistory_EmptiesStack()
    {
        router.RegisterRoute("/menu", "menuPanel");
        router.RegisterRoute("/settings", "settingsPanel");

        router.Navigate("/menu");
        router.Navigate("/settings");
        router.ClearHistory();

        Assert.AreEqual(0, router.HistoryCount);
        Assert.IsFalse(router.CanGoBack);
    }

    // --- Deep Linking ---

    [Test]
    public void Navigate_DirectToDeepRoute()
    {
        router.RegisterRoute("/settings/audio", "audioPanel");

        bool result = router.Navigate("/settings/audio");

        Assert.IsTrue(result);
        Assert.AreEqual("/settings/audio", router.CurrentPath);
    }

    [Test]
    public void Navigate_MultipleSubRoutes()
    {
        router.RegisterRoute("/settings", "settingsPanel");
        router.RegisterRoute("/settings/audio", "audioPanel");
        router.RegisterRoute("/settings/display", "displayPanel");

        router.Navigate("/settings");
        router.Navigate("/settings/audio");

        Assert.AreEqual("/settings/audio", router.CurrentPath);
        Assert.AreEqual(1, router.HistoryCount);
    }
}
