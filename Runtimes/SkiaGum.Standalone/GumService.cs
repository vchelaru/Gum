// This is shared source file-linked into host projects with differing <Nullable>
// settings (e.g. SkiaGum.Wpf does not enable it), so declare the nullable context
// here to keep the file's annotations valid and warning-free everywhere.
#nullable enable

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

// Render-only GumService for SkiaSharp host environments (WPF, MAUI, Silk.NET,
// standalone bring-your-own-canvas). namespace Gum / type GumService mirrors the
// game-host GumService (MonoGameGum/GumService.cs) so user code is portable across
// hosts. This file is shared source: it is NOT compiled into Gum.SkiaSharp (which is
// rendering-only) but file-linked into each host lib/sample/tool that needs it.
// See .claude/designs/runtime-unification/GumServiceHostModel.md (issues #3218, #2738).
namespace Gum;
public class GumService : IGumService
{
    static GumService? _default;
    public static GumService Default
    {
        get
        {
            if (_default == null)
            {
                _default = new GumService();
            }
            return _default;
        }
    }

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

    // Skia is a rendering technology, not a windowing/input system, so there is no built-in
    // cursor and the Forms input pump (FormsUtilities) is not wired on Skia. Returning null
    // is intentional; nothing on the Skia path consumes this today (Forms controls render via
    // their contained visual, which needs no cursor).
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

        GumProjectSave? gumProject = null;

        if (!string.IsNullOrEmpty(gumProjectFile))
        {

            gumProject = GumProjectSave.Load(gumProjectFile);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var absolutePath = FileManager.IsRelative(gumProjectFile)
                ? FileManager.MakeAbsolute(gumProjectFile)
                : gumProjectFile;
            var gumDirectory = FileManager.GetDirectory(absolutePath);

            FileManager.RelativeDirectory = gumDirectory;
        }

        // Size the canvas coordinate space before configuring Root, so the
        // RelativeToParent root has something to resolve against.
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

        Root.AddToManagers(SystemManagers.Default);
        Root.UpdateLayout();

        DeferredQueue = new DeferredActionQueue();

        // Wire this service as the runtime-agnostic default so GumCommon code resolves the
        // Skia runtime the same way it does MonoGame/raylib — most importantly so that
        // FrameworkElement.AddToRoot (which adds element.Visual to IGumService.Default.Root)
        // works on Skia. Forms controls render via their contained visual, so this needs no
        // input/cursor plumbing.
        IGumService.Default = this;

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
