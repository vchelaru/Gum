using Gum;
using RenderingLibrary;
using Shouldly;
using SkiaHostTests;
using SkiaSharp;

namespace BrutalGum.Tests;

public class GumServiceTests : BaseTestClass
{
    [Fact]
    public void Clipboard_IsAssigned_AfterInitialize()
    {
        GumService.Default.Clipboard.ShouldNotBeNull();
    }

    [Fact]
    public void Initialize_WithoutCanvas_Succeeds()
    {
        // BRUTAL's canvas only exists between Begin() and End(), so Initialize must not need one.
        GumService.Default.Uninitialize();
        try
        {
            GumService.Default.Initialize(new FakeBrutalInput(), 640, 480);

            GumService.Default.IsInitialized.ShouldBeTrue();
        }
        finally
        {
            GumService.Default.Uninitialize();
            SKSurface surface = SKSurface.Create(new SKImageInfo(SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight));
            GumService.Default.Initialize(surface.Canvas, new FakeBrutalInput(), SkiaHostTestFramework.CanvasWidth, SkiaHostTestFramework.CanvasHeight);
            SkiaHostTestBase.CaptureRenderableBaseline();
            Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Clear();
        }
    }

    [Fact]
    public void Update_WithFakeWindow_DoesNotThrow()
    {
        Should.NotThrow(() => GumService.Default.Update(0));
    }

    [Fact]
    public void ApplyGamePadState_WithNoGamepad_LeavesItDisconnected()
    {
        Gum.Input.GamePad gamePad = new();

        ((IGumService)GumService.Default).ApplyGamePadState(gamePad, 0, 1);

        gamePad.IsConnected.ShouldBeFalse();
    }
}
