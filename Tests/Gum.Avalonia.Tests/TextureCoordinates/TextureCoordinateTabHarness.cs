using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TextureCoordinates;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary;
using Shouldly;
using TextureCoordinateSelectionPlugin.RegionSelection;
using TextureCoordinateSelectionPlugin.ViewModels;

namespace Gum.Avalonia.Tests.TextureCoordinates;

/// <summary>
/// The head's own Texture Coordinates tab (the plugin's singleton view, canvas and scroll bars) on a
/// real graphics device, in a headless window next to the <see cref="CanvasHarness"/>'s Editor tab
/// and Project tree over the same temp project. Selecting through the tree gives the tab the same
/// visual, texture and plugin events it gets in the tool. The canvas polls its input once per
/// frame, so every pointer event is followed by one drawn frame. Needs a display and a GL driver;
/// scenarios are <c>[SkippableFact]</c>s run through <see cref="OnUiThread"/>.
/// </summary>
internal sealed class TextureCoordinateTabHarness : IDisposable
{
    public const string SkipReason = CanvasHarness.SkipReason;

    public const string TabTitle = "Texture Coordinates";

    private static IServiceProvider Services => TestAppBuilder.Services;

    private readonly HeadlessWindowDriver _driver;
    private Exception? _frameError;
    private Point _pointer;

    /// <summary>True when the canvases can be built here.</summary>
    public static bool CanRun => CanvasHarness.CanRun;

    /// <inheritdoc cref="CanvasHarness.OnUiThread"/>
    public static void OnUiThread(Action action) => CanvasHarness.OnUiThread(action);

    public TextureCoordinateTabHarness()
    {
        // Builds the editor tab's canvas and, the first time, raises XnaInitialized, which builds this tab.
        Editor = new CanvasHarness();
        try
        {
            Plugin = Editor.TextureCoordinatePlugin;
            Tab = TabManager.AllTabs.SingleOrDefault(candidate => candidate.Title == TabTitle)
                ?? throw new InvalidOperationException(
                    $"The tool has no {TabTitle} tab; an earlier test may have removed it. Tabs: {string.Join(", ", TabManager.AllTabs.Select(candidate => candidate.Title))}");
            View = (TextureCoordinateView)Tab.Content;
            _driver = new HeadlessWindowDriver(View, width: 600, height: 500, framesFolderName: "GumTextureCoordinates", contentOutlivesTest: true);
            CanvasControl = View.GetVisualDescendants().OfType<ImageRegionCanvasControl>().Single();
            CanvasControl.ErrorOccurred += HandleFrameError;
            // The zoom is the tool's for the session, so it outlives each test; start where the tool does.
            ViewModel.SelectedZoomLevel = 100;
            Frame();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>The Editor tab and, through it, the Project tree and the temp project.</summary>
    public CanvasHarness Editor { get; }

    public ProjectTreeHarness Tree => Editor.Tree;

    public ToolProjectFixture Project => Editor.Project;

    public AvaloniaTextureCoordinatePlugin Plugin { get; }

    /// <summary>The tab as the shell holds it; <see cref="AvaloniaPluginTab.IsVisible"/> is whether the user sees it.</summary>
    public AvaloniaPluginTab Tab { get; }

    public TextureCoordinateView View { get; }

    public ImageRegionCanvasControl CanvasControl { get; }

    /// <summary>The framework-neutral canvas: the shown texture and the region selectors.</summary>
    public ImageRegionSelectionCore Canvas => CanvasControl.Core;

    public MainControlViewModel ViewModel => (MainControlViewModel)(View.DataContext
        ?? throw new InvalidOperationException("The tab has no view model."));

    public HeadlessWindowDriver Input => _driver;

    /// <summary>The canvas's camera, which zooming and panning move.</summary>
    public Camera Camera => Canvas.SystemManagers.Renderer.Camera;

    /// <summary>The toolbar's "Snap to grid" check box.</summary>
    public CheckBox SnapToGridCheckBox => View.GetVisualDescendants().OfType<CheckBox>()
        .Single(box => (box.Content as string) == "Snap to grid");

    /// <summary>The toolbar's "+" button, right of the zoom combo box.</summary>
    public Button ZoomInButton => View.GetVisualDescendants().OfType<Button>()
        .Single(button => (button.Content as string) == "+");

    public AvaloniaTabManager TabManager => (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();

    public PluginManager PluginManager => Services.GetRequiredService<PluginManager>();

    /// <summary>Whether the shell lists the tab in its dock region, which is what makes it appear.</summary>
    public bool IsTabInShell => TabManager.RightBottom.Contains(Tab);

    #region Building

    /// <summary>
    /// Copies a 256x256 PNG into the project folder as <paramref name="fileName"/> and returns the
    /// name, relative to the project, to use as a SourceFile.
    /// </summary>
    public string AddTextureFile(string fileName)
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "TestFont_0.png"), Path.Combine(Project.ProjectFolder, fileName));
        return fileName;
    }

    /// <summary>
    /// Adds a Sprite instance showing <paramref name="sourceFile"/> with a custom texture region,
    /// as one undo step, and saves.
    /// </summary>
    public InstanceSave AddSprite(ElementSave owner, string name, string sourceFile, int left, int top, int width, int height)
    {
        InstanceSave instance = Project.AddInstance(owner, name, "Sprite");
        using (Project.UndoManager.RequestLock(owner))
        {
            StateSave state = owner.DefaultState!;
            state.SetValue($"{name}.SourceFile", sourceFile, "string");
            state.SetValue($"{name}.TextureAddress", TextureAddress.Custom, nameof(TextureAddress));
            state.SetValue($"{name}.TextureLeft", left, "int");
            state.SetValue($"{name}.TextureTop", top, "int");
            state.SetValue($"{name}.TextureWidth", width, "int");
            state.SetValue($"{name}.TextureHeight", height, "int");
        }
        Tree.SaveAll();
        Editor.Wireframe.RefreshAll(forceLayout: true);
        Frame();
        return instance;
    }

    #endregion

    #region Gestures

    /// <summary>Clicks <paramref name="element"/>'s row in the Project tree.</summary>
    public void Select(ElementSave element)
    {
        Tree.Click(Tree.NodeFor(element));
        Frames();
    }

    /// <summary>Clicks <paramref name="instance"/>'s row in the Project tree.</summary>
    public void Select(InstanceSave instance)
    {
        Tree.Click(Tree.NodeFor(instance));
        Frames();
    }

    /// <summary>Ctrl+Z, handled app-wide as in the main window, then a frame of each canvas.</summary>
    public void Undo()
    {
        Editor.Undo();
        Frames();
    }

    /// <summary>
    /// Turns the plugin off and back on as the Manage Plugins dialog does. <paramref name="whileOff"/>
    /// runs in between.
    /// </summary>
    public void TurnPluginOffAndOn(Action? whileOff = null)
    {
        PluginContainer container = PluginManager.PluginContainers[Plugin];
        PluginManager.DisableUserPlugin(container).IsEnabled.ShouldBeFalse("turning the plugin off left it on");
        // The shell refilters its dock regions on the next dispatcher pass.
        _driver.Layout();
        whileOff?.Invoke();
        PluginManager.TryEnablePlugin(container).IsEnabled.ShouldBeTrue("turning the plugin back on left it off");
        Frames();
    }

    /// <summary>The window point showing texture pixel (<paramref name="textureX"/>, <paramref name="textureY"/>).</summary>
    public Point WindowPointOf(float textureX, float textureY)
    {
        Camera camera = Camera;
        camera.WorldToScreen(textureX, textureY, out float screenX, out float screenY);
        // The headless render scale is 1, so a canvas pixel is one device-independent unit.
        return CanvasControl.TranslatePoint(new Point(screenX, screenY), _driver.Window)
            ?? throw new InvalidOperationException("The canvas is not in the window.");
    }

    /// <summary>A left-button drag from <paramref name="from"/> to <paramref name="to"/> in 10 pixel moves, a frame after every event.</summary>
    public void Drag(Point from, Point to)
    {
        MoveTo(from);
        _driver.Window.MouseDown(from, MouseButton.Left, RawInputModifiers.None);
        Frame();
        int moves = Math.Max(1, (int)Math.Floor(Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y)) / 10));
        for (int i = 1; i <= moves; i++)
        {
            double t = (double)i / moves;
            _pointer = new Point(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
            _driver.Window.MouseMove(_pointer, RawInputModifiers.LeftMouseButton);
            Frame();
        }
        _driver.Window.MouseUp(_pointer, MouseButton.Left, RawInputModifiers.None);
        Frame();
    }

    public void MoveTo(Point point)
    {
        _pointer = point;
        _driver.Window.MouseMove(point, RawInputModifiers.None);
        Frame();
    }

    /// <summary>Draws one frame of this tab's canvas, which is when it reads its input; fails if a frame or gesture crashed.</summary>
    public void Frame()
    {
        _driver.Layout();
        CanvasControl.RenderFrameNow();
        if (_frameError != null)
        {
            throw new InvalidOperationException("A Texture Coordinates frame failed.", _frameError);
        }
        Tree.ThrowIfCrashed();
    }

    /// <summary>A frame of each canvas, after a gesture elsewhere that both react to.</summary>
    private void Frames()
    {
        Editor.Frame();
        Frame();
    }

    #endregion

    /// <summary>What the tab shows, for a failure message.</summary>
    public string Describe()
    {
        Camera camera = Camera;
        IEnumerable<string> selectors = Canvas.RectangleSelectors.Select(selector =>
            $"({selector.Left}, {selector.Top}, {selector.Width}x{selector.Height}, visible {selector.Visible})");
        PluginContainer container = PluginManager.PluginContainers[Plugin];
        return $"plugin on {container.IsEnabled} {container.FailureDetails}, tab visible {Tab.IsVisible}, in shell {IsTabInShell} " +
            $"(listed {TabManager.AllTabs.Contains(Tab)}), texture {Canvas.CurrentTexture?.Width}x{Canvas.CurrentTexture?.Height}, " +
            $"camera ({camera.X}, {camera.Y}) zoom {camera.Zoom}, selectors [{string.Join(", ", selectors)}]";
    }

    private void HandleFrameError(Exception exception) => _frameError ??= exception;

    public void Dispose()
    {
        try
        {
            if (CanvasControl != null)
            {
                CanvasControl.ErrorOccurred -= HandleFrameError;
            }
            _driver?.Dispose();
        }
        finally
        {
            Editor.Dispose();
        }
    }
}
