// SILK arm of the partial GumService (issue #3608) — compiled only for the Gum.SilkNet host. The
// shared majority lives in MonoGameGum's GumService.cs (file-linked into SilkNetGum.csproj); this
// file holds the SILK-divergent members: the SKCanvas/IInputContext Initialize family and its
// renderer/forms bootstrap, the Silk input factories, the double-typed Update, HandleResize, and the
// concrete Default. SilkNet has NO [Obsolete] back-compat subclass (unlike MonoGame/Raylib), so
// GumServiceCompat.cs is not linked here and Default returns Gum.GumService directly. SilkNet renders
// through SkiaGum and pumps real Silk.NET.Input, so more of its body is genuinely platform-specific
// than the Raylib arm. The #if SILK wrap is belt-and-suspenders: only SilkNetGum compiles this file,
// and SILK is always defined there.
#if SILK
#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaSharp;
using Silk.NET.Input;
using System;
// Silk.NET.Input also defines an ICursor (the OS mouse-cursor appearance); alias to Gum's input
// abstraction so IGumService.Cursor / CreateCursor resolve unambiguously.
using ICursor = Gum.Wireframe.ICursor;

namespace Gum;

public partial class GumService
{
    #region Default

    /// <summary>
    /// The singleton service instance. Assigned as <see cref="IGumService.Default"/> during
    /// <see cref="Initialize(SKCanvas, IInputContext, string?)"/>. Unlike MonoGame/Raylib, SilkNet
    /// has no <c>[Obsolete]</c> back-compat subclass, so Default is the concrete <see cref="GumService"/>.
    /// </summary>
    public static GumService Default => _default ??= new GumService();

    #endregion

    private IInputContext? _inputContext;

    private double _previousTotalSeconds;
    private bool _hasReceivedUpdate;

    /// <inheritdoc/>
    float? IGumService.GameTime => _hasReceivedUpdate ? (float?)_previousTotalSeconds : null;

    // SilkNetGum requires a canvas + input context, so the host-agnostic no-arg Initialize overloads
    // are not supported -- callers must use Initialize(SKCanvas, IInputContext, ...).
    void IGumService.Initialize() =>
        throw new NotSupportedException(
            "SilkNetGum requires a canvas and input context. Call " +
            "GumService.Default.Initialize(SKCanvas, IInputContext, ...) instead.");

    void IGumService.Initialize(string gumProjectFile) =>
        throw new NotSupportedException(
            "SilkNetGum requires a canvas and input context. Call " +
            "GumService.Default.Initialize(SKCanvas, IInputContext, ..., gumProjectFile) instead.");

    /// <inheritdoc/>
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

    // ApplyGamePadState is intentionally NOT overridden -- gamepad support is out of scope (#3564),
    // so the IGumService default no-op is inherited.

    // AssignClipboard is implemented below via Silk.NET.Input's IKeyboard.ClipboardText (#3651).
    // The AssignNativeTextInput / UninitializePlatform / ApplyTextureFilterPlatform /
    // ExtractUnresolvedTextures partial seams remain intentionally unimplemented (elided) on SILK:
    // Silk.NET.Input exposes no IME/composition API to back NativeTextInput (KeyChar only reports
    // committed characters), so TextBox/PasswordBox type through GetStringTyped same as Raylib; Skia
    // has no Skia-specific Uninitialize teardown or global texture filter; and there is no embedded
    // snapshot texture PNG export. This matches the prior standalone service.
    partial void AssignClipboard()
    {
        if (_inputContext != null)
        {
            Clipboard = new global::Gum.Input.SilkGumClipboard(_inputContext);
        }
    }

    // GetWindowSize backs the shared window-fit helpers. SilkNet has no OS window (the caller owns it
    // and hands us an SKCanvas), so report the current canvas size. SilkNet's own resize path is
    // HandleResize; the inherited zoom/expand fit policies are degenerate against a canvas host.
    private (int width, int height) GetWindowSize() =>
        ((int)GraphicalUiElement.CanvasWidth, (int)GraphicalUiElement.CanvasHeight);

    #region Initialize

    /// <summary>
    /// Initializes Gum for a Silk.NET application, optionally loading a Gum project. The canvas size
    /// is read from <see cref="SKCanvas.DeviceClipBounds"/>; use the explicit-size overload if that
    /// does not match the window.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="inputContext">
    /// The Silk input context, from <c>window.CreateInput()</c> on a window created via
    /// <see cref="Silk.NET.Windowing.Window.Create"/>. Do NOT build this from
    /// <c>SdlWindowing.CreateFrom(existingHandle)</c> wrapping a window you created yourself —
    /// that path skips the view's normal initialization, so it never subscribes to receive input
    /// events; the resulting context looks valid but silently never delivers clicks, key presses,
    /// or typed text (see #3652).
    /// </param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, IInputContext inputContext, string? gumProjectFile = null)
    {
        var bounds = canvas.DeviceClipBounds;
        Initialize(canvas, inputContext, bounds.Width, bounds.Height, gumProjectFile);
    }

    /// <summary>
    /// Initializes Gum for a Silk.NET application with an explicit canvas size, optionally loading a
    /// Gum project.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas Gum should render to.</param>
    /// <param name="inputContext">
    /// The Silk input context. See the remarks on the <see cref="Initialize(SKCanvas, IInputContext, string?)"/>
    /// overload for a gotcha around how this must be constructed.
    /// </param>
    /// <param name="width">The width to use for the root container and canvas coordinate space.</param>
    /// <param name="height">The height to use for the root container and canvas coordinate space.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(SKCanvas canvas, IInputContext inputContext, int width, int height, string? gumProjectFile = null)
    {
        // Stored before InitializeDefaults so the CreateCursor/CreateKeyboard overrides it invokes can
        // build Silk-backed input from this context.
        _inputContext = inputContext;

        // The ctor-time AssignClipboard() call (GumService.cs) runs before this Initialize -- Default's
        // lazy new GumService() constructs the instance on first access, which normally happens before
        // Initialize assigns _inputContext above -- so its null-guard always failed and Clipboard stayed
        // null. Re-run it now that _inputContext is guaranteed non-null (#3651).
        AssignClipboard();

        var managers = new SystemManagers();
        this.SystemManagers = managers;
        SystemManagers.Default = managers;
        ISystemManagers.Default = managers;
        managers.Canvas = canvas;
        managers.Initialize();
        managers.Renderer.ClearsCanvas = false;

        // Size the canvas coordinate space before FinishInitialize adds and lays out Root so the
        // RelativeToParent root has something to resolve against.
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;

        // Wire this service as the runtime-agnostic default BEFORE InitializeDefaults, which calls back
        // into CreateCursor/CreateKeyboard and creates PopupRoot/ModalRoot.
        IGumService.Default = this;

        FormsUtilities.InitializeDefaults(managers, DefaultVisualsVersion.V3);

        // Shared tail: adds the ctor-created Root to managers, reinserts it at the bottom of the main
        // layer, and (when a path is given) loads the project + its localization/standards/texture
        // filter. SilkNet's canvas/renderer/forms bootstrap above is the only Initialize divergence.
        FinishInitialize(gumProjectFile);

        IsInitialized = true;
    }

    #endregion

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

    #region Update

    /// <summary>
    /// Per-frame tick. Call once per frame, before <see cref="Draw"/>, with total elapsed seconds
    /// since startup. Pumps Forms input (cursor/keyboard activity, control events) and advances
    /// AnimationChain playback on <see cref="Root"/> and its descendants.
    /// </summary>
    /// <param name="totalSeconds">Total elapsed time in seconds since startup.</param>
    public void Update(double totalSeconds)
    {
        double delta = _hasReceivedUpdate ? totalSeconds - _previousTotalSeconds : 0;

        roots.Clear();
        roots.Add(Root);

        UpdatePreamble(roots);

        _previousTotalSeconds = totalSeconds;
        _hasReceivedUpdate = true;

        FormsUtilities.Update(totalSeconds, Root);

        AnimateRoots(delta, roots);
    }

    #endregion
}
#endif
