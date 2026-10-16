using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Spike for the epic on running several GumService instances in one process (#6066). Two services
/// each load their own project (both define a component named "Button", one red and one blue) and
/// both draw into the same render target. The tests assert the behavior we want, so a red test marks
/// a collision between the two services.
/// </summary>
public class MultiGumServiceSpikeTests : BaseTestClass
{
    private const int CaptureWidth = 400;
    private const int CaptureHeight = 300;

    // Project A places its button at (20,20) and B places its button at (200,100); each is 100x50.
    private static readonly Point PointInsideA = new Point(70, 45);
    private static readonly Point PointInsideB = new Point(250, 125);

    private readonly string _rootDirectory;

    public MultiGumServiceSpikeTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "GumTwoServices_" + Guid.NewGuid().ToString("N"));
    }

    public override void Dispose()
    {
        base.Dispose();
        try { Directory.Delete(_rootDirectory, recursive: true); } catch { /* best effort */ }
    }

    // Harness check: a lone service must draw its own project, otherwise the two-service
    // failures below say nothing about collisions.
    [Fact]
    public void SingleService_Alone_ShouldDrawItsButton()
    {
        string pathA = WriteProject("A", red: 255, green: 0, blue: 0, screenName: "MainScreen", x: 20, y: 20);
        using TwoServiceGame game = new TwoServiceGame(pathA, pathB: null);
        game.RunOneFrame();

        BuildScreen(game.ServiceA, game.ProjectA, "MainScreen");

        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new RenderTarget2D(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.CornflowerBlue);
        game.ServiceA.SystemManagers.Renderer.Draw(game.ServiceA.SystemManagers);
        game.ServiceA.SystemManagers.Renderer.Draw(game.ServiceA.SystemManagers);
        gd.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);

        IsRed(pixels[IndexOf(PointInsideA)]).ShouldBeTrue(Info(pixels) + " " + "the lone service's button should be red");
    }

    [Fact]
    public void TwoServices_DrawnOnTopOfEachOther_WithoutAnySwap_ShouldShowBothProjects()
    {
        using TwoServiceGame game = CreateGame();

        GraphicalUiElement screenA = BuildScreen(game.ServiceA, game.ProjectA, "MainScreen");
        GraphicalUiElement screenB = BuildScreen(game.ServiceB, game.ProjectB, "EditorScreen");

        XnaColor[] pixels = DrawBothInto(game, restoreStatics: false);

        IsRed(pixels[IndexOf(PointInsideA)]).ShouldBeTrue(Info(pixels) + " " + "project A's button should be red");
        IsBlue(pixels[IndexOf(PointInsideB)]).ShouldBeTrue(Info(pixels) + " " + "project B's button should be blue");
    }

    [Fact]
    public void TwoServices_DrawnOnTopOfEachOther_WithManualSnapshotSwap_ShouldShowBothProjects()
    {
        using TwoServiceGame game = CreateGame();

        game.StateA.Restore();
        GraphicalUiElement screenA = BuildScreen(game.ServiceA, game.ProjectA, "MainScreen");
        game.StateB.Restore();
        GraphicalUiElement screenB = BuildScreen(game.ServiceB, game.ProjectB, "EditorScreen");

        XnaColor[] pixels = DrawBothInto(game, restoreStatics: true);

        IsRed(pixels[IndexOf(PointInsideA)]).ShouldBeTrue(Info(pixels) + " " + "project A's button should be red");
        IsBlue(pixels[IndexOf(PointInsideB)]).ShouldBeTrue(Info(pixels) + " " + "project B's button should be blue");
    }

    [Fact]
    public void ServiceDraw_OnFirstService_ShouldDrawOnlyItsOwnTree()
    {
        using TwoServiceGame game = CreateGame();

        game.StateA.Restore();
        BuildScreen(game.ServiceA, game.ProjectA, "MainScreen");
        game.StateB.Restore();
        BuildScreen(game.ServiceB, game.ProjectB, "EditorScreen");

        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new RenderTarget2D(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.CornflowerBlue);

        // No restore: this is what a user calling service A's public Draw() gets today.
        game.ServiceA.Draw();
        gd.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);

        IsRed(pixels[IndexOf(PointInsideA)]).ShouldBeTrue(Info(pixels) + " " + "A.Draw() should draw A's button");
        IsBlue(pixels[IndexOf(PointInsideB)]).ShouldBeFalse(Info(pixels) + " " + "A.Draw() should not draw B's button");
    }

    [Fact]
    public void UninitializingSecondService_ShouldLeaveFirstServiceAbleToBuildAndDraw()
    {
        using TwoServiceGame game = CreateGame();

        game.ServiceB.Uninitialize();

        game.StateA.Restore();
        GraphicalUiElement screenA = BuildScreen(game.ServiceA, game.ProjectA, "MainScreen");

        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new RenderTarget2D(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.CornflowerBlue);
        game.ServiceA.SystemManagers.Renderer.Draw(game.ServiceA.SystemManagers);
        gd.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);

        IsRed(pixels[IndexOf(PointInsideA)]).ShouldBeTrue(Info(pixels) + " " + "A should still draw its red button");
    }

    [Fact]
    public void SecondInitialize_ShouldNotChangeFirstServicesView_OfStaticState()
    {
        using TwoServiceGame game = CreateGame();

        // The snapshot captured right after A's Initialize is the reference; any static the second
        // Initialize changed is listed.
        StaticSnapshot afterA = game.StateA;
        // Initializing B made B the current service (the last one entered). Rule 3 of the overwrite
        // semantics: look at A's view by activating A first.
        StaticSnapshot now = StaticSnapshot.Capture(game.ServiceA);

        List<string> changed = new List<string>();
        void Check(string name, object? before, object? after)
        {
            if (!Equals(before, after)) { changed.Add(name); }
        }

        Check("SystemManagers.Default", afterA.Managers, now.Managers);
        Check("ISystemManagers.Default", afterA.InterfaceManagers, now.InterfaceManagers);
        Check("IGumService.Default", afterA.Service, now.Service);
        Check("ObjectFinder.Self.GumProjectSave", afterA.Project, now.Project);
        Check("FileManager.RelativeDirectory", afterA.RelativeDirectory, now.RelativeDirectory);
        Check("FrameworkElement.PopupRoot", afterA.PopupRoot, now.PopupRoot);
        Check("FrameworkElement.ModalRoot", afterA.ModalRoot, now.ModalRoot);
        Check("FormsUtilities.Cursor", afterA.Cursor, now.Cursor);
        Check("FormsUtilities.Keyboard", afterA.Keyboard, now.Keyboard);
        Check("Styling.ActiveStyle", afterA.Style, now.Style);
        Check("GraphicalUiElement.CanvasWidth", afterA.CanvasWidth, now.CanvasWidth);
        Check("GraphicalUiElement.CanvasHeight", afterA.CanvasHeight, now.CanvasHeight);
        Check("ElementSaveExtensions.CustomCreateGraphicalComponentFunc", afterA.CreateComponent, now.CreateComponent);
        Check("GraphicalUiElement.SetPropertyOnRenderable", afterA.SetProperty, now.SetProperty);

        changed.ShouldBeEmpty("statics the second Initialize overwrote: " + string.Join(", ", changed));
    }

    #region Helpers

    private TwoServiceGame CreateGame()
    {
        string pathA = WriteProject("A", red: 255, green: 0, blue: 0, screenName: "MainScreen", x: 20, y: 20);
        string pathB = WriteProject("B", red: 0, green: 0, blue: 255, screenName: "EditorScreen", x: 200, y: 100);

        TwoServiceGame game = new TwoServiceGame(pathA, pathB);
        game.RunOneFrame();
        return game;
    }

    private static GraphicalUiElement BuildScreen(Gum.GumService service, GumProjectSave project, string screenName)
    {
        ScreenSave screenSave = project.Screens.First(item => item.Name == screenName);
        GraphicalUiElement screen = screenSave.ToGraphicalUiElement(service.SystemManagers, addToManagers: true);
        screen.UpdateLayout();
        return screen;
    }

    private static XnaColor[] DrawBothInto(TwoServiceGame game, bool restoreStatics)
    {
        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new RenderTarget2D(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.CornflowerBlue);

        // Two passes so render-target baking (none here) and first-frame state settle the same way
        // the other render tests do.
        for (int pass = 0; pass < 2; pass++)
        {
            // Renderer.Draw leaves no render target bound, so rebind before each service's pass.
            gd.SetRenderTarget(capture);
            if (restoreStatics) { game.StateA.Restore(); }
            game.ServiceA.SystemManagers.Renderer.Draw(game.ServiceA.SystemManagers);

            gd.SetRenderTarget(capture);
            if (restoreStatics) { game.StateB.Restore(); }
            game.ServiceB.SystemManagers.Renderer.Draw(game.ServiceB.SystemManagers);
        }

        gd.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);
        return pixels;
    }

    private static int IndexOf(Point point) => (point.Y * CaptureWidth) + point.X;

    private static bool IsRed(XnaColor c) => c.R > 200 && c.G < 60 && c.B < 60;

    private static bool IsBlue(XnaColor c) => c.B > 200 && c.R < 60 && c.G < 60;

    private static string Info(XnaColor[] pixels)
    {
        XnaColor a = pixels[IndexOf(PointInsideA)];
        XnaColor b = pixels[IndexOf(PointInsideB)];
        return $"[at A ({a.R},{a.G},{a.B}); at B ({b.R},{b.G},{b.B})]";
    }

    /// <summary>
    /// Writes a minimal project: the sample's real Standards, a component named "Button" holding one
    /// colored rectangle, and a screen with one Button instance.
    /// </summary>
    private string WriteProject(string folderName, int red, int green, int blue, string screenName, int x, int y)
    {
        string directory = Path.Combine(_rootDirectory, folderName);
        string standardsDirectory = Path.Combine(directory, "Standards");
        Directory.CreateDirectory(standardsDirectory);
        Directory.CreateDirectory(Path.Combine(directory, "Components"));
        Directory.CreateDirectory(Path.Combine(directory, "Screens"));

        string sampleStandards = FindSampleStandardsDirectory();
        string[] standardNames = Directory.GetFiles(sampleStandards, "*.gutx")
            .Select(item => Path.GetFileNameWithoutExtension(item)!)
            .ToArray();
        foreach (string standardName in standardNames)
        {
            File.Copy(Path.Combine(sampleStandards, standardName + ".gutx"),
                Path.Combine(standardsDirectory, standardName + ".gutx"));
        }

        File.WriteAllText(Path.Combine(directory, "Components", "Button.gucx"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>Button</Name>
              <BaseType>Container</BaseType>
              <State>
                <Name>Default</Name>
            {Var("float", "Width", "100", "float")}
            {Var("float", "Height", "50", "float")}
            {Var("DimensionUnitType", "WidthUnits", "0", "int")}
            {Var("DimensionUnitType", "HeightUnits", "0", "int")}
            {Var("DimensionUnitType", "Fill.WidthUnits", "1", "int")}
            {Var("DimensionUnitType", "Fill.HeightUnits", "1", "int")}
            {Var("float", "Fill.Width", "100", "float")}
            {Var("float", "Fill.Height", "100", "float")}
            {Var("int", "Fill.Red", red.ToString(), "int")}
            {Var("int", "Fill.Green", green.ToString(), "int")}
            {Var("int", "Fill.Blue", blue.ToString(), "int")}
              </State>
              <Instance>
                <Name>Fill</Name>
                <BaseType>ColoredRectangle</BaseType>
                <DefinedByBase>false</DefinedByBase>
              </Instance>
              <Behaviors />
            </ComponentSave>
            """);

        File.WriteAllText(Path.Combine(directory, "Screens", screenName + ".gusx"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>{screenName}</Name>
              <State>
                <Name>Default</Name>
            {Var("float", "Btn.X", x.ToString(), "float")}
            {Var("float", "Btn.Y", y.ToString(), "float")}
              </State>
              <Instance>
                <Name>Btn</Name>
                <BaseType>Button</BaseType>
                <DefinedByBase>false</DefinedByBase>
              </Instance>
              <Behaviors />
            </ScreenSave>
            """);

        string standardReferences = string.Concat(standardNames.Select(name => $"""
              <StandardElementReference>
                <Name>{name}</Name>
                <ElementType>Standard</ElementType>
                <LinkType>ReferenceOriginal</LinkType>
              </StandardElementReference>

            """));

        string gumxPath = Path.Combine(directory, "Project.gumx");
        File.WriteAllText(gumxPath, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <DefaultCanvasWidth>800</DefaultCanvasWidth>
              <DefaultCanvasHeight>480</DefaultCanvasHeight>
              <ScreenReference>
                <Name>{screenName}</Name>
                <ElementType>Screen</ElementType>
                <LinkType>ReferenceOriginal</LinkType>
              </ScreenReference>
              <ComponentReference>
                <Name>Button</Name>
                <ElementType>Component</ElementType>
                <LinkType>ReferenceOriginal</LinkType>
              </ComponentReference>
            {standardReferences}</GumProjectSave>
            """);
        return gumxPath;
    }

    private static string Var(string type, string name, string value, string xsiType) =>
        $"    <Variable><Type>{type}</Type><Name>{name}</Name><Value xsi:type=\"xsd:{xsiType}\">{value}</Value><SetsValue>true</SetsValue></Variable>";

    private static string FindSampleStandardsDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Samples", "MonoGameGumFromFile", "MonoGameGumFromFile", "Content", "Standards");
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
        throw new InvalidOperationException("could not locate sample Standards from " + AppContext.BaseDirectory);
    }

    #endregion

    #region Test game

    private sealed class TwoServiceGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly string _pathA;
        private readonly string? _pathB;

        public Gum.GumService ServiceA { get; }
        public Gum.GumService ServiceB { get; }
        public GumProjectSave ProjectA { get; private set; } = null!;
        public GumProjectSave ProjectB { get; private set; } = null!;
        public StaticSnapshot StateA { get; private set; } = null!;
        public StaticSnapshot StateB { get; private set; } = null!;

        public TwoServiceGame(string pathA, string? pathB)
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            _pathA = pathA;
            _pathB = pathB;
            ServiceA = new Gum.GumService();
            ServiceB = new Gum.GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            ProjectA = ServiceA.Initialize(this, _pathA)!;
            StateA = StaticSnapshot.Capture(ServiceA);
            if (_pathB != null)
            {
                ProjectB = ServiceB.Initialize(this, _pathB)!;
                StateB = StaticSnapshot.Capture(ServiceB);
            }
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (ServiceB.IsInitialized) { ServiceB.Uninitialize(); }
            if (ServiceA.IsInitialized) { ServiceA.Uninitialize(); }
            LoaderManager.Self?.DisposeAndClear();

            // A restored snapshot can leave stale statics behind after both services are gone.
            SystemManagers.Default = null!;
            ISystemManagers.Default = null!;
            IGumService.Default = null;
            FrameworkElement.PopupRoot = null!;
            FrameworkElement.ModalRoot = null!;
            Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = null!;
            base.Dispose(disposing);
        }
    }

    #endregion
}
