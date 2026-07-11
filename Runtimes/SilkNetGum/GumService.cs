// Shared-nullable context: this file compiles only into Gum.SilkNet, but keep the annotation
// explicit to match the sibling host GumService files (SkiaGum.Standalone, Sokol).
#nullable enable

using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Threading;
using Gum.Wireframe;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;
using Silk.NET.Input;
using System;
using ToolsUtilities;
// Silk.NET.Input also defines an ICursor (the OS mouse-cursor appearance); alias to Gum's input
// abstraction so IGumService.Cursor / CreateCursor resolve unambiguously.
using ICursor = Gum.Wireframe.ICursor;

namespace Gum;

/// <summary>
/// Game-host GumService for Silk.NET applications. Renders through SkiaGum (the caller supplies an
/// <see cref="SKCanvas"/>) and drives real Forms input via Silk.NET.Input (the caller supplies an
/// <see cref="IInputContext"/> from <c>view.CreateInput()</c>). The caller still owns the window.
///
/// Unlike the render-only SkiaGum.Standalone GumService (Cursor => null, no input pump), this
/// service overrides <see cref="IGumService.CreateCursor"/> / <see cref="IGumService.CreateKeyboard"/>
/// with Silk-backed input and pumps <see cref="FormsUtilities.Update"/> each frame, so Forms
/// controls become interactive. Mirrors the game-host shape of MonoGame/Raylib/Sokol.
/// </summary>
public class GumService : IGumService
{
    static GumService? _default;

    /// <summary>
    /// The singleton service instance. Assigned as <see cref="IGumService.Default"/> during
    /// <see cref="Initialize(SKCanvas, IInputContext, string?)"/>.
    /// </summary>
    public static GumService Default => _default ??= new GumService();

    private IInputContext? _inputContext;

    /// <summary>
    /// Gets whether <c>Initialize</c> has been called. Guards extension methods like
    /// <see cref="GraphicalUiElementExtensionMethods.AddToRoot(GraphicalUiElement)"/>.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// The root container that fills the entire canvas. Elements added via <c>AddToRoot</c> become
    /// children of this container. Null until <c>Initialize</c> is called.
    /// </summary>
    public InteractiveGue Root { get; private set; } = null!;

    /// <summary>
    /// The Silk-backed cursor, fed by the caller's <see cref="IInputContext"/> mice.
    /// </summary>
    public Cursor Cursor => (FormsUtilities.Cursor as Cursor)!;

    /// <summary>
    /// The Silk-backed keyboard, fed by the caller's <see cref="IInputContext"/> keyboards.
    /// </summary>
    public Keyboard Keyboard => (FormsUtilities.Keyboard as Keyboard)!;

    /// <summary>
    /// The popup root (mirrors <see cref="FrameworkElement.PopupRoot"/>) for non-modal overlays.
    /// Exposed on the instance so shared input code (CursorExtensions) resolves it via
    /// GumService.Default, matching the MonoGame/Sokol shape.
    /// </summary>
    public InteractiveGue PopupRoot => FrameworkElement.PopupRoot;

    /// <summary>
    /// The modal root (mirrors <see cref="FrameworkElement.ModalRoot"/>); blocks input below it.
    /// </summary>
    public InteractiveGue ModalRoot => FrameworkElement.ModalRoot;

    #region IGumService implementation

    // SilkNetGum requires a canvas + input context, so the host-agnostic no-arg Initialize
    // overloads are not supported -- callers must use Initialize(SKCanvas, IInputContext, ...).
    void IGumService.Initialize() =>
        throw new NotSupportedException(
            "SilkNetGum requires a canvas and input context. Call " +
            "GumService.Default.Initialize(SKCanvas, IInputContext, ...) instead.");

    void IGumService.Initialize(string gumProjectFile) =>
        throw new NotSupportedException(
            "SilkNetGum requires a canvas and input context. Call " +
            "GumService.Default.Initialize(SKCanvas, IInputContext, ..., gumProjectFile) instead.");

    IRenderer IGumService.Renderer => SystemManagers.Default.Renderer;

    ICursor IGumService.Cursor => FormsUtilities.Cursor;

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
    /// Queue used to defer actions onto the main loop. Pending actions are processed at the start
    /// of each <see cref="Update"/>.
    /// </summary>
    public DeferredActionQueue DeferredQueue { get; private set; } = null!;

    float? IGumService.GameTime => _hasReceivedUpdate ? (float?)_previousTotalSeconds : null;

    // Silk.NET has no native on-screen keyboard or OS clipboard implementation here.
    INativeTextInput? IGumService.NativeTextInput => null;
    IGumClipboard? IGumService.Clipboard => null;

    IRenderable IGumService.CreateSpriteRenderable() => new Sprite();

    /// <inheritdoc/>
    ICursor? IGumService.CreateCursor()
    {
        var cursor = new Cursor();
        if (_inputContext != null)
        {
            cursor.AttachSilkInput(_inputContext);
        }
        return cursor;
    }

    /// <inheritdoc/>
    IInputReceiverKeyboard? IGumService.CreateKeyboard()
    {
        // Real desktop Silk contexts always expose at least one keyboard. For the degenerate
        // (headless) case, return an inert device-less Keyboard rather than null: FormsUtilities.Update
        // ticks keyboard.Activity() unconditionally, so a null here would NRE on the first Update.
        if (_inputContext == null || _inputContext.Keyboards.Count == 0)
        {
            return new Keyboard();
        }
        return new Keyboard(_inputContext.Keyboards[0]);
    }

    // ApplyGamePadState is intentionally NOT overridden -- gamepad support is out of scope
    // (#3564), so the IGumService default no-op is inherited.

    #endregion

    /// <summary>
    /// Initializes Gum for a Silk.NET application, optionally loading a Gum project. The canvas
    /// size is read from <see cref="SKCanvas.DeviceClipBounds"/>; use the explicit-size overload if
    /// that does not match the window.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="inputContext">The Silk input context, from <c>view.CreateInput()</c>.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, IInputContext inputContext, string? gumProjectFile = null)
    {
        var bounds = canvas.DeviceClipBounds;
        Initialize(canvas, inputContext, bounds.Width, bounds.Height, gumProjectFile);
    }

    /// <summary>
    /// Initializes Gum for a Silk.NET application with an explicit canvas size, optionally loading
    /// a Gum project.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="inputContext">The Silk input context, from <c>view.CreateInput()</c>.</param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, IInputContext inputContext, int width, int height, string? gumProjectFile = null)
    {
        // Stored before InitializeDefaults so the CreateCursor/CreateKeyboard overrides it invokes
        // can build Silk-backed input from this context.
        _inputContext = inputContext;

        SystemManagers.Default = new SystemManagers();
        SystemManagers.Default.Canvas = canvas;
        SystemManagers.Default.Initialize();
        SystemManagers.Default.Renderer.ClearsCanvas = false;

        if (!string.IsNullOrEmpty(gumProjectFile))
        {
            var gumProject = GumProjectSave.Load(gumProjectFile);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var absolutePath = FileManager.IsRelative(gumProjectFile)
                ? FileManager.MakeAbsolute(gumProjectFile)
                : gumProjectFile;
            FileManager.RelativeDirectory = FileManager.GetDirectory(absolutePath);
        }

        // Size the canvas coordinate space before configuring Root so the RelativeToParent root has
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

        Root.AddToManagers(SystemManagers.Default);
        Root.UpdateLayout();

        DeferredQueue = new DeferredActionQueue();

        // Wire this service as the runtime-agnostic default BEFORE InitializeDefaults, which calls
        // back into CreateCursor/CreateKeyboard and creates PopupRoot/ModalRoot.
        IGumService.Default = this;

        FormsUtilities.InitializeDefaults(SystemManagers.Default, DefaultVisualsVersion.V3);

        IsInitialized = true;
    }

    /// <summary>
    /// Registers the Silk keyboard as a UI input source so Forms controls (TextBox focus
    /// navigation, shortcut keys, arrow-key list navigation, etc.) read from it. Call once after
    /// <see cref="Initialize(SKCanvas, IInputContext, string?)"/>. Mirrors MonoGame/Sokol
    /// <c>UseKeyboardDefaults</c>.
    /// </summary>
    public void UseKeyboardDefaults()
    {
        FrameworkElement.KeyboardsForUiControl.Add(Keyboard);
    }

    /// <summary>
    /// Updates the canvas coordinate space and re-runs layout on the root container. Call this from
    /// your window-resized callback so Gum-layouted elements reposition to the new window size.
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
    /// Per-frame tick. Call once per frame, before <see cref="Draw"/>, with total elapsed seconds
    /// since startup. Pumps Forms input (cursor/keyboard activity, control events) and advances
    /// AnimationChain playback on <see cref="Root"/> and its descendants.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    public void Update(double totalSeconds)
    {
        DeferredQueue?.ProcessPending();

        double delta = _hasReceivedUpdate ? totalSeconds - _previousTotalSeconds : 0;
        _previousTotalSeconds = totalSeconds;
        _hasReceivedUpdate = true;

        FormsUtilities.Update(totalSeconds, Root);

        Root?.AnimateSelf(delta);
    }
}
