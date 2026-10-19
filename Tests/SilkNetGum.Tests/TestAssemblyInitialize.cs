using Gum;
using Moq;
using Silk.NET.Input;
using SkiaHostTests;
using SkiaSharp;
using System.Collections.Generic;
using Xunit.Abstractions;

[assembly: Xunit.TestFramework("SilkNetGum.Tests.TestAssemblyInitialize", "SilkNetGum.Tests")]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SilkNetGum.Tests;

/// <summary>
/// Assembly-wide test bootstrap for SilkNetGum: the shared <see cref="SkiaHostTestFramework"/> with a
/// mocked <see cref="IInputContext"/> that has no devices. Forms-input tests add their own mocks.
/// Drives Gum through the real <see cref="GumService.Initialize(SKCanvas, IInputContext, int, int, string?)"/>
/// so <c>new Button()</c> etc. produce a control with a valid V3 Visual on Skia.
/// </summary>
public class TestAssemblyInitialize : SkiaHostTestFramework
{
    public TestAssemblyInitialize(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override void InitializeGum(SKCanvas canvas)
    {
        // An input context with no mice/keyboards: CreateCursor yields a device-less cursor and
        // CreateKeyboard yields an inert keyboard, which is fine for tests.
        var inputContext = new Mock<IInputContext>();
        inputContext.SetupGet(x => x.Mice).Returns(new List<IMouse>());
        inputContext.SetupGet(x => x.Keyboards).Returns(new List<IKeyboard>());
        inputContext.SetupGet(x => x.Gamepads).Returns(new List<IGamepad>());

        GumService.Default.Initialize(canvas, inputContext.Object, CanvasWidth, CanvasHeight);
    }
}
