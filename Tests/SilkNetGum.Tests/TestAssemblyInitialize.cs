using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Silk.NET.Input;
using SkiaSharp;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("SilkNetGum.Tests.TestAssemblyInitialize", "SilkNetGum.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SilkNetGum.Tests;

/// <summary>
/// Assembly-wide test bootstrap for SilkNetGum. Unlike the Raylib bootstrap (which opens a hidden
/// GL window), SkiaGum rendering is CPU raster + RichTextKit text measurement, so this bootstraps
/// against an in-memory <see cref="SKSurface"/> with no window / GL context. Input is supplied by a
/// mocked <see cref="IInputContext"/> with no devices — Forms-input tests add their own mocks.
/// Drives Gum through the real <see cref="GumService.Initialize(SKCanvas, IInputContext, int, int, string?)"/>
/// so <c>new Button()</c> etc. produce a control with a valid V3 Visual on Skia.
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

        // An input context with no mice/keyboards: CreateCursor yields a device-less cursor and
        // CreateKeyboard yields null, which is fine for tests (Forms-input tests add mock devices).
        var inputContext = new Mock<IInputContext>();
        inputContext.SetupGet(x => x.Mice).Returns(new List<IMouse>());
        inputContext.SetupGet(x => x.Keyboards).Returns(new List<IKeyboard>());

        GumService.Default.Initialize(_surface.Canvas, inputContext.Object, 800, 600);

        // #3066 pattern: record post-bootstrap renderables so BaseTestClass.Dispose can sweep
        // anything a test leaks onto the shared layers, keeping tests order-independent.
        BaseTestClass.CaptureRenderableBaseline();

        // No real keyboard is registered (the mock context had none); ensure the list is clean so
        // tests that Add a Mock<IInputReceiverKeyboard> are the sole registered keyboard.
        FrameworkElement.KeyboardsForUiControl.Clear();
    }
}
