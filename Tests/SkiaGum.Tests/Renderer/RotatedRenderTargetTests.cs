using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

/// <summary>
/// A rotated render-target container must look like the same container drawn without a render
/// target: its children turn with it and nothing inside its own rectangle is clipped (#6588).
/// </summary>
public class RotatedRenderTargetTests
{
    const int Size = 200;

    public RotatedRenderTargetTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(30f, 0f)]
    [InlineData(0f, 30f)]
    [InlineData(30f, -50f)]
    public void Draw_RenderTarget_MatchesSameContainerWithoutRenderTarget(float containerRotation, float parentRotation)
    {
        using SKBitmap plain = RenderScene(isRenderTarget: false, containerRotation, parentRotation);
        using SKBitmap viaRenderTarget = RenderScene(isRenderTarget: true, containerRotation, parentRotation);

        // Text resamples differently when the baked image is turned, so the comparison is by shape:
        // how many pixels each child covers and where their center is, not pixel for pixel.
        Coverage plainRed = Measure(plain, IsRed);
        Coverage renderTargetRed = Measure(viaRenderTarget, IsRed);
        Coverage plainText = Measure(plain, IsTextColored);
        Coverage renderTargetText = Measure(viaRenderTarget, IsTextColored);

        plainRed.Count.ShouldBeGreaterThan(300);
        plainText.Count.ShouldBeGreaterThan(30);
        renderTargetRed.Count.ShouldBeInRange(plainRed.Count * 9 / 10, plainRed.Count * 11 / 10);
        renderTargetRed.CenterX.ShouldBe(plainRed.CenterX, 1f);
        renderTargetRed.CenterY.ShouldBe(plainRed.CenterY, 1f);
        renderTargetText.Count.ShouldBeInRange(plainText.Count * 3 / 4, plainText.Count * 5 / 4);
        renderTargetText.CenterX.ShouldBe(plainText.CenterX, 1.5f);
        renderTargetText.CenterY.ShouldBe(plainText.CenterY, 1.5f);
    }

    record struct Coverage(int Count, float CenterX, float CenterY);

    static bool IsRed(SKColor c) => c.Red > 150 && c.Green < 80 && c.Blue < 80;

    static bool IsTextColored(SKColor c) => c.Red > 100 && c.Green > 100 && c.Blue > 100;

    static Coverage Measure(SKBitmap bitmap, System.Func<SKColor, bool> predicate)
    {
        int count = 0;
        long sumX = 0;
        long sumY = 0;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (predicate(bitmap.GetPixel(x, y)))
                {
                    count++;
                    sumX += x;
                    sumY += y;
                }
            }
        }
        return count == 0 ? new Coverage(0, -1, -1) : new Coverage(count, sumX / (float)count, sumY / (float)count);
    }

    // Pins the reference scene itself: the corner rectangle of a container rotated 30 degrees
    // counterclockwise lands above the container's origin, outside its axis-aligned rectangle.
    [Fact]
    public void Draw_RotatedPlainContainer_PutsCornerChildAboveOrigin()
    {
        using SKBitmap plain = RenderScene(isRenderTarget: false, containerRotation: 30, parentRotation: 0);

        // Local (50, 10), the corner rectangle's center, turned 30 degrees counterclockwise about
        // the container origin (60, 60): (60 + 50cos30 + 10sin30, 60 - 50sin30 + 10cos30).
        plain.GetPixel(108, 44).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Draw_RotatedRenderTarget_DrawsCornerChildOutsideAxisAlignedRectangle()
    {
        using SKBitmap viaRenderTarget = RenderScene(isRenderTarget: true, containerRotation: 30, parentRotation: 0);

        viaRenderTarget.GetPixel(108, 44).ShouldBe(SKColors.Red);
    }

    private static SKBitmap RenderScene(bool isRenderTarget, float containerRotation, float parentRotation)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(Size, Size));
        GumService.Default.Initialize(surface.Canvas, Size, Size);

        ContainerRuntime parent = new() { X = 60, Y = 60, Width = 0, Height = 0, Rotation = parentRotation };
        ContainerRuntime container = new()
        {
            Width = 60,
            Height = 60,
            Rotation = containerRotation,
            IsRenderTarget = isRenderTarget,
        };
        container.Children.Add(new RectangleRuntime
        {
            X = 40,
            Y = 0,
            Width = 20,
            Height = 20,
            IsFilled = true,
            FillColor = SKColors.Red,
        });
        container.Children.Add(new TextRuntime { X = 0, Y = 35, Text = "WW", Color = SKColors.White });
        parent.Children.Add(container);
        GumService.Default.Root.Children.Add(parent);

        surface.Canvas.Clear(SKColors.Black);
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        SKBitmap bitmap = SKBitmap.FromImage(image);

        string? outDir = System.Environment.GetEnvironmentVariable("GUM_6588_OUT");
        if (outDir != null)
        {
            using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir,
                $"6588-skia-{(isRenderTarget ? "rt" : "plain")}-{containerRotation}-{parentRotation}.png"), png.ToArray());
        }

        GumService.Default.Root.Children.Remove(parent);
        return bitmap;
    }
}
