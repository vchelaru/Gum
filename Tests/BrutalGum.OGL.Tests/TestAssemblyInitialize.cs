using Gum;
using SkiaHostTests;
using SkiaSharp;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("BrutalGum.Tests.TestAssemblyInitialize", "BrutalGum.OGL.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BrutalGum.Tests;

/// <summary>
/// Assembly-wide bootstrap: the shared <see cref="SkiaHostTestFramework"/> with a fake window that reports
/// nothing. Forms-input tests add their own.
/// </summary>
public class TestAssemblyInitialize : SkiaHostTestFramework
{
    /// <summary>The fake window the shared test service reads, for tests that script a gamepad.</summary>
    internal static FakeBrutalInput Input { get; } = new();

    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override void InitializeGum(SKCanvas canvas) =>
        GumService.Default.Initialize(canvas, Input, CanvasWidth, CanvasHeight);

    /// <summary>
    /// Initializes the shared service the way a game does, over <paramref name="input"/> and a fake GPU backend, runs
    /// <paramref name="body"/>, then puts the bootstrap's service back.
    /// </summary>
    internal static void WithBackend(FakeBrutalInput input, Action<FakeBrutalGpuBackend> body)
    {
        FakeBrutalGpuBackend backend = new FakeBrutalGpuBackend();
        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.Initialize(input, backend);
            body(backend);
        }
        finally
        {
            GumService.Default.Uninitialize();
            SKSurface surface = SKSurface.Create(new SKImageInfo(CanvasWidth, CanvasHeight));
            GumService.Default.Initialize(surface.Canvas, Input, CanvasWidth, CanvasHeight);
            SkiaHostTestBase.CaptureRenderableBaseline();
            Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Clear();
        }
    }
}
