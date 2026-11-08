using Gum;
using Gum.Forms.Controls;
using Moq;
using Silk.NET.Input;
using SkiaHostTests;
using SkiaSharp;
using System.Collections.Generic;

namespace SilkNetGum.Tests;

/// <summary>
/// The base owns the order in which a host's devices are attached; the cases live in
/// <see cref="HostDeviceAttachmentTestsBase"/>. This supplies how to put the shared Silk service back.
/// </summary>
public class HostDeviceAttachmentTests : HostDeviceAttachmentTestsBase
{
    protected override void RestoreSharedService()
    {
        Mock<IMouse> mouse = new Mock<IMouse>();
        mouse.SetupGet(m => m.ScrollWheels).Returns(new List<ScrollWheel>());
        Mock<IInputContext> context = new Mock<IInputContext>();
        context.SetupGet(c => c.Keyboards).Returns(new List<IKeyboard> { new Mock<IKeyboard>().Object });
        context.SetupGet(c => c.Mice).Returns(new List<IMouse> { mouse.Object });
        context.SetupGet(c => c.Gamepads).Returns(new List<IGamepad>());

        GumService.Default.Uninitialize();
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.Initialize(surface.Canvas, context.Object, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
        SkiaHostTestBase.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }
}
