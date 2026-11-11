using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests;

/// <summary>
/// Pins the sequence the "Rendering to an Image" docs page shows: the caller owns an offscreen
/// <see cref="SKSurface"/>, Gum draws into it, and the caller reads the bytes back.
/// </summary>
public class GumServiceRenderToImageTests : BaseTestClass
{
    private static RectangleRuntime AddRedRectangle()
    {
        RectangleRuntime rectangle = new()
        {
            X = 10,
            Y = 10,
            Width = 50,
            Height = 40,
            IsFilled = true,
            FillColor = SKColors.Red,
        };
        rectangle.AddToRoot();
        return rectangle;
    }

    private static SKColor ReadPixel(SKSurface surface, int x, int y)
    {
        using SKImage image = surface.Snapshot();
        using SKPixmap pixmap = image.PeekPixels();
        return pixmap.GetPixelColor(x, y);
    }

    [Fact]
    public void Draw_AfterShortInitialize_ProducesPngWithRectangleOnTransparentBackground()
    {
        // The same sequence as the docs sample.
        GumService gumUI = GumService.Default;

        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100, SKColorType.Rgba8888, SKAlphaType.Premul));
        gumUI.Initialize(surface.Canvas);

        AddRedRectangle();

        gumUI.Update(0);
        gumUI.Draw();

        using SKImage image = surface.Snapshot();
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
        byte[] bytes = png.ToArray();

        using SKBitmap decoded = SKBitmap.Decode(bytes);
        decoded.Width.ShouldBe(200);
        decoded.Height.ShouldBe(100);
        decoded.GetPixel(30, 30).ShouldBe(SKColors.Red);
        // Gum does not clear the surface, so everything it did not draw stays transparent.
        decoded.GetPixel(150, 80).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Initialize_OnFreshSurface_ReadsSizeFromClipBoundsLikeTheExplicitOverload()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));

        GumService.Default.Initialize(surface.Canvas);
        float shortWidth = GraphicalUiElement.CanvasWidth;
        float shortHeight = GraphicalUiElement.CanvasHeight;

        GumService.Default.Initialize(surface.Canvas, 200, 100);

        shortWidth.ShouldBe(200);
        shortHeight.ShouldBe(100);
        GraphicalUiElement.CanvasWidth.ShouldBe(200);
        GraphicalUiElement.CanvasHeight.ShouldBe(100);
    }

    [Fact]
    public void Draw_AfterClearingBeforeDraw_KeepsBackgroundBehindGum()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas);
        AddRedRectangle();

        surface.Canvas.Clear(SKColors.White);
        GumService.Default.Update(0);
        GumService.Default.Draw();

        ReadPixel(surface, 30, 30).ShouldBe(SKColors.Red);
        ReadPixel(surface, 150, 80).ShouldBe(SKColors.White);
    }

    [Fact]
    public void Draw_CalledAgainOnTheSameService_RendersTheChangedScreenIntoANewSurface()
    {
        using SKSurface first = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(first.Canvas);
        RectangleRuntime rectangle = AddRedRectangle();
        GumService.Default.Update(0);
        GumService.Default.Draw();

        rectangle.FillColor = SKColors.Blue;
        using SKSurface second = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Update(0);
        GumService.Default.Draw(second.Canvas);

        ReadPixel(first, 30, 30).ShouldBe(SKColors.Red);
        ReadPixel(second, 30, 30).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void Initialize_CalledAgainWithoutUninitialize_RendersTheNewScreenOnly()
    {
        using SKSurface first = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(first.Canvas);
        AddRedRectangle();
        GumService.Default.Update(0);
        GumService.Default.Draw();

        using SKSurface second = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(second.Canvas);
        GumService.Default.Update(0);
        GumService.Default.Draw();

        // The first screen is gone; only what was added after the second Initialize draws.
        ReadPixel(second, 30, 30).Alpha.ShouldBe((byte)0);
    }
}
