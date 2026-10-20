// Silk.NET's GumService, as a subclass of GumServiceSkiaInteractiveBase. Supplies the
// CreateHostCursor/CreateHostKeyboard partial methods and the ApplyGamePadState hook with real
// Silk.NET.Input-backed implementations. The per-frame Forms input pump in Update comes from the base, and the typed
// Default/Cursor/Keyboard accessors from Runtimes/SkiaHost/GumService.SkiaHost.cs. Everything else
// -- rendering, .gumx loading, non-interactive Forms controls, hot reload, window-fit, the sync
// context -- comes from GumServiceSkiaBase, the same way SkiaGum.Wpf/SkiaGum.Maui/any
// bring-your-own-canvas consumer gets it, so this class also serves as the reference example for
// writing a custom interactive Skia host.
#if SILK
#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;
using Silk.NET.Input;
using System;

namespace Gum;

public partial class GumService : GumServiceSkiaInteractiveBase, IGumService
{
    private IInputContext? _inputContext;

    private partial Cursor CreateHostCursor()
    {
        Cursor cursor = new Cursor();
        if (_inputContext != null)
        {
            cursor.AttachSilkInput(_inputContext);
        }
        return cursor;
    }

    // Real desktop Silk contexts always expose at least one keyboard.
    private partial Keyboard? CreateHostKeyboard() =>
        _inputContext != null && _inputContext.Keyboards.Count > 0 ? new Keyboard(_inputContext.Keyboards[0]) : null;

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time)
    {
        if (_inputContext != null && index < _inputContext.Gamepads.Count)
        {
            Gum.Input.GamePadDriver.Apply(gamepad, _inputContext.Gamepads[index], time);
        }
        else
        {
            gamepad.SetConnected(false);
            gamepad.Activity(time);
        }
    }

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
        // Stored before base.Initialize, which calls FormsUtilities.InitializeDefaults, which
        // dispatches back to this instance's CreateCursor/CreateKeyboard overrides above.
        _inputContext = inputContext;
        Clipboard = new SilkGumClipboard(inputContext);

        base.Initialize(canvas, width, height, gumProjectFile);
    }
}
#endif
