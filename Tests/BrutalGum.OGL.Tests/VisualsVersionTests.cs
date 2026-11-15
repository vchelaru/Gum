using Gum;
using Gum.Forms;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;

namespace BrutalGum.Tests;

/// <summary>
/// The public Initialize overloads that take a DefaultVisualsVersion forward to the internal body, which
/// Skia restricts to V3.
/// </summary>
public class VisualsVersionTests : BaseTestClass
{
    private static void Restore()
    {
        GumService.Default.Uninitialize();
        SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
        GumService.Default.Initialize(surface.Canvas, TestAssemblyInitialize.Input, SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
        SkiaHostTestBase.CaptureRenderableBaseline();
        Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Clear();
    }

    [Fact]
    public void Initialize_WithV3_IsAccepted()
    {
        try
        {
            GumService.Default.Uninitialize();
            Should.NotThrow(() => GumService.Default.Initialize(new FakeBrutalInput(), new FakeBrutalGpuBackend(), null, DefaultVisualsVersion.V3));
            GumService.Default.IsInitialized.ShouldBeTrue();
        }
        finally
        {
            Restore();
        }
    }

    [Fact]
    public void Initialize_WithAnUnsupportedVersion_ThrowsNotSupportedAndStaysUninitialized()
    {
        try
        {
            GumService.Default.Uninitialize();
            Should.Throw<NotSupportedException>(() =>
                GumService.Default.Initialize(new FakeBrutalInput(), new FakeBrutalGpuBackend(), null, (DefaultVisualsVersion)99));
            GumService.Default.IsInitialized.ShouldBeFalse();
        }
        finally
        {
            Restore();
        }
    }
}
