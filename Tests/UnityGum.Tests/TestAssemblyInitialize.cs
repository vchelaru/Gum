using Gum;
using Gum.Forms.Controls;
using SkiaSharp;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("UnityGum.Tests.TestAssemblyInitialize", "UnityGum.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace UnityGum.Tests;

/// <summary>
/// Assembly-wide bootstrap. Initializes the Unity <see cref="GumService"/> once against an in-memory
/// raster <see cref="SKSurface"/>, the same canvas the Unity package's CPU fallback draws into.
/// </summary>
public class TestAssemblyInitialize : XunitTestFramework
{
    public const int CanvasWidth = 800;
    public const int CanvasHeight = 600;

    // Kept alive for the whole run: the surface backs SystemManagers.Default.Canvas.
    private static SKSurface? _surface;

    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
        _surface = SKSurface.Create(new SKImageInfo(CanvasWidth, CanvasHeight));
        GumService.Default.Initialize(_surface.Canvas, CanvasWidth, CanvasHeight);

        BaseTestClass.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }
}
