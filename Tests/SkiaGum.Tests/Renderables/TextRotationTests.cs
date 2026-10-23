using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Text turns by its absolute rotation (its own plus every parent's), as on MonoGame, where
/// <c>RenderingLibrary.Graphics.Text</c> draws with <c>GetAbsoluteRotation()</c>.
/// </summary>
public class TextRotationTests : BaseTestClass
{
    const int Size = 120;

    [Fact]
    public void Render_InRotatedParent_MatchesTextWithTheSummedRotation()
    {
        using SKBitmap inParent = RenderText(parentRotation: 60, textRotation: 30);
        using SKBitmap standalone = RenderText(parentRotation: null, textRotation: 90);

        CountDifferentPixels(inParent, standalone).ShouldBe(0);
    }

    [Fact]
    public void Render_InUnrotatedParent_MatchesTextWithNoParent()
    {
        using SKBitmap inParent = RenderText(parentRotation: 0, textRotation: 90);
        using SKBitmap standalone = RenderText(parentRotation: null, textRotation: 90);

        CountDifferentPixels(inParent, standalone).ShouldBe(0);
    }

    // Gum rotation is counterclockwise about the top-left, so text at (60, 60) turned 90 degrees reads
    // upward: its ink is above y 60 and right of x 60.
    [Fact]
    public void Render_RotatedWithNoParent_TurnsCounterclockwiseAboutTopLeft()
    {
        using SKBitmap bitmap = RenderText(parentRotation: null, textRotation: 90);

        (int left, int top, int right, int bottom) = InkBounds(bitmap);
        left.ShouldBeGreaterThanOrEqualTo(60 - 1);
        bottom.ShouldBeLessThanOrEqualTo(60 + 1);
        top.ShouldBeLessThan(60 - 10);
        right.ShouldBeGreaterThan(60 + 5);
    }

    static SKBitmap RenderText(float? parentRotation, float textRotation)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(Size, Size));
        surface.Canvas.Clear(SKColors.Transparent);
        Gum.GumService.Default.Initialize(surface.Canvas, Size, Size);
        TextRuntime text = new() { Text = "Hello", Rotation = textRotation };
        if (parentRotation is float rotation)
        {
            ContainerRuntime parent = new() { X = 60, Y = 60, Width = 10, Height = 10, Rotation = rotation };
            parent.Children.Add(text);
            Gum.GumService.Default.Root.Children.Add(parent);
        }
        else
        {
            text.X = 60;
            text.Y = 60;
            Gum.GumService.Default.Root.Children.Add(text);
        }

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    static int CountDifferentPixels(SKBitmap a, SKBitmap b)
    {
        int count = 0;
        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                if (a.GetPixel(x, y) != b.GetPixel(x, y))
                {
                    count++;
                }
            }
        }
        return count;
    }

    static (int left, int top, int right, int bottom) InkBounds(SKBitmap bitmap)
    {
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha != 0)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
            }
        }
        left.ShouldNotBe(int.MaxValue, "nothing was drawn");
        return (left, top, right, bottom);
    }
}
