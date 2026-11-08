// BRUTAL's GumService, a subclass of GumServiceSkiaInteractiveBase. BRUTAL has no renderer, so the game
// draws Gum into the canvas of a SkiaGameRendering.Brutal SkiaRenderTarget2D and samples that target's
// Vulkan image itself. Input comes from the GLFW window (Ahwoo.Brutal.Glfw) the game created.
#if BRUTAL
#nullable enable

using Brutal.GlfwApi;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;
using System;

namespace Gum;

/// <remarks>
/// A per-frame loop looks like this:
/// <code>
/// GumService.Default.Update(totalSeconds);
/// target.Begin();
/// GumService.Default.Draw(target.Canvas);
/// target.End();
/// </code>
/// Things that need care when you put the target's Vulkan image on screen:
/// <list type="bullet">
/// <item>The image is in <c>SHADER_READ_ONLY_OPTIMAL</c> whenever you own it, after construction and after
/// <c>End()</c>. <c>Begin()</c> and <c>End()</c> record the transitions on the target's queue.</item>
/// <item>If several threads submit to that queue, take your own lock around your submits and pass it as the
/// <c>acquireQueueLock</c> option when creating the Vulkan context.</item>
/// <item>The image is plain UNORM and holds sRGB bytes. If your pass writes to an sRGB target without
/// decoding the sampled value first, colors are encoded twice and look washed out.</item>
/// <item>Cursor positions are GLFW window coordinates. Create the target at the window size so they match
/// Gum's coordinates; on a high-DPI display that is the window size, not the framebuffer size.</item>
/// </list>
/// </remarks>
public partial class GumService : GumServiceSkiaInteractiveBase, IGumService
{
    private IBrutalInput? _input;

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
    /// Initializes Gum for a BRUTAL application, optionally loading a Gum project. No canvas is needed
    /// yet: pass the render target's canvas to <see cref="GumServiceSkiaBase.Draw(SKCanvas)"/> each frame.
    /// </summary>
    /// <param name="window">The GLFW window input is read from.</param>
    /// <param name="width">The width of the render target, used for the root container and canvas coordinate space.</param>
    /// <param name="height">The height of the render target.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    public void Initialize(GlfwWindow window, int width, int height, string? gumProjectFile = null) =>
        Initialize(new BrutalGlfwInput(window), width, height, gumProjectFile);

    /// <summary>
    /// Initializes Gum with a canvas that already exists. See
    /// <see cref="Initialize(GlfwWindow, int, int, string?)"/>.
    /// </summary>
    public void Initialize(SKCanvas canvas, GlfwWindow window, int width, int height, string? gumProjectFile = null) =>
        Initialize(canvas, new BrutalGlfwInput(window), width, height, gumProjectFile);

    internal void Initialize(IBrutalInput input, int width, int height, string? gumProjectFile = null) =>
        Initialize((SKCanvas?)null, input, width, height, gumProjectFile);

    internal void Initialize(SKCanvas? canvas, IBrutalInput input, int width, int height, string? gumProjectFile = null)
    {
        InitializeWithDevices(canvas, width, height, gumProjectFile, () =>
        {
            _input = input;
            Clipboard = CreateClipboard(input);
        });
    }

    // GLFW's clipboard reads and writes the OS clipboard synchronously.
    internal static IGumClipboard CreateClipboard(IBrutalInput input) =>
        new DelegateClipboard(() => input.ClipboardText, text => input.ClipboardText = text);
}
#endif
