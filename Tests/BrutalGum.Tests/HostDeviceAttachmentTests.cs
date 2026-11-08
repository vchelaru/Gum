using Gum;
using Gum.Forms.Controls;
using SkiaHostTests;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>The base owns the order devices attach in; this supplies how to restore the shared service.</summary>
public class HostDeviceAttachmentTests : HostDeviceAttachmentTestsBase
{
    protected override void RestoreSharedService()
    {
        GumService.Default.Uninitialize();
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.Initialize(surface.Canvas, new FakeBrutalInput(), SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
        SkiaHostTestBase.CaptureRenderableBaseline();
        FrameworkElement.KeyboardsForUiControl.Clear();
    }
}
