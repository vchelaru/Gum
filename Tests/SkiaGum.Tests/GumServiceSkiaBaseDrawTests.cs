using Gum;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests;

public class GumServiceSkiaBaseDrawTests : BaseTestClass
{
    // A host that owns its render target overrides Draw() to draw into that target's canvas.
    private sealed class OwnsItsCanvasService : GumServiceSkiaBase
    {
        public int DrawCallCount;
        public SKSurface? Surface;

        public override void Draw()
        {
            DrawCallCount++;
            Draw(Surface!.Canvas);
        }
    }

    [Fact]
    public void Draw_ThroughBaseTypedReference_ShouldRunTheHostsOverride()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(50, 50));
        // Creates the shared SystemManagers the base draws through.
        GumService.Default.Initialize(surface.Canvas, 50, 50);
        OwnsItsCanvasService service = new OwnsItsCanvasService { Surface = surface };
        GumServiceSkiaBase baseReference = service;

        baseReference.Draw();

        service.DrawCallCount.ShouldBe(1);
    }

    [Fact]
    public void DrawWithCanvas_FromInsideAnOverriddenDraw_ShouldNotRecurse()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(50, 50));
        // Creates the shared SystemManagers the base draws through.
        GumService.Default.Initialize(surface.Canvas, 50, 50);
        OwnsItsCanvasService service = new OwnsItsCanvasService { Surface = surface };

        service.Draw();

        service.DrawCallCount.ShouldBe(1);
    }
}
