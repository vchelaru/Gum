using Gum.Bundle;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Threading;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Gum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using ToolsUtilities;

// The shared base for every Skia-family GumService (WPF, MAUI, bring-your-own-canvas, and
// Silk.NET). Built only against SKCanvas and the IGumService capability-interface pattern
// (CreateCursor/CreateKeyboard default to null per ADR
// 0006-runtimes-declare-capabilities-through-igumservice.md), so any Skia host gets identical
// instantiation syntax, rendering, .gumx project loading, non-interactive Forms controls, window-fit,
// hot reload, and the sync context for free; a host that wants real mouse/touch/keyboard input
// derives and overrides the two Create* hooks (see Runtimes/SilkNetGum/GumService.Silk.cs for a
// worked example). Named GumServiceSkiaBase, not GumServiceBase -- that name stays free for a
// possible future cross-engine base spanning MonoGame/raylib/Skia/Sokol (#4451). Compiled directly
// into Gum.SkiaSharp (issue #4452); the concrete default Gum.GumService for hosts that don't need
// custom input lives in the separate SkiaGum.Standalone.csproj (referenced by WPF/MAUI/standalone),
// keeping this base free of a competing concrete type for Silk.NET's own GumService to collide with.
namespace Gum;

public abstract class GumServiceSkiaBase : IGumService
{
    /// <summary>
    /// Gets whether GumService has been initialized. Used by extension methods
    /// like <see cref="GraphicalUiElement.AddToRoot()"/>
    /// to guard against calls made before Initialize.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// The root container that fills the entire canvas. Elements added via
    /// <see cref="GraphicalUiElement.AddToRoot()"/>
    /// become children of this container. Null until <c>Initialize</c> is called.
    /// </summary>
    public InteractiveGue Root { get; private set; } = null!;

    /// <summary>
    /// Overlaid above <see cref="Root"/>; for non-modal popups. Created by
    /// <see cref="FormsUtilities.InitializeDefaults"/> during <c>Initialize</c>.
    /// </summary>
    public InteractiveGue PopupRoot => FrameworkElement.PopupRoot;

    /// <summary>
    /// Topmost layer; blocks input to everything below. Created by
    /// <see cref="FormsUtilities.InitializeDefaults"/> during <c>Initialize</c>.
    /// </summary>
    public InteractiveGue ModalRoot => FrameworkElement.ModalRoot;

    #region IGumService implementation

    // SkiaGum requires a canvas to initialize, so the host-agnostic no-arg Initialize
    // overloads defined by IGumService are not supported — callers must use one of the
    // Initialize(SKCanvas, ...) overloads below.
    void IGumService.Initialize() =>
        throw new NotSupportedException(
            "SkiaGum requires a canvas. Call GumService.Default.Initialize(SKCanvas, ...) instead.");

    void IGumService.Initialize(string gumProjectFile) =>
        throw new NotSupportedException(
            "SkiaGum requires a canvas. Call GumService.Default.Initialize(SKCanvas, ..., gumProjectFile) instead.");

    IRenderer IGumService.Renderer => SystemManagers.Default.Renderer;

    // Skia is a rendering technology, not a windowing/input system, so the render-only base has no
    // built-in cursor. A host that overrides CreateCursor (e.g. a future Silk.NET-on-Skia consumer)
    // gets real input; nothing on the render-only path consumes this today (Forms controls render
    // via their contained visual, which needs no cursor).
    ICursor IGumService.Cursor => null!;

    float IGumService.CanvasWidth
    {
        get => GraphicalUiElement.CanvasWidth;
        set => GraphicalUiElement.CanvasWidth = value;
    }

    float IGumService.CanvasHeight
    {
        get => GraphicalUiElement.CanvasHeight;
        set => GraphicalUiElement.CanvasHeight = value;
    }

    /// <summary>
    /// Queue used to defer actions onto the main loop. Pending actions are processed at
    /// the start of each <see cref="Update"/>.
    /// </summary>
    public DeferredActionQueue DeferredQueue { get; private set; } = null!;

    float? IGumService.GameTime => _hasReceivedUpdate ? (float?)_previousTotalSeconds : null;

    /// <inheritdoc/>
    // Skia has no native on-screen keyboard implementation. Settable (not just IGumService's DIM
    // default) so a subclass with a real one to offer (e.g. Silk's IKeyboard-backed clipboard) can
    // assign it during its own Initialize; implicitly satisfies IGumService.NativeTextInput too.
    public INativeTextInput? NativeTextInput { get; protected set; }

    /// <inheritdoc/>
    public IGumClipboard? Clipboard { get; protected set; }

    /// <summary>
    /// The <see cref="global::RenderingLibrary.SystemManagers"/> this service initialized.
    /// Convenience accessor for <see cref="global::RenderingLibrary.SystemManagers.Default"/>,
    /// valid after <c>Initialize</c>.
    /// </summary>
    public SystemManagers SystemManagers => SystemManagers.Default;

    /// <summary>
    /// The collection of connected gamepads available to the application.
    /// </summary>
    public Gum.Input.GamePad[] Gamepads => FormsUtilities.Gamepads;

    /// <summary>
    /// Registers this service's keyboard (created via <see cref="IGumService.CreateKeyboard"/> during
    /// <c>Initialize</c>) as one of <see cref="FrameworkElement.KeyboardsForUiControl"/>, so Forms
    /// controls respond to it by default without the host having to wire that up manually.
    /// </summary>
    public void UseKeyboardDefaults() =>
        FrameworkElement.KeyboardsForUiControl.Add(FormsUtilities.Keyboard);

    /// <summary>
    /// Registers this service's <see cref="Gamepads"/> as <see cref="FrameworkElement.GamePadsForUiControl"/>,
    /// so Forms controls respond to gamepad input by default without the host having to wire that up manually.
    /// </summary>
    public void UseGamepadDefaults() =>
        FrameworkElement.GamePadsForUiControl.AddRange(Gamepads);

    IRenderable IGumService.CreateSpriteRenderable() => new Sprite();

    #endregion

    #region Window fit

    // Composed rather than owned inline so this shares WindowFitController/WindowFitMath with
    // GumService (the MonoGame/Raylib/Silk-partial family) instead of a second copy (issue #4452).
    private WindowFitController? _windowFit;
    private WindowFitController WindowFit => _windowFit ??= new WindowFitController(
        GetWindowSize,
        (zoom, canvasWidth, canvasHeight) =>
        {
            SystemManagers.Renderer.Camera.Zoom = zoom;
            GraphicalUiElement.CanvasWidth = canvasWidth;
            GraphicalUiElement.CanvasHeight = canvasHeight;
            Root.UpdateLayout();
        });

    /// <summary>
    /// Returns the current window size used as the fit-policy reference. This render-only base has
    /// no OS window of its own — the caller owns it and reports resizes via <see cref="HandleResize"/>
    /// — so the default reports the current canvas size, making <see cref="EnableZoomToWindow"/> and
    /// <see cref="EnableExpandToWindow"/> degenerate (canvas and "window" are the same value) unless a
    /// subclass that does own a real window overrides this.
    /// </summary>
    protected virtual (int width, int height) GetWindowSize() =>
        ((int)GraphicalUiElement.CanvasWidth, (int)GraphicalUiElement.CanvasHeight);

    /// <inheritdoc cref="WindowFitController.EnableZoomToWindow"/>
    public void EnableZoomToWindow(WindowZoomMode mode = WindowZoomMode.HeightDominant, float defaultZoom = 1f) =>
        WindowFit.EnableZoomToWindow(mode, defaultZoom);

    /// <inheritdoc cref="WindowFitController.EnableExpandToWindow"/>
    public void EnableExpandToWindow(float defaultZoom = 1f) =>
        WindowFit.EnableExpandToWindow(defaultZoom);

    #endregion

    #region Hot reload

    private IGumHotReloadManager? _hotReloadManager;
    private readonly List<GraphicalUiElement> _hotReloadRoots = new();

    /// <summary>
    /// Starts watching the Gum project source files at the given path.
    /// When any .gumx, .gucx, .gusx, or .gutx file changes, the project
    /// is reloaded and active elements in Root have their state reapplied.
    /// </summary>
    /// <param name="absoluteGumxSourcePath">
    /// Absolute path to the source .gumx file (not the bin/Content copy).
    /// </param>
    /// <remarks>
    /// Reloads the element tree, re-applies *Animations.ganx/.ganj files, and re-applies the project's
    /// texture filter, matching the render-only base's own capabilities. Skia has no global
    /// texture-filter setting to re-apply (elided, same as the prior Silk.NET implementation).
    /// </remarks>
    public void EnableHotReload(string absoluteGumxSourcePath)
    {
        _hotReloadManager = new GumHotReloadManager(
            applyProjectTextureFilter: _ => { },
            loadAnimationsFromProvider: GumAnimationLoader.LoadAnimationsFromProvider,
            disposeCachedAsset: path => LoaderManager.Self.Dispose(path));
        _hotReloadManager.ReloadCompleted += () => HotReloadCompleted?.Invoke();
        _hotReloadManager.Start(absoluteGumxSourcePath);
    }

    /// <summary>
    /// Raised after a hot-reload pass completes (Root.Children rebuilt from updated
    /// ElementSaves). Subscribe to react to project changes — e.g. rebuild
    /// entity-attached Gum visuals that aren't part of Root.Children and therefore weren't
    /// touched by the in-place patch.
    /// </summary>
    public event Action? HotReloadCompleted;

    #endregion

    #region Snapshot export and animation loading

    // No engine-level texture-save capability is wired up on this render-only base (a Skia consumer
    // could save an SKBitmap to PNG, but that's not implemented here) -- embedded/generated textures
    // stay unresolved in an exported snapshot, same as Raylib's GumService (issue #4460).
    private GumSnapshotExporter? _snapshotExporter;
    private GumSnapshotExporter SnapshotExporter => _snapshotExporter ??= new GumSnapshotExporter();

    /// <summary>
    /// Exports the live UI tree under <see cref="Root"/> to a Gum project at <paramref name="filePath"/>,
    /// so it can be opened and inspected in the Gum tool. This is the headline path for code-only games,
    /// which have no design-time .gumx to open. Each runtime element is written as a standard-element
    /// instance and the screen is named after the file.
    /// </summary>
    /// <param name="filePath">
    /// Destination project (.gumx) path. Its directory receives the Screens/ and Standards/ subfolders.
    /// </param>
    /// <param name="shake">
    /// When true (default), values equal to the standard-element default are pruned so the artifact is
    /// light and reads as "unedited" in the tool. When false, every value is written — heavier, but the
    /// always-correct baseline-free form.
    /// </param>
    public void ExportSnapshot(string filePath, bool shake = true) =>
        SnapshotExporter.ExportSnapshot(Root, filePath, shake);

    // Set by Initialize when a project is loaded from a loose .gumx/.gumj file, so LoadAnimations can
    // enumerate *Animations.ganx/.ganj files from the project's own directory. This base has no
    // .gumpkg (bundle) support, so it's always a loose-file provider. Not used by EnableHotReload's
    // reload path -- GumHotReloadManager builds its own provider from the watched source path.
    private IGumFileProvider? _projectFileProvider;

    /// <summary>
    /// Loads animations for all elements in the project by enumerating the project's
    /// <c>*Animations.ganx</c> and <c>*Animations.ganj</c> files from the directory the project was
    /// loaded from.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a Gum project hasn't been loaded first (via the <c>gumProjectFile</c> overload of
    /// <see cref="Initialize(SKCanvas, int, int, string)"/>).
    /// </exception>
    [Obsolete("Experimental - this API may change in future versions")]
    public void LoadAnimations() => GumAnimationLoader.LoadAnimations(_projectFileProvider);

    #endregion

    #region Sync context

    private Gum.Async.SingleThreadSynchronizationContext? _syncContext;

    /// <summary>
    /// The active single-threaded synchronization context, or <c>null</c> if
    /// <see cref="UseSingleThreadedAsync"/> has not been called.
    /// </summary>
    public Gum.Async.SingleThreadSynchronizationContext? SynchronizationContext => _syncContext;

    /// <summary>
    /// Installs a <see cref="Gum.Async.SingleThreadSynchronizationContext"/> on the calling
    /// thread so that <c>await</c> continuations (including
    /// <c>await dialogBox.ShowAsync(...)</c>) resume on the host's primary thread. Call once,
    /// after <c>Initialize</c>. Subsequent calls are no-ops.
    /// </summary>
    /// <remarks>
    /// Off by default. Skip this call if you've already installed your own
    /// <see cref="System.Threading.SynchronizationContext"/> — installing two would
    /// route continuations through the wrong queue.
    /// </remarks>
    public void UseSingleThreadedAsync()
    {
        if (_syncContext != null) return;
        _syncContext = new Gum.Async.SingleThreadSynchronizationContext();
    }

    #endregion

    /// <summary>
    /// Initializes Gum for a Skia canvas, optionally loading a Gum project. The canvas
    /// size is read from <see cref="SKCanvas.DeviceClipBounds"/> to size the root container.
    /// If that does not produce the size you expect (for example if the canvas's clip has not
    /// yet been configured), use the overload that takes explicit width and height instead.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, string? gumProjectFile = null)
    {
        var bounds = canvas.DeviceClipBounds;
        Initialize(canvas, bounds.Width, bounds.Height, gumProjectFile);
    }

    /// <summary>
    /// Initializes Gum for a Skia canvas with an explicit canvas size, optionally loading
    /// a Gum project.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, int width, int height, string? gumProjectFile = null)
    {
        // SkiaGum relies on ModuleInitializer instead of explicitly registering
        // runtimes.
        SystemManagers.Default = new SystemManagers();
        SystemManagers.Default.Canvas = canvas;
        SystemManagers.Default.Initialize();
        SystemManagers.Default.Renderer.ClearsCanvas = false;

        // Size the canvas coordinate space before Root and the InitializeDefaults-created
        // PopupRoot/ModalRoot are created, so their RelativeToParent/fullscreen layout has
        // something to resolve against.
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;

        Root = new ContainerRuntime
        {
            Width = 0,
            WidthUnits = DimensionUnitType.RelativeToParent,
            Height = 0,
            HeightUnits = DimensionUnitType.RelativeToParent,
            Name = "Main Root",
            HasEvents = false,
        };

        DeferredQueue = new DeferredActionQueue();

        // Wire this service as the runtime-agnostic default so GumCommon code resolves the
        // Skia runtime the same way it does MonoGame/raylib — most importantly so that
        // FrameworkElement.AddToRoot (which adds element.Visual to IGumService.Default.Root)
        // works on Skia. Must happen before InitializeDefaults, which calls back into
        // CreateCursor/CreateKeyboard through IGumService.Default.
        IGumService.Default = this;

        // Registers the code-only V3 default visuals (Button, Label, ...) and creates
        // PopupRoot/ModalRoot, same as every other backend's Initialize. Without this, a
        // code-only Forms control got no Visual unless a .gumx project happened to define one
        // for it (issue #4452).
        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);

        Root.AddToManagers(SystemManagers.Default);
        Root.UpdateLayout();

        if (!string.IsNullOrEmpty(gumProjectFile))
        {
            var gumProject = GumProjectSave.Load(gumProjectFile);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            // Overrides the code-only defaults registered above with the project's own
            // Forms-behavior visuals, where the project defines one.
            FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var absolutePath = FileManager.IsRelative(gumProjectFile)
                ? FileManager.MakeAbsolute(gumProjectFile)
                : gumProjectFile;
            var gumDirectory = FileManager.GetDirectory(absolutePath);

            FileManager.RelativeDirectory = gumDirectory;
            _projectFileProvider = new LooseFileGumFileProvider(gumDirectory);
        }

        IsInitialized = true;
    }

    /// <summary>
    /// Updates the canvas coordinate space and re-runs layout on the root container.
    /// Call this from your platform's window-resized callback so Gum-layouted elements
    /// reposition to match the new window size.
    /// </summary>
    /// <param name="width">The new canvas width.</param>
    /// <param name="height">The new canvas height.</param>
    public void HandleResize(int width, int height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Root?.UpdateLayout();
    }

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }

    private double _previousTotalSeconds;
    private bool _hasReceivedUpdate;

    /// <summary>
    /// Per-frame tick. Call once per frame, before <see cref="Draw"/>, with the total
    /// number of seconds elapsed since the application started. Drives AnimateSelf on
    /// the root and (via recursion) every descendant — without it, AnimationChain
    /// playback won't advance. Hosts that need this to find their screens must attach
    /// them via <see cref="GraphicalUiElement.AddToRoot()"/> so they
    /// become children of <see cref="Root"/>.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    public virtual void Update(double totalSeconds)
    {
        _windowFit?.PollAndApplyFit();

        _syncContext?.Update();
        DeferredQueue?.ProcessPending();
        if (_hotReloadManager != null)
        {
            _hotReloadRoots.Clear();
            _hotReloadRoots.Add(Root);
            _hotReloadManager.Update(_hotReloadRoots);
        }

        double delta = _hasReceivedUpdate ? totalSeconds - _previousTotalSeconds : 0;
        _previousTotalSeconds = totalSeconds;
        _hasReceivedUpdate = true;

        Root?.AnimateSelf(delta);
    }
}
