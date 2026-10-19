using Gum;
using Gum.Forms.Controls;
using Gum.Input;
using SkiaSharp;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("KniSkiaGum.Tests.TestAssemblyInitialize", "KniSkiaGum.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace KniSkiaGum.Tests;

/// <summary>
/// Assembly-wide test bootstrap for KniSkiaGum. SkiaGum rendering is CPU raster plus RichTextKit
/// text measurement, so this bootstraps against an in-memory <see cref="SKSurface"/> with no window
/// or GL context. No KNI <c>Game</c> exists in tests (it needs a platform package), so Gum is
/// initialized without a window and the input tests supply device state through the Cursor,
/// Keyboard and GamePadDriver seams.
/// </summary>
public class TestAssemblyInitialize : XunitTestFramework
{
    // Kept alive for the whole run: the raster surface backs SystemManagers.Default.Canvas.
    private static SKSurface? _surface;

    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
        ApplyDefaultTestState();
    }

    /// <summary>
    /// Sets up the assembly-wide test state. Called once from the constructor.
    /// </summary>
    public static void ApplyDefaultTestState()
    {
        _surface = SKSurface.Create(new SKImageInfo(800, 600));

        // Initialize polls the gamepads once, and KNI's GamePad needs a platform package. No pad.
        GamePadDriver.ReadState = _ => default;

        GumService.Default.InitializeWithoutWindow(_surface.Canvas, 800, 600);

        // Same for the mouse and keyboard, which Update polls: no device state in a test process.
        GumService.Default.Cursor.ReadMouse = () => default;
        GumService.Default.Keyboard.ReadKeyboard = () => default;

        // #3066 pattern: record post-bootstrap renderables so BaseTestClass.Dispose can sweep
        // anything a test leaks onto the shared layers, keeping tests order-independent.
        BaseTestClass.CaptureRenderableBaseline();

        FrameworkElement.KeyboardsForUiControl.Clear();
    }
}
