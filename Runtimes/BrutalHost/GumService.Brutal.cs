// BRUTAL's GumService, a subclass of GumServiceSkiaInteractiveBase, shared by Gum.Brutal (OpenGL) and
// Gum.Brutal.Vulkan. The game calls Initialize once, Update and Draw every frame, and never touches
// Skia: GumService owns the SkiaGameRendering context and render target, recreates the target when the
// framebuffer size changes, and puts the frame on the window (the per-package IBrutalGpuBackend).
// Input comes from the GLFW window (Ahwoo.Brutal.Glfw) the game created.
#if BRUTAL
#nullable enable

using Brutal.GlfwApi;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaGum.Helpers;
using SkiaSharp;
using System;

namespace Gum;

/// <summary>
/// A per-frame loop looks like this:
/// <code>
/// GumService.Default.Initialize(window, "Content/GumProject/GumProject.gumj");
/// while (!window.ShouldClose)
/// {
///     Glfw.PollEvents();
///     GumService.Default.Update(totalSeconds);
///     GumService.Default.Draw();
///     window.SwapBuffers(); // OpenGL only; Gum.Brutal.Vulkan presents inside Draw
/// }
/// </code>
/// Gum's canvas is the window's framebuffer. Make the window resizable and Gum follows its size; cursor
/// positions are scaled from window coordinates to framebuffer pixels, so a high-DPI display works.
/// </summary>
public partial class GumService : GumServiceSkiaInteractiveBase, IGumService
{
    private IBrutalInput? _input;
    private IBrutalGpuBackend? _backend;
    private SkiaTargetHolder<IBrutalTarget>? _targetHolder;

    private partial Cursor CreateHostCursor()
    {
        Cursor cursor = new Cursor();
        if (_input != null)
        {
            cursor.AttachBrutalInput(_input);
        }
        return cursor;
    }

    private partial Keyboard? CreateHostKeyboard() => _input != null ? new Keyboard(_input) : null;

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time)
    {
        byte[] buttons = new byte[Gum.Input.GamePadDriver.ButtonCount];
        float[] axes = new float[Gum.Input.GamePadDriver.AxisCount];
        if (_input != null && _input.TryReadGamepad(index, buttons, axes))
        {
            Gum.Input.GamePadDriver.Apply(gamepad, buttons, axes, time);
        }
        else
        {
            gamepad.SetConnected(false);
            gamepad.Activity(time);
        }
    }

    /// <summary>
    /// The color the frame is cleared to before Gum draws. Transparent by default, which shows black on
    /// the window. Set it before <see cref="Draw"/>.
    /// </summary>
    public SKColor ClearColor { get; set; } = SKColors.Transparent;

    /// <summary>
    /// Draws Gum into the render target Gum created and puts the frame on the window. If the framebuffer
    /// size changed since the last call, the target is recreated at the new size first. Does nothing while
    /// the framebuffer is empty (a minimized window).
    /// </summary>
    // Hides GumServiceSkiaBase.Draw(). Switch to override once that method is virtual (Gum PR 7764).
    public new void Draw()
    {
        if (_targetHolder == null || _backend == null || _input == null)
        {
            // Initialized with a canvas (tests), so there is no target of Gum's own to manage.
            base.Draw();
            return;
        }

        int width = _input.FramebufferWidth;
        int height = _input.FramebufferHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (width != _targetHolder.Width || height != _targetHolder.Height)
        {
            // The previous frame may still be sampling the target that is about to be freed.
            _backend.WaitForGpu();
        }

        _targetHolder.EnsureSize(width, height);
        SKCanvas? canvas = _targetHolder.BeginFrame();
        IBrutalTarget? target = _targetHolder.Target;
        if (canvas == null || target == null)
        {
            return;
        }

        canvas.Clear(ClearColor);
        base.Draw();
        target.End();
        _backend.Present(target, _targetHolder.Width, _targetHolder.Height);
    }

    /// <summary>
    /// Initializes Gum with the GPU backend that owns Skia, at the window's framebuffer size.
    /// </summary>
    internal void Initialize(
        IBrutalInput input, IBrutalGpuBackend backend, string? gumProjectFile = null,
        DefaultVisualsVersion visualsVersion = DefaultVisualsVersion.Newest)
    {
        _targetHolder?.Dispose();
        _backend?.Dispose();

        _backend = backend;
        _targetHolder = new SkiaTargetHolder<IBrutalTarget>(
            backend.CreateTarget,
            canvas => SystemManagers.Default.Canvas = canvas,
            HandleResize);
        _targetHolder.Create(input.FramebufferWidth, input.FramebufferHeight);

        // With a zero-size framebuffer (minimized at startup) there is no canvas yet; the first Draw at a
        // real size creates the target.
        InitializeWithDevices(
            _targetHolder.Canvas, Math.Max(1, _targetHolder.Width), Math.Max(1, _targetHolder.Height), gumProjectFile,
            () => AttachInput(input), visualsVersion);
    }

    /// <summary>
    /// Initializes Gum with a canvas that already exists and no target of Gum's own. For tests, which draw into
    /// an in-memory surface.
    /// </summary>
    internal void Initialize(SKCanvas? canvas, IBrutalInput input, int width, int height, string? gumProjectFile = null)
    {
        _targetHolder?.Dispose();
        _targetHolder = null;
        _backend?.Dispose();
        _backend = null;

        InitializeWithDevices(canvas, width, height, gumProjectFile, () => AttachInput(input));
    }

    /// <inheritdoc/>
    /// <remarks>Also disposes the render target and the GPU context Gum created.</remarks>
    public override void Uninitialize()
    {
        base.Uninitialize();
        _targetHolder?.Dispose();
        _targetHolder = null;
        _backend?.Dispose();
        _backend = null;
    }

    private void AttachInput(IBrutalInput input)
    {
        _input = input;
        Clipboard = CreateClipboard(input);
    }

    // GLFW's clipboard reads and writes the OS clipboard synchronously.
    internal static IGumClipboard CreateClipboard(IBrutalInput input) =>
        new DelegateClipboard(() => input.ClipboardText, text => input.ClipboardText = text);
}
#endif
