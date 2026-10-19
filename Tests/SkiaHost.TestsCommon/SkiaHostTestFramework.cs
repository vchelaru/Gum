using Gum.Forms.Controls;
using SkiaSharp;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SkiaHostTests;

/// <summary>
/// Assembly-wide bootstrap for the interactive Skia host test projects. SkiaGum rendering is CPU
/// raster plus RichTextKit text measurement, so Gum is initialized against an in-memory
/// <see cref="SKSurface"/> with no window or GL context. A host's <c>TestAssemblyInitialize</c>
/// derives from this, supplies <see cref="InitializeGum"/>, and is named by the assembly's
/// <c>[assembly: Xunit.TestFramework]</c> attribute.
/// </summary>
public abstract class SkiaHostTestFramework : XunitTestFramework
{
    public const int CanvasWidth = 800;
    public const int CanvasHeight = 600;

    // Kept alive for the whole run: the raster surface backs SystemManagers.Default.Canvas.
    private static SKSurface? _surface;

    protected SkiaHostTestFramework(IMessageSink messageSink) : base(messageSink)
    {
        _surface = SKSurface.Create(new SKImageInfo(CanvasWidth, CanvasHeight));

        InitializeGum(_surface.Canvas);

        // #3066 pattern: record post-bootstrap renderables so SkiaHostTestBase.Dispose can sweep
        // anything a test leaks onto the shared layers, keeping tests order-independent.
        SkiaHostTestBase.CaptureRenderableBaseline();

        // Tests that add a mock keyboard must be the only registered one.
        FrameworkElement.KeyboardsForUiControl.Clear();
    }

    /// <summary>
    /// Initializes the host's <c>GumService.Default</c> against <paramref name="canvas"/> at
    /// <see cref="CanvasWidth"/> by <see cref="CanvasHeight"/>, with input devices that report nothing
    /// (Forms-input tests attach their own). Called from the constructor, so it must not rely on state
    /// the derived class sets up itself.
    /// </summary>
    protected abstract void InitializeGum(SKCanvas canvas);
}
