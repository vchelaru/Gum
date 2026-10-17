using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Second spike for the epic on running several GumService instances in one process (#6066).
/// Each service loads a real shipped Forms theme project (Bubblegum and Hazard, the same folders
/// "Add Forms" imports) and builds that project's own demo screen with real Forms controls (Button,
/// CheckBox, ListBox, Slider, TextBox, ...). Service B's screen is shifted so the two overlap only
/// partly, and the screens are drawn twice into one render target, once A then B and once B then A.
/// The tests assert the behavior we want, so a red test marks a collision between the two services.
/// Captures are saved as PNGs in <see cref="CaptureDirectory"/>.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class MultiGumServiceThemesSpikeTests : BaseTestClass
{
    private const int CaptureWidth = 1024;
    private const int CaptureHeight = 768;

    // Service B's whole screen is shifted by this much. The demo screens are ~775x756, so this keeps
    // both fully inside the capture while the two overlap in a large region and each still has an
    // area the other does not cover (A's left strip and window title; B's dialog box and window).
    private static readonly Point OffsetB = new Point(240, 12);

    // Theme accent colors, from each theme's Components/<Theme>/Styles.gucx (Accent swatch).
    private static readonly XnaColor BubblegumAccent = new XnaColor(255, 107, 157);
    private static readonly XnaColor HazardAccent = new XnaColor(244, 200, 26);

    // Panel fills, from each theme's Screens/<Theme>/DemoScreenGum.gusx (Background.Fill*).
    private static readonly XnaColor BubblegumPanel = new XnaColor(255, 231, 242);
    private static readonly XnaColor HazardPanel = new XnaColor(18, 16, 7);

    private static readonly XnaColor ClearColor = XnaColor.CornflowerBlue;

    private readonly string _rootDirectory;
    private readonly StringBuilder _log = new StringBuilder();

    /// <summary>Where the PNG captures are written (next to the test binaries).</summary>
    public static string CaptureDirectory { get; } =
        Path.Combine(AppContext.BaseDirectory, "MultiServiceCaptures");

    public MultiGumServiceThemesSpikeTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "GumTwoThemes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(CaptureDirectory);
    }

    public override void Dispose()
    {
        base.Dispose();

        // A throw inside ToGraphicalUiElement leaves layout suspended for the whole process (found in
        // the first spike), which would make every later test draw nothing.
        GraphicalUiElement.IsAllLayoutSuspended = false;

        try { File.AppendAllText(Path.Combine(CaptureDirectory, "run-log.txt"), _log.ToString()); } catch { /* best effort */ }
        try { Directory.Delete(_rootDirectory, recursive: true); } catch { /* best effort */ }
    }

    #region Harness checks (a lone service must render its own theme)

    [Theory]
    [InlineData("Bubblegum")]
    [InlineData("Hazard")]
    public void SingleService_Alone_ShouldRenderItsOwnTheme(string theme)
    {
        string path = CopyTheme(theme);
        using ThemedGame game = new ThemedGame(path, pathB: null);
        game.Initialize();

        DemoScreen screen = BuildDemoScreen(game.ServiceA, game.ProjectA, theme, offset: Point.Zero);
        Log($"{theme} solo: {screen.Describe()}");
        AssertScreenBuilt(screen, game.ProjectA);

        Frame frame = DrawInto(game, new[] { game.ServiceA }, makeServiceDefault: true,
            pngName: $"solo-{theme}.png");

        AssertOwnTheme(frame, otherSolo: null, theme, theme + " (solo)");
    }

    #endregion

    #region Two services

    [Fact]
    public void TwoThemedServices_ShouldRenderEachThemeInBothDrawOrders()
    {
        using ThemedGame game = CreateGame();

        // Draws go straight through each renderer under whichever service is the default. The message
        // says which collision hit first.
        DemoScreen screenA;
        DemoScreen screenB;
        try
        {
            screenA = BuildDemoScreen(game.ServiceA, game.ProjectA, "Bubblegum", offset: Point.Zero);
            screenB = BuildDemoScreen(game.ServiceB, game.ProjectB, "Hazard", OffsetB);
        }
        catch (Exception e)
        {
            Log("build threw: " + e);
            Assert.Fail($"Building a demo screen throws. {e.GetType().Name}: {e.Message}");
            return;
        }
        Log($"two services A: {screenA.Describe()}");
        Log($"two services B: {screenB.Describe()}");
        SoftChecks soft = new SoftChecks();
        soft.Check(() => AssertScreenBuilt(screenA, game.ProjectA));
        soft.Check(() => AssertScreenBuilt(screenB, game.ProjectB));

        Frame soloA = DrawInto(game, new[] { game.ServiceA }, makeServiceDefault: false, pngName: "twoservices-solo-A-bubblegum.png");
        Frame soloB = DrawInto(game, new[] { game.ServiceB }, makeServiceDefault: false, pngName: "twoservices-solo-B-hazard.png");
        Frame aThenB = DrawInto(game, new[] { game.ServiceA, game.ServiceB },
            makeServiceDefault: false, pngName: "twoservices-1-A-then-B.png");
        Frame bThenA = DrawInto(game, new[] { game.ServiceB, game.ServiceA },
            makeServiceDefault: false, pngName: "twoservices-2-B-then-A.png");

        AssertBothCaptures(soft, soloA, soloB, aThenB, bThenA);
        soft.Verify();
    }

    // B.Uninitialize() disposes the shared LoaderManager content and runs the shape package's
    // teardown hook, both process-wide, while A is still live and has its theme's sprite sheet and
    // fonts loaded.
    [Fact]
    public void UninitializingSecondThemedService_ShouldLeaveFirstServiceAbleToDraw()
    {
        using ThemedGame game = CreateGame();

        BuildDemoScreen(game.ServiceA, game.ProjectA, "Bubblegum", offset: Point.Zero);

        game.ServiceB.Uninitialize();

        Frame frame;
        try
        {
            frame = DrawInto(game, new[] { game.ServiceA }, makeServiceDefault: true, pngName: "uninit-B-then-draw-A.png");
        }
        catch (Exception e)
        {
            Log("draw A after B.Uninitialize threw: " + e);
            Assert.Fail($"Drawing A after B.Uninitialize() throws {e.GetType().Name}: {e.Message}");
            return;
        }

        AssertOwnTheme(frame, otherSolo: null, "Bubblegum", "A after B.Uninitialize()");
    }

    #endregion

    #region Assertions

    private void AssertBothCaptures(SoftChecks soft, Frame soloA, Frame soloB, Frame aThenB, Frame bThenA)
    {
        // 1. Each capture shows each theme in the area the other screen does not cover.
        soft.Check(() => AssertOwnTheme(aThenB, otherSolo: soloB, "Bubblegum", "A (Bubblegum) in capture A-then-B"));
        soft.Check(() => AssertOwnTheme(aThenB, otherSolo: soloA, "Hazard", "B (Hazard) in capture A-then-B"));
        soft.Check(() => AssertOwnTheme(bThenA, otherSolo: soloB, "Bubblegum", "A (Bubblegum) in capture B-then-A"));
        soft.Check(() => AssertOwnTheme(bThenA, otherSolo: soloA, "Hazard", "B (Hazard) in capture B-then-A"));

        // 2. Where the screens overlap, whichever service drew last is on top.
        soft.Check(() =>
        {
            Point probe = FindOverlapProbe(soloA, soloB);
            XnaColor panelA = soloA.At(probe);
            XnaColor panelB = soloB.At(probe);
            Log($"overlap probe {probe}: A alone {Rgb(panelA)}, B alone {Rgb(panelB)}");
            Near(aThenB.At(probe), panelB, 2).ShouldBeTrue(
                $"A-then-B: B draws last, so B's Hazard panel should be on top at {probe}: was {Rgb(aThenB.At(probe))}, expected {Rgb(panelB)} (A alone there: {Rgb(panelA)}). {Where(aThenB)}");
            Near(bThenA.At(probe), panelA, 2).ShouldBeTrue(
                $"B-then-A: A draws last, so A's Bubblegum panel should be on top at {probe}: was {Rgb(bThenA.At(probe))}, expected {Rgb(panelA)} (B alone there: {Rgb(panelB)}). {Where(bThenA)}");
        });
        soft.Check(() =>
        {
            double bOnTop = FractionEqualInOverlap(aThenB, later: soloB, other: soloA);
            bOnTop.ShouldBeGreaterThan(0.9, $"A-then-B: B drew last, so across the overlap the capture should match B drawn alone; only {bOnTop:P1} does. {Where(aThenB)}");
        });
        soft.Check(() =>
        {
            double aOnTop = FractionEqualInOverlap(bThenA, later: soloA, other: soloB);
            aOnTop.ShouldBeGreaterThan(0.9, $"B-then-A: A drew last, so across the overlap the capture should match A drawn alone; only {aOnTop:P1} does. {Where(bThenA)}");
        });

        // 3. Wherever only one screen draws, the capture equals that service drawn alone, in both
        //    draw orders: drawing a second service did not change the first one's output.
        foreach ((string label, Frame composite) in new[] { ("A-then-B", aThenB), ("B-then-A", bThenA) })
        {
            soft.Check(() =>
            {
                int differentInA = CountDifferencesWhereOtherIsClear(composite, soloA, soloB);
                differentInA.ShouldBe(0, $"{label}: {differentInA} pixels only A draws differ from A drawn alone. {Where(composite)}");
            });
            soft.Check(() =>
            {
                int differentInB = CountDifferencesWhereOtherIsClear(composite, soloB, soloA);
                differentInB.ShouldBe(0, $"{label}: {differentInB} pixels only B draws differ from B drawn alone. {Where(composite)}");
            });
        }
    }

    /// <summary>
    /// Runs each check and keeps going after a failed one, so a red test reports every collision it
    /// hit instead of only the first.
    /// </summary>
    private sealed class SoftChecks
    {
        private readonly List<string> _failures = new List<string>();

        public void Check(Action check)
        {
            try
            {
                check();
            }
            catch (Exception e) when (e is ShouldAssertException || e is InvalidOperationException)
            {
                _failures.Add(System.Text.RegularExpressions.Regex.Replace(e.Message, @"\s+", " ").Trim());
            }
        }

        public void Verify()
        {
            if (_failures.Count > 0)
            {
                Assert.Fail($"{_failures.Count} check(s) failed:{Environment.NewLine}- " + string.Join(Environment.NewLine + "- ", _failures));
            }
        }
    }

    /// <summary>
    /// In the pixels the other screen leaves untouched (it is clear there when drawn alone), the
    /// capture contains this theme's accent and panel colors and none of the other theme's. With no
    /// <paramref name="otherSolo"/> the whole frame is checked.
    /// </summary>
    private static void AssertOwnTheme(Frame frame, Frame? otherSolo, string theme, string label)
    {
        ThemeColorsFor(theme, out XnaColor accent, out XnaColor panel, out XnaColor otherAccent, out XnaColor otherPanel);

        int ownAccent = CountWhereOtherIsClear(frame, otherSolo, accent);
        int ownPanel = CountWhereOtherIsClear(frame, otherSolo, panel);
        int foreignAccent = CountWhereOtherIsClear(frame, otherSolo, otherAccent);
        int foreignPanel = CountWhereOtherIsClear(frame, otherSolo, otherPanel);
        string counts = $"in the area only this screen covers: own accent {Rgb(accent)} {ownAccent} px, own panel {Rgb(panel)} {ownPanel} px, " +
            $"other theme's accent {Rgb(otherAccent)} {foreignAccent} px, other theme's panel {Rgb(otherPanel)} {foreignPanel} px.";

        ownAccent.ShouldBeGreaterThan(300, $"{label}: should show its theme's accent color; {counts} {Where(frame)}");
        ownPanel.ShouldBeGreaterThan(1000, $"{label}: should show its theme's panel color; {counts} {Where(frame)}");
        foreignAccent.ShouldBe(0, $"{label}: should show none of the other theme's accent color; {counts} {Where(frame)}");
        foreignPanel.ShouldBe(0, $"{label}: should show none of the other theme's panel color; {counts} {Where(frame)}");
    }

    /// <summary>
    /// The demo screen is the real shipped one: its named controls exist and each is backed by its real
    /// Forms control. A screen built from the wrong project (or none) fails here with the cause.
    /// </summary>
    private static void AssertScreenBuilt(DemoScreen screen, GumProjectSave expectedProject)
    {
        bool projectIsCurrent = ReferenceEquals(Gum.Managers.ObjectFinder.Self.GumProjectSave, expectedProject);
        string context = $"{screen.Theme} demo screen has {screen.DescendantCount} descendants; ObjectFinder.Self.GumProjectSave is " +
            (projectIsCurrent ? "this theme's project" : "NOT this theme's project (the last project loaded wins)");

        (Type expected, string name)[] expectations =
        {
            (typeof(Button), "ButtonConfirmInstance"),
            (typeof(CheckBox), "FullScreenCheckbox"),
            (typeof(Slider), "MusicSlider"),
            (typeof(ListBox), "ResolutionBox"),
            (typeof(TextBox), "TextBoxInstance"),
        };
        foreach ((Type expected, string name) in expectations)
        {
            screen.FormsControlTypes.TryGetValue(name, out Type? actual);
            actual.ShouldBe(expected, $"{screen.Theme} demo: {name} should be a real Forms {expected.Name}. {context}");
        }
    }

    /// <summary>
    /// A pixel where, drawn alone, A shows plain Bubblegum panel and B shows plain Hazard panel, so
    /// the winner of the overlap is unambiguous.
    /// </summary>
    private static Point FindOverlapProbe(Frame soloA, Frame soloB)
    {
        for (int y = 0; y < CaptureHeight; y += 2)
        {
            for (int x = 0; x < CaptureWidth; x += 2)
            {
                if (Near(soloA.At(x, y), BubblegumPanel, 2) && Near(soloB.At(x, y), HazardPanel, 2))
                {
                    return new Point(x, y);
                }
            }
        }
        throw new InvalidOperationException("no pixel is plain Bubblegum panel when A is drawn alone and plain Hazard panel when B is; " +
            "the screens do not overlap on panel. " + Where(soloA) + " " + Where(soloB));
    }

    private static string Where(Frame frame) => "Capture: " + frame.PngPath;

    #endregion

    #region Building and drawing

    private ThemedGame CreateGame()
    {
        string pathA = CopyTheme("Bubblegum");
        string pathB = CopyTheme("Hazard");
        ThemedGame game = new ThemedGame(pathA, pathB);
        game.Initialize();
        return game;
    }

    private DemoScreen BuildDemoScreen(Gum.GumService service, GumProjectSave project, string theme, Point offset)
    {
        string screenName = theme + "/DemoScreenGum";
        ScreenSave screenSave = project.Screens.First(item => item.Name == screenName);
        // Building for a service means making it the default first; nothing switches implicitly.
        global::RenderingLibrary.IGumService.Default = service;
        GraphicalUiElement screen = screenSave.ToGraphicalUiElement(service.SystemManagers, addToManagers: true);
        screen.X += offset.X;
        screen.Y += offset.Y;
        screen.UpdateLayout();
        return new DemoScreen(theme, screen);
    }

    /// <summary>
    /// Draws the services in order into one capture. With <paramref name="makeServiceDefault"/> each becomes
    /// the default right before its draw; without it every draw runs under the current default.
    /// </summary>
    private Frame DrawInto(ThemedGame game, Gum.GumService[] order, bool makeServiceDefault, string pngName)
    {
        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new RenderTarget2D(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        // Pass 0 lets first-draw work (lazy fonts, sprite sheet, shape batch) settle; pass 1 is the capture.
        for (int pass = 0; pass < 2; pass++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(ClearColor);
            foreach (Gum.GumService service in order)
            {
                // Renderer.Draw leaves no render target bound, so rebind before each service's pass.
                gd.SetRenderTarget(capture);
                if (makeServiceDefault) { IGumService.Default = service; }
                service.SystemManagers.Renderer.Draw(service.SystemManagers);
            }
        }

        gd.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);

        string pngPath = Path.Combine(CaptureDirectory, pngName);
        using (FileStream stream = File.Create(pngPath))
        {
            capture.SaveAsPng(stream, CaptureWidth, CaptureHeight);
        }
        Log("wrote " + pngPath);
        return new Frame(pixels, pngPath);
    }

    private void Log(string line) => _log.AppendLine(line);

    #endregion

    #region Pixel helpers

    private static void ThemeColorsFor(string theme, out XnaColor accent, out XnaColor panel, out XnaColor otherAccent, out XnaColor otherPanel)
    {
        bool bubblegum = theme == "Bubblegum";
        accent = bubblegum ? BubblegumAccent : HazardAccent;
        panel = bubblegum ? BubblegumPanel : HazardPanel;
        otherAccent = bubblegum ? HazardAccent : BubblegumAccent;
        otherPanel = bubblegum ? HazardPanel : BubblegumPanel;
    }

    private static bool Near(XnaColor a, XnaColor b, int tolerance) =>
        Math.Abs(a.R - b.R) <= tolerance && Math.Abs(a.G - b.G) <= tolerance && Math.Abs(a.B - b.B) <= tolerance;

    private static string Rgb(XnaColor c) => $"({c.R},{c.G},{c.B})";

    private static bool IsClear(XnaColor c) => Near(c, ClearColor, 0);

    private static int CountWhereOtherIsClear(Frame frame, Frame? otherSolo, XnaColor color)
    {
        int count = 0;
        for (int y = 0; y < CaptureHeight; y++)
        {
            for (int x = 0; x < CaptureWidth; x++)
            {
                if (otherSolo != null && !IsClear(otherSolo.At(x, y))) { continue; }
                if (Near(frame.At(x, y), color, 6)) { count++; }
            }
        }
        return count;
    }

    private static int CountDifferencesWhereOtherIsClear(Frame composite, Frame solo, Frame otherSolo)
    {
        int count = 0;
        for (int y = 0; y < CaptureHeight; y++)
        {
            for (int x = 0; x < CaptureWidth; x++)
            {
                if (!IsClear(otherSolo.At(x, y))) { continue; }
                if (!Near(composite.At(x, y), solo.At(x, y), 0)) { count++; }
            }
        }
        return count;
    }

    // Of the pixels both screens draw on (neither is clear when drawn alone), the share where the
    // capture equals what the later-drawn service paints alone.
    private static double FractionEqualInOverlap(Frame composite, Frame later, Frame other)
    {
        int overlap = 0;
        int equal = 0;
        for (int y = 0; y < CaptureHeight; y++)
        {
            for (int x = 0; x < CaptureWidth; x++)
            {
                if (IsClear(later.At(x, y)) || IsClear(other.At(x, y))) { continue; }
                overlap++;
                if (Near(composite.At(x, y), later.At(x, y), 2)) { equal++; }
            }
        }
        return overlap == 0 ? 0 : (double)equal / overlap;
    }

    private sealed class Frame
    {
        private readonly XnaColor[] _pixels;
        public string PngPath { get; }
        public Frame(XnaColor[] pixels, string pngPath) { _pixels = pixels; PngPath = pngPath; }
        public XnaColor At(int x, int y) => _pixels[(y * CaptureWidth) + x];
        public XnaColor At(Point point) => At(point.X, point.Y);
    }

    /// <summary>A built demo screen and what the assertions need to know about it.</summary>
    private sealed class DemoScreen
    {
        public string Theme { get; }
        public GraphicalUiElement Root { get; }
        public int DescendantCount { get; }

        /// <summary>The Forms control type behind each named demo control (null when missing or not a Forms control).</summary>
        public Dictionary<string, Type?> FormsControlTypes { get; } = new();

        public DemoScreen(string theme, GraphicalUiElement root)
        {
            Theme = theme;
            Root = root;
            DescendantCount = CountDescendants(root);

            foreach (string name in new[] { "ButtonConfirmInstance", "FullScreenCheckbox", "MusicSlider", "ResolutionBox", "TextBoxInstance" })
            {
                FormsControlTypes[name] = (root.GetGraphicalUiElementByName(name) as InteractiveGue)?.FormsControlAsObject?.GetType();
            }
        }

        private static int CountDescendants(GraphicalUiElement element)
        {
            int count = 0;
            foreach (GraphicalUiElement child in element.Children.OfType<GraphicalUiElement>())
            {
                count += 1 + CountDescendants(child);
            }
            return count;
        }

        public string Describe()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"{Theme}: {DescendantCount} descendants");
            foreach (KeyValuePair<string, Type?> pair in FormsControlTypes) { builder.Append($", {pair.Key}->{pair.Value?.Name ?? "null"}"); }
            return builder.ToString();
        }
    }

    #endregion

    #region Theme project on disk

    /// <summary>
    /// Copies a shipped Forms theme (Tools/Gum.ProjectServices/Templates/FormsThemes/&lt;theme&gt;) and the
    /// shared FormsBehaviors it references into a temp folder, laid out the way the repo has them, so the
    /// theme's relative ../../FormsBehaviors paths still resolve. Returns the .gumx path.
    /// </summary>
    private string CopyTheme(string theme)
    {
        string templates = FindTemplatesDirectory();
        string sharedBehaviors = Path.Combine(_rootDirectory, "FormsBehaviors");
        if (!Directory.Exists(sharedBehaviors))
        {
            CopyDirectory(Path.Combine(templates, "FormsBehaviors"), sharedBehaviors);
        }

        string destination = Path.Combine(_rootDirectory, "FormsThemes", theme);
        CopyDirectory(Path.Combine(templates, "FormsThemes", theme), destination);
        return Path.Combine(destination, "GumProject.gumx");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static string FindTemplatesDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate Tools/Gum.ProjectServices/Templates from " + AppContext.BaseDirectory);
    }

    #endregion

    #region Test game

    private sealed class ThemedGame : IDisposable
    {
        private readonly MultiServiceGameLease _lease;
        private readonly string _pathA;
        private readonly string? _pathB;

        public Gum.GumService ServiceA { get; }
        public Gum.GumService ServiceB { get; }
        public GumProjectSave ProjectA { get; private set; } = null!;
        public GumProjectSave ProjectB { get; private set; } = null!;

        public GraphicsDevice GraphicsDevice => _lease.GraphicsDevice;

        public ThemedGame(string pathA, string? pathB)
        {
            _lease = new MultiServiceGameLease();
            _pathA = pathA;
            _pathB = pathB;
            ServiceA = new Gum.GumService();
            ServiceB = new Gum.GumService();
        }

        public void Initialize()
        {
            ShapeRenderer.Self.Initialize(GraphicsDevice, _lease.Game.Content);

            ProjectA = ServiceA.Initialize(_lease.Game, _pathA)!;
            if (_pathB != null)
            {
                ProjectB = ServiceB.Initialize(_lease.Game, _pathB)!;
            }
        }

        public void Dispose()
        {
            if (ServiceB.IsInitialized)
            {
                try { ServiceB.Uninitialize(); } catch { /* teardown after a red test */ }
            }
            if (ServiceA.IsInitialized)
            {
                try { ServiceA.Uninitialize(); } catch { /* teardown after a red test */ }
            }
            ShapeRenderer.Self.Uninitialize();
            LoaderManager.Self?.DisposeAndClear();

            SystemManagers.Default = null!;
            ISystemManagers.Default = null!;
            IGumService.Default = null;
            FrameworkElement.PopupRoot = null!;
            FrameworkElement.ModalRoot = null!;
            Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = null!;
            _lease.Dispose();
        }
    }

    #endregion
}
