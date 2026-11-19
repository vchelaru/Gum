using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A <c>[Custom]</c> callback runs on every draw, as on MonoGame and raylib, so a time-driven effect moves (#8308).
/// </summary>
public class TextCustomEveryDrawTests : BaseTestClass
{
    private static Text MakeText(string rawText)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 40;
        text.Width = 1000;
        text.Color = SKColors.Black;
        text.X = 20;
        text.Y = 20;
        text.RawText = rawText;
        return text;
    }

    private static SKBitmap Draw(Text text)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 200));
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new() { Canvas = surface.Canvas };
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static int InkTop(SKBitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 128)
                {
                    return y;
                }
            }
        }
        return -1;
    }

    private static SKColor DarkestInk(SKBitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha == 255)
                {
                    return pixel;
                }
            }
        }
        return SKColors.Transparent;
    }

    [Fact]
    public void Render_CustomOffsetChangesBetweenDraws_MovesTheLetter()
    {
        float yOffset = 0;
        Text.Customizations["Wave"] = (index, block) => new LetterCustomization { YOffset = yOffset };
        Text text = MakeText("[Custom=Wave]I[/Custom]");

        using SKBitmap first = Draw(text);
        yOffset = 30;
        using SKBitmap second = Draw(text);

        ((float)(InkTop(second) - InkTop(first))).ShouldBe(30f, tolerance: 1);
    }

    [Fact]
    public void Render_CustomOffsetChangesAfterBlobsAreBuilt_MovesTheLetter()
    {
        float yOffset = 0;
        Text.Customizations["Wave"] = (index, block) => new LetterCustomization { YOffset = yOffset };
        Text text = MakeText("[Custom=Wave]I[/Custom]");

        using SKBitmap first = Draw(text);
        Draw(text).Dispose();
        Draw(text).Dispose();
        yOffset = 30;
        using SKBitmap moved = Draw(text);

        ((float)(InkTop(moved) - InkTop(first))).ShouldBe(30f, tolerance: 1);
    }

    [Fact]
    public void Render_CustomOffsetChangesBetweenDraws_KeepsTheLaidOutBlock()
    {
        float yOffset = 0;
        Text.Customizations["Wave"] = (index, block) => new LetterCustomization { YOffset = yOffset };
        Text text = MakeText("[Custom=Wave]I[/Custom]");

        Draw(text).Dispose();
        Topten.RichTextKit.TextBlock firstBlock = text.GetCachedTextBlock();
        yOffset = 30;
        Draw(text).Dispose();

        text.GetCachedTextBlock().ShouldBeSameAs(firstBlock);
    }

    [Fact]
    public void Render_CustomScaleChangesBetweenDraws_ResizesTheLetter()
    {
        float scale = 1.5f;
        Text.Customizations["Grow"] = (index, block) => new LetterCustomization { ScaleX = scale, ScaleY = scale };
        Text text = MakeText("[Custom=Grow]I[/Custom]");

        using SKBitmap small = Draw(text);
        scale = 3f;
        using SKBitmap large = Draw(text);

        InkTop(large).ShouldBeLessThan(InkTop(small) - 5);
    }

    [Fact]
    public void Render_CustomColorChangesBetweenDraws_RecolorsTheLetter()
    {
        System.Drawing.Color color = System.Drawing.Color.Red;
        Text.Customizations["Pulse"] = (index, block) => new LetterCustomization { Color = color };
        Text text = MakeText("[Custom=Pulse]I[/Custom]");

        using SKBitmap first = Draw(text);
        color = System.Drawing.Color.Blue;
        using SKBitmap second = Draw(text);

        DarkestInk(first).ShouldBe(new SKColor(255, 0, 0, 255));
        DarkestInk(second).ShouldBe(new SKColor(0, 0, 255, 255));
    }

    [Fact]
    public void Render_CustomOutputUnchangedBetweenDraws_KeepsTheLaidOutBlock()
    {
        Text.Customizations["Still"] = (index, block) => new LetterCustomization { YOffset = 5 };
        Text text = MakeText("[Custom=Still]I[/Custom]");

        Draw(text).Dispose();
        Topten.RichTextKit.TextBlock firstBlock = text.GetCachedTextBlock();
        Draw(text).Dispose();

        text.GetCachedTextBlock().ShouldBeSameAs(firstBlock);
    }

    [Fact]
    public void Render_CustomCallback_RunsOncePerLetterPerDraw()
    {
        int calls = 0;
        Text.Customizations["Count"] = (index, block) =>
        {
            calls++;
            return new LetterCustomization { YOffset = calls };
        };
        Text text = MakeText("[Custom=Count]I[/Custom]");
        Draw(text).Dispose();
        calls = 0;

        Draw(text).Dispose();

        calls.ShouldBe(1);
    }
}
