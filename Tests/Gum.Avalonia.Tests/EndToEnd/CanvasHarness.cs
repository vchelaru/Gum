using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using EditorTabPlugin_XNA.ViewModels;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.Avalonia.Plugins.TextureCoordinates;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Diagnostics;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The head's own Editor tab (toolbar, wireframe canvas, scroll bars) on a real graphics device, in
/// a headless window next to the <see cref="ProjectTreeHarness"/> over a temp project. The canvas
/// polls its input once per frame, so every pointer or key event is followed by one drawn frame.
/// Needs a display and a GL driver (see <see cref="TestEnvironment.CanCreateDeviceInProcess"/>);
/// scenarios are <c>[SkippableFact]</c>s that skip where there is none and run on the Avalonia UI
/// thread through <see cref="OnUiThread"/>.
/// </summary>
internal sealed class CanvasHarness : IDisposable
{
    public const string SkipReason = "needs a display and a GL driver, and not macOS; set GUM_RUN_CANVAS_DEVICE_TESTS=1 to run on CI";

    private static IServiceProvider Services => TestAppBuilder.Services;

    // The tool raises XnaInitialized once, when its render surface is ready; the plugin, its canvas
    // and the shared device then live for the rest of the process, as in the tool.
    private static bool _isXnaInitialized;

    private readonly HeadlessWindowDriver _driver;
    private Exception? _frameError;
    private Point _pointer;

    /// <summary>True when the canvas can be built here.</summary>
    public static bool CanRun => TestEnvironment.CanCreateDeviceInProcess("GUM_RUN_CANVAS_DEVICE_TESTS");

    /// <summary>
    /// Runs <paramref name="action"/> on the Avalonia UI thread, which owns the shared GL device,
    /// with <see cref="DeviceTestThread"/>'s bounded wait. An <c>[AvaloniaFact]</c> runs there too
    /// but cannot skip on a machine without GL.
    /// </summary>
    public static void OnUiThread(Action action) => DeviceTestThread.Run(action);

    public CanvasHarness()
    {
        Tree = new ProjectTreeHarness();
        try
        {
            PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
            Plugin = pluginManager.PluginContainers.Keys.OfType<AvaloniaEditorTabPlugin>().Single();
            TextureCoordinatePlugin = pluginManager.PluginContainers.Keys.OfType<AvaloniaTextureCoordinatePlugin>().Single();
            Canvas = Plugin.CanvasControl ?? throw new InvalidOperationException("The editor tab plugin built no canvas.");
            AvaloniaTabManager tabManager = (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();
            Control tab = (Control)tabManager.AllTabs.Single(candidate => candidate.Title == "Editor").Content;
            _driver = new HeadlessWindowDriver(tab, width: 1000, height: 800, framesFolderName: "GumCanvas");
            AppWideWindowInput.RouteHotkeys(_driver.Window,
                Services.GetRequiredService<IHotkeyManager>(),
                Services.GetRequiredService<AvaloniaModifierKeyState>());
            Canvas.ErrorOccurred += HandleFrameError;

            if (!_isXnaInitialized)
            {
                InitializeRenderSurface();
                _isXnaInitialized = true;
            }
            // The canvas plugins sat out the fixture's new project, so they hear about it now.
            Tree.Project.IncludeCanvasTabs();
            Plugin.CallProjectLoad(Tree.Project.Project);
            TextureCoordinatePlugin.CallProjectLoad(Tree.Project.Project);
            ResetCamera();
            Frame();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>The Project tree over the same project; its oracles are the canvas scenarios' oracles.</summary>
    public ProjectTreeHarness Tree { get; }

    public ToolProjectFixture Project => Tree.Project;

    public AvaloniaEditorTabPlugin Plugin { get; }

    /// <summary>The Texture Coordinates tab's plugin; its tab is built when the render surface is ready, as in the tool.</summary>
    public AvaloniaTextureCoordinatePlugin TextureCoordinatePlugin { get; }

    public WireframeCanvasControl Canvas { get; }

    public HeadlessWindowDriver Input => _driver;

    public Camera Camera => Renderer.Self.Camera;

    /// <summary>The toolbar's view model: zoom, canvas size, font scale, grid snap.</summary>
    public EditorViewModel Editor => (EditorViewModel)(Plugin.Toolbar?.DataContext
        ?? throw new InvalidOperationException("The editor tab has no toolbar."));

    public IWireframeObjectManager Wireframe => Services.GetRequiredService<IWireframeObjectManager>();

    #region Building

    /// <summary>
    /// Adds an instance through the tool's command and places it, recording the placement as one
    /// undo step so a scenario's first undo undoes only its own gesture.
    /// </summary>
    public InstanceSave AddInstance(ElementSave owner, string name, string type, float x, float y, float? width = null, float? height = null)
    {
        InstanceSave instance = Project.AddInstance(owner, name, type);
        using (Project.UndoManager.RequestLock(owner))
        {
            Gum.DataTypes.Variables.StateSave state = owner.DefaultState!;
            state.SetValue($"{name}.X", x, "float");
            state.SetValue($"{name}.Y", y, "float");
            if (width != null)
            {
                state.SetValue($"{name}.Width", width.Value, "float");
            }
            if (height != null)
            {
                state.SetValue($"{name}.Height", height.Value, "float");
            }
        }
        Tree.SaveAll();
        Wireframe.RefreshAll(forceLayout: true);
        Frame();
        return instance;
    }

    #endregion

    #region Reading

    /// <summary>The value of <paramref name="variableName"/> in <paramref name="element"/>'s default state as saved on disk.</summary>
    public object? SavedValue(ElementSave element, string variableName) =>
        SavedElement(element).DefaultState!.GetValue(variableName);

    /// <summary><paramref name="element"/> as saved on disk.</summary>
    public ElementSave SavedElement(ElementSave element)
    {
        GumProjectSave saved = GumProjectSave.Load(Project.ProjectFilePath, out GumLoadResult result)
            ?? throw new InvalidOperationException($"The saved project did not load: {result.ErrorMessage}");
        return saved.AllElements.SingleOrDefault(candidate => candidate.Name == element.Name)
            ?? throw new InvalidOperationException($"The saved project has no {element.Name}.");
    }

    /// <summary>The toolbar's zoom-in button, the "+" right of the zoom combo box.</summary>
    public Button ZoomInButton => Toolbar.SizedButtons[1];

    /// <summary>The toolbar's "Preview in runtime" button.</summary>
    public Button PreviewButton => Toolbar.PreviewButton;

    private EditorToolbar Toolbar => Plugin.Toolbar ?? throw new InvalidOperationException("The editor tab has no toolbar.");

    /// <summary>The toolbar's font scale "-" button.</summary>
    public Button FontScaleDecreaseButton => Toolbar.SizedButtons[2];

    /// <summary>The toolbar's font scale "+" button.</summary>
    public Button FontScaleIncreaseButton => Toolbar.SizedButtons[3];

    /// <summary>The toolbar's canvas size combo box (Project Default, 480p, 720p...).</summary>
    public ComboBox CanvasSizeComboBox => Toolbar.GetVisualDescendants().OfType<ComboBox>()
        .Single(combo => combo.ItemsSource == Editor.CustomCanvasSizes);

    public CheckBox SnapToGridCheckBox => Toolbar.GetVisualDescendants().OfType<CheckBox>().Single();

    /// <summary>The toolbar's Grid Size box (the combo boxes hold text boxes of their own).</summary>
    public TextBox GridSizeBox => Toolbar.GetVisualDescendants().OfType<TextBox>().Single(box => box.FindAncestorOfType<ComboBox>() == null);

    /// <summary>The thumb of the canvas's scroll bar running along <paramref name="orientation"/>.</summary>
    public Thumb ScrollBarThumb(Orientation orientation) =>
        ((Panel)Canvas.Parent!).Children.OfType<ScrollBar>().Single(bar => bar.Orientation == orientation)
            .GetVisualDescendants().OfType<Thumb>().Single();

    /// <summary>The canvas's top edge, in window coordinates.</summary>
    public double CanvasTopInWindow => CanvasBoundsInWindow().Top;

    /// <summary>The canvas's right edge, in window coordinates.</summary>
    public double CanvasRightInWindow => CanvasBoundsInWindow().Right;

    private Rect CanvasBoundsInWindow()
    {
        Point topLeft = Canvas.TranslatePoint(new Point(0, 0), _driver.Window) ?? throw new InvalidOperationException("The canvas is not in the window.");
        return new Rect(topLeft, Canvas.Bounds.Size);
    }

    public Ruler TopRuler => Canvas.Core.TopRuler ?? throw new InvalidOperationException("The canvas has no rulers.");

    public Ruler LeftRuler => Canvas.Core.LeftRuler ?? throw new InvalidOperationException("The canvas has no rulers.");

    #endregion

    #region Gestures

    /// <summary>Draws one frame, which is when the canvas reads its input; fails if the frame or a gesture crashed.</summary>
    public void Frame()
    {
        _driver.Layout();
        Canvas.RenderFrameNow();
        if (_frameError != null)
        {
            throw new InvalidOperationException("A canvas frame failed.", _frameError);
        }
        Tree.ThrowIfCrashed();
    }

    /// <summary>The window point showing world position (<paramref name="worldX"/>, <paramref name="worldY"/>).</summary>
    public Point WindowPointOf(float worldX, float worldY)
    {
        Camera.WorldToScreen(worldX, worldY, out float screenX, out float screenY);
        // The headless render scale is 1, so a canvas pixel is one device-independent unit.
        return Canvas.TranslatePoint(new Point(screenX, screenY), _driver.Window)
            ?? throw new InvalidOperationException("The canvas is not in the window.");
    }

    public void MoveTo(Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _pointer = point;
        _driver.Window.MouseMove(point, modifiers);
        Frame();
    }

    /// <summary>Moves to <paramref name="point"/>, presses and releases the left button there, a frame after each.</summary>
    public void Click(Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        MoveTo(point, modifiers);
        _driver.Window.MouseDown(point, MouseButton.Left, modifiers);
        Frame();
        _driver.Window.MouseUp(point, MouseButton.Left, modifiers);
        Frame();
    }

    /// <summary>A drag from <paramref name="from"/> to <paramref name="to"/>, a frame after every event.</summary>
    public void Drag(Point from, Point to, RawInputModifiers modifiers = RawInputModifiers.None, int? steps = null, MouseButton button = MouseButton.Left)
    {
        PressButton(from, modifiers, button);
        DragTo(to, modifiers, steps, button);
        ReleaseButton(modifiers, button);
    }

    /// <summary>Moves to <paramref name="point"/> and presses <paramref name="button"/> there, starting a drag.</summary>
    public void PressButton(Point point, RawInputModifiers modifiers = RawInputModifiers.None, MouseButton button = MouseButton.Left)
    {
        MoveTo(point, modifiers);
        _driver.Window.MouseDown(point, button, modifiers);
        _pointer = point;
        Frame();
    }

    /// <summary>
    /// With <paramref name="button"/> held, moves to <paramref name="to"/> in <paramref name="steps"/>
    /// even moves; by default moves of 10 to 20 pixels.
    /// </summary>
    public void DragTo(Point to, RawInputModifiers modifiers = RawInputModifiers.None, int? steps = null, MouseButton button = MouseButton.Left)
    {
        RawInputModifiers held = modifiers | button switch
        {
            MouseButton.Middle => RawInputModifiers.MiddleMouseButton,
            MouseButton.Right => RawInputModifiers.RightMouseButton,
            _ => RawInputModifiers.LeftMouseButton,
        };
        Point from = _pointer;
        int moves = steps ?? Math.Max(1, (int)Math.Floor(Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y)) / 10));
        for (int i = 1; i <= moves; i++)
        {
            double t = (double)i / moves;
            _pointer = new Point(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
            _driver.Window.MouseMove(_pointer, held);
            Frame();
        }
    }

    /// <summary>Releases <paramref name="button"/> where the pointer is, ending a drag.</summary>
    public void ReleaseButton(RawInputModifiers modifiers = RawInputModifiers.None, MouseButton button = MouseButton.Left)
    {
        _driver.Window.MouseUp(_pointer, button, modifiers);
        Frame();
    }

    /// <summary>Presses and releases <paramref name="key"/> with the canvas focused, as after a click on it.</summary>
    public void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        FocusCanvas();
        _driver.Window.KeyPress(key, modifiers, physicalKey, null);
        Frame();
        _driver.Window.KeyRelease(key, modifiers, physicalKey, null);
        Frame();
    }

    /// <summary>Holds a modifier key down on the canvas until <see cref="ReleaseKey"/>.</summary>
    public void HoldKey(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers)
    {
        FocusCanvas();
        _driver.Window.KeyPress(key, modifiers, physicalKey, null);
        Frame();
    }

    public void ReleaseKey(Key key, PhysicalKey physicalKey)
    {
        _driver.Window.KeyRelease(key, RawInputModifiers.None, physicalKey, null);
        Frame();
    }

    /// <summary>Turns the mouse wheel over <paramref name="point"/>; a positive <paramref name="delta"/> scrolls up.</summary>
    public void Wheel(Point point, double delta, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        MoveTo(point, modifiers);
        _driver.Window.MouseWheel(point, new Vector(0, delta), modifiers);
        Frame();
    }

    /// <summary>
    /// Drags <paramref name="data"/> in from outside the canvas and drops it at <paramref name="point"/>,
    /// as the platform delivers a drag from the Standards palette or the file manager. Returns the
    /// effect the canvas reported while the drag was over it.
    /// </summary>
    public DragDropEffects DropOnCanvas(Point point, IDataTransfer data)
    {
        DragDropEffects reported = DragDropEffects.None;
        EventHandler<DragEventArgs> record = (_, e) => reported = e.DragEffects;
        _driver.Window.AddHandler(DragDrop.DragOverEvent, record, handledEventsToo: true);
        try
        {
            _driver.Window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Copy);
            Frame();
            _driver.Window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Copy);
            Frame();
            _driver.Window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Copy);
            Frame();
        }
        finally
        {
            _driver.Window.RemoveHandler(DragDrop.DragOverEvent, record);
        }
        return reported;
    }

    /// <summary>Right-clicks the canvas at <paramref name="point"/>, which opens its menu when an instance is selected.</summary>
    public void RightClick(Point point)
    {
        MoveTo(point);
        _driver.Window.MouseDown(point, MouseButton.Right, RawInputModifiers.None);
        Frame();
        _driver.Window.MouseUp(point, MouseButton.Right, RawInputModifiers.None);
        Frame();
        WaitForPendingMenu(throwOnTimeout: true);
    }

    // Longer than the plugin's own fallback wait for a frame, so a pending menu always settles first.
    private static readonly TimeSpan PendingMenuTimeout = TimeSpan.FromSeconds(5);

    // The plugin opens the menu once the frame after the press is presented. That frame's task
    // completes its continuations on the thread pool, which then posts the menu's opening to the UI
    // thread, possibly after the frames above ran their jobs; so pump until the plugin is done.
    private void WaitForPendingMenu(bool throwOnTimeout)
    {
        Stopwatch waited = Stopwatch.StartNew();
        while (Plugin.IsCanvasContextMenuPending)
        {
            if (waited.Elapsed > PendingMenuTimeout)
            {
                if (throwOnTimeout)
                {
                    throw new TimeoutException($"The canvas menu was still pending {PendingMenuTimeout.TotalSeconds} s after the right-click. {Describe()}");
                }
                return;
            }
            Thread.Sleep(1);
            Dispatcher.UIThread.RunJobs();
        }
        _driver.Layout();
    }

    public ContextMenu ContextMenu => Plugin.CanvasContextMenu;

    /// <summary>The headers of the open canvas menu's items (separators excluded).</summary>
    public List<string> MenuHeaders() => ContextMenu.Items.OfType<MenuItem>().Select(item => item.Header?.ToString() ?? "").ToList();

    /// <summary>
    /// Picks the item at <paramref name="path"/> in the open canvas menu, one header per level
    /// ("Add child object to 'Box'", "Rectangle"), as a click on it does.
    /// </summary>
    public void PickMenu(params string[] path)
    {
        if (!ContextMenu.IsOpen)
        {
            throw new InvalidOperationException("No canvas menu is open.");
        }
        IEnumerable<object?> items = ContextMenu.Items;
        MenuItem? item = null;
        foreach (string header in path)
        {
            List<MenuItem> candidates = items.OfType<MenuItem>().ToList();
            item = candidates.SingleOrDefault(candidate => candidate.Header?.ToString() == header)
                ?? throw new InvalidOperationException($"The menu has no \"{header}\"; it has [{string.Join(", ", candidates.Select(candidate => candidate.Header))}].");
            items = item.Items;
        }
        // The menu's popup is its own top level, which the window's pointer input does not reach.
        item!.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        ContextMenu.Close();
        Frame();
    }

    /// <summary>The rendered colors of the <paramref name="count"/> window pixels right of <paramref name="start"/>.</summary>
    public List<global::Avalonia.Media.Color> PixelsAlong(Point start, int count)
    {
        Frame();
        return _driver.PixelsAlong(start, count);
    }

    /// <summary>Ctrl+Z on the canvas, handled app-wide as in the main window.</summary>
    public void Undo() => Press(Key.Z, PhysicalKey.Z, RawInputModifiers.Control);

    /// <summary>Ctrl+Y on the canvas, handled app-wide as in the main window.</summary>
    public void Redo() => Press(Key.Y, PhysicalKey.Y, RawInputModifiers.Control);

    private void FocusCanvas()
    {
        if (!Canvas.IsFocused)
        {
            Canvas.Focus();
        }
    }

    #endregion

    /// <summary>What the canvas last read from its input and what it shows, for a failure message.</summary>
    public string Describe()
    {
        InputLibrary.Cursor cursor = InputLibrary.Cursor.Self;
        IEnumerable<string> shown = Wireframe.AllIpsos.Select(ipso =>
            $"{ipso.Name} ({ipso.AbsoluteX}, {ipso.AbsoluteY}, {ipso.AbsoluteWidth}x{ipso.AbsoluteHeight})");
        return $"cursor ({cursor.X}, {cursor.Y}), over the canvas {cursor.IsInWindow}, canvas focused {Canvas.IsFocused}, " +
            $"pointer shape {Canvas.InputHost.Cursor}, camera ({Camera.X}, {Camera.Y}) zoom {Camera.Zoom}; shown: {string.Join("; ", shown)}";
    }

    /// <summary>
    /// Puts the camera where the tool starts it: unzoomed, with the canvas's top-left corner a
    /// little inside the view. The camera is the process-wide renderer's, so it outlives each test.
    /// </summary>
    public void ResetCamera()
    {
        Editor.PercentZoom = 100;
        Camera.X = -30;
        Camera.Y = -30;
        Services.GetRequiredService<PluginManager>().CameraChanged();
    }

    /// <summary>The shared oracles, as in <see cref="ProjectTreeHarness.AssertOracles"/>, plus no failed canvas frame.</summary>
    public void AssertOracles()
    {
        Tree.AssertOracles();
        Frame();
    }

    private void HandleFrameError(Exception exception) => _frameError ??= exception;

    // The rest of GumStartupSequence that ToolStartup leaves out: the wireframe's process-wide
    // settings, then the render surface. The tool gets there before any project loads, so the
    // canvas finds its default font relative to the executable's folder, not a project's.
    private void InitializeRenderSurface()
    {
        ((WireframeObjectManager)Wireframe).Initialize();
        string relativeDirectory = ToolsUtilities.FileManager.RelativeDirectory;
        ToolsUtilities.FileManager.RelativeDirectory = AppContext.BaseDirectory;
        try
        {
            // The tool raises it on every plugin, the editor tab (a priority plugin) first. The
            // Texture Coordinates canvas then takes over the content loader, as it does in the tool.
            Plugin.CallXnaInitialized();
            TextureCoordinatePlugin.CallXnaInitialized();
        }
        finally
        {
            ToolsUtilities.FileManager.RelativeDirectory = relativeDirectory;
        }
    }

    public void Dispose()
    {
        try
        {
            if (Canvas != null)
            {
                Canvas.ErrorOccurred -= HandleFrameError;
            }
            // The plugin and its menu outlive the test: a menu left open, or one still pending
            // from a right-click, would be open when the next test starts.
            if (Plugin != null && _driver != null)
            {
                WaitForPendingMenu(throwOnTimeout: false);
                Plugin.CanvasContextMenu.Close();
            }
            _driver?.Dispose();
        }
        finally
        {
            Tree.Dispose();
        }
    }
}
