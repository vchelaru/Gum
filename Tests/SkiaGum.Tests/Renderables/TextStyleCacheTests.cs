using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

public class TextStyleCacheTests
{
    private static Text CreateText(string rawText)
    {
        Text text = new Text();
        text.RawText = rawText;
        text.FontSize = 24;
        text.Width = 200;
        text.Height = 100;
        return text;
    }

    private static byte[] Render(Text text)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(260, 160));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        surface.Canvas.Clear(SKColors.Transparent);
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    [Theory]
    [InlineData("[Color=Red]ab[/Color]\nline", "[Color=Blue]ab[/Color]\nline")]
    [InlineData("[FontSize=20]ab[/FontSize]\nline", "[FontSize=30]ab[/FontSize]\nline")]
    [InlineData("[FontScale=1]ab[/FontScale]\nline", "[FontScale=1.5]ab[/FontScale]\nline")]
    [InlineData("[IsBold=false]ab[/IsBold]\nline", "[IsBold=true]ab[/IsBold]\nline")]
    [InlineData("[IsItalic=false]ab[/IsItalic]\nline", "[IsItalic=true]ab[/IsItalic]\nline")]
    [InlineData("[Font=Arial]ab[/Font]\nline", "[Font=Courier New]ab[/Font]\nline")]
    [InlineData("[OutlineThickness=1]ab[/OutlineThickness]\nline", "[OutlineThickness=3]ab[/OutlineThickness]\nline")]
    public void OneFieldDiffers_SecondRenderMatchesFreshAndDiffersFromFirst(string first, string second)
    {
        Text changed = CreateText(first);
        changed.OutlineColor = SKColors.Black;
        byte[] firstPixels = Render(changed);
        changed.RawText = second;
        byte[] secondPixels = Render(changed);

        Text fresh = CreateText(second);
        fresh.OutlineColor = SKColors.Black;
        secondPixels.ShouldBe(Render(fresh));
        secondPixels.ShouldNotBe(firstPixels);
    }

    [Fact]
    public void LineHeightMultiplierDiffers_SecondRenderMatchesFresh()
    {
        Text changed = CreateText("[Color=Red]ab[/Color]\nline");
        byte[] firstPixels = Render(changed);
        changed.LineHeightMultiplier = 2;
        changed.RawText = "[Color=Red]ab[/Color]\nline ";
        changed.RawText = "[Color=Red]ab[/Color]\nline";
        byte[] secondPixels = Render(changed);

        Text fresh = CreateText("[Color=Red]ab[/Color]\nline");
        fresh.LineHeightMultiplier = 2;
        secondPixels.ShouldBe(Render(fresh));
        secondPixels.ShouldNotBe(firstPixels);
    }

    [Fact]
    public void OutlineColorDiffers_SecondRenderMatchesFresh()
    {
        Text changed = CreateText("[OutlineThickness=3]ab[/OutlineThickness]");
        changed.OutlineColor = SKColors.Black;
        byte[] firstPixels = Render(changed);
        changed.OutlineColor = SKColors.Green;
        changed.RawText = "[OutlineThickness=3]ab[/OutlineThickness] ";
        byte[] secondPixels = Render(changed);

        Text fresh = CreateText("[OutlineThickness=3]ab[/OutlineThickness] ");
        fresh.OutlineColor = SKColors.Green;
        secondPixels.ShouldBe(Render(fresh));
        secondPixels.ShouldNotBe(firstPixels);
    }

    [Fact]
    public void MoreThan64DistinctStyles_EachRenderMatchesFresh()
    {
        Text changed = CreateText("x");
        for (int i = 0; i < 140; i++)
        {
            string raw = $"[Red={(i * 7) % 256}]ab[/Red] [FontSize={10 + (i % 50)}]c[/FontSize]";
            changed.RawText = raw;
            byte[] actual = Render(changed);
            if (i % 10 == 0 || i >= 60 && i <= 70 || i > 130)
            {
                actual.ShouldBe(Render(CreateText(raw)), $"iteration {i}");
            }
        }
    }
}
