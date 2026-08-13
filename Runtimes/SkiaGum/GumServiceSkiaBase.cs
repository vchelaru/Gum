using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Threading;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using ToolsUtilities;

// The shared, render-only base for every Skia-family GumService (WPF, MAUI, bring-your-own-canvas,
// and -- pending #4452 phase 2 -- Silk.NET). Built only against SKCanvas and the IGumService
// capability-interface pattern (CreateCursor/CreateKeyboard default to null per ADR
// 0006-runtimes-declare-capabilities-through-igumservice.md), so any Skia host gets identical
// instantiation syntax, rendering, .gumx project loading, and non-interactive Forms controls for
// free; a host that wants real mouse/touch/keyboard input derives and overrides the two Create*
// hooks. Named GumServiceSkiaBase, not GumServiceBase -- that name stays free for a possible future
// cross-engine base spanning MonoGame/raylib/Skia/Sokol (#4451). Compiled directly into
// Gum.SkiaSharp (issue #4452 phase 1); WPF/MAUI/standalone consumers get it through the
// SkiaGum.csproj ProjectReference they already have instead of file-linking shared source.
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

    // Skia has no native on-screen keyboard or OS clipboard implementation.
    INativeTextInput? IGumService.NativeTextInput => null;
    IGumClipboard? IGumService.Clipboard => null;

    IRenderable IGumService.CreateSpriteRenderable() => new Sprite();

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
    public void Update(double totalSeconds)
    {
        DeferredQueue?.ProcessPending();

        double delta = _hasReceivedUpdate ? totalSeconds - _previousTotalSeconds : 0;
        _previousTotalSeconds = totalSeconds;
        _hasReceivedUpdate = true;

        Root?.AnimateSelf(delta);
    }
}
