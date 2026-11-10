using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaSharp;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// <see cref="Text.Render"/> replays a recorded picture of its text block and keeps its effect paint
/// between frames. These pin that the replay draws what a direct paint draws and that every input the
/// caches depend on still reaches the screen.
/// </summary>
public class TextRenderCacheTests
{
    private static Text CreateText(string rawText, bool hasDropshadow = false, float shadowOffsetX = 0)
    {
        Text text = new Text();
        text.RawText = rawText;
        text.FontSize = 24;
        text.Width = 160;
        text.Height = 60;
        text.HasDropshadow = hasDropshadow;
        text.DropshadowColor = SKColors.Red;
        text.DropshadowOffsetX = shadowOffsetX;
        return text;
    }

    private static byte[] RenderFrames(Text text, int frames, float canvasScale = 1f)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(240, 100));
        surface.Canvas.Scale(canvasScale);
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };

        for (int i = 0; i < frames; i++)
        {
            surface.Canvas.Clear(SKColors.Transparent);
            text.Render(managers);
        }

        return Snapshot(surface);
    }

    private static byte[] Snapshot(SKSurface surface)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Render_ReplayedPicture_MatchesPaintingTheBlockDirectly(float canvasScale)
    {
        Text text = CreateText("Hello world, wrapped over lines");
        using SKSurface direct = SKSurface.Create(new SKImageInfo(240, 100));
        direct.Canvas.Scale(canvasScale);
        text.GetCachedTextBlock().Paint(direct.Canvas, new SKPoint(0, 0));
        byte[] expected = Snapshot(direct);

        byte[] firstFrame = RenderFrames(CreateText("Hello world, wrapped over lines"), frames: 1, canvasScale);
        byte[] replayedFrame = RenderFrames(CreateText("Hello world, wrapped over lines"), frames: 3, canvasScale);

        firstFrame.ShouldBe(expected);
        replayedFrame.ShouldBe(expected);
    }

    [Fact]
    public void Render_TextChangedAfterFirstFrame_DrawsTheNewText()
    {
        Text changed = CreateText("First");
        RenderFrames(changed, frames: 2);
        changed.RawText = "Second text";

        byte[] actual = RenderFrames(changed, frames: 1);

        actual.ShouldBe(RenderFrames(CreateText("Second text"), frames: 1));
    }

    [Fact]
    public void Render_DropshadowChangedAfterFirstFrame_DrawsTheNewShadow()
    {
        Text changed = CreateText("Shadow", hasDropshadow: true, shadowOffsetX: 4);
        RenderFrames(changed, frames: 2);
        changed.DropshadowOffsetX = 20;

        byte[] actual = RenderFrames(changed, frames: 1);

        actual.ShouldBe(RenderFrames(CreateText("Shadow", hasDropshadow: true, shadowOffsetX: 20), frames: 1));
    }

    [Fact]
    public void Render_DropshadowTurnedOffAfterFirstFrame_DrawsNoShadow()
    {
        Text changed = CreateText("Shadow", hasDropshadow: true, shadowOffsetX: 20);
        RenderFrames(changed, frames: 2);
        changed.HasDropshadow = false;

        byte[] actual = RenderFrames(changed, frames: 1);

        actual.ShouldBe(RenderFrames(CreateText("Shadow"), frames: 1));
    }

    [Fact]
    public void SettingFontValuesToWhatTheyAlreadyAre_KeepsTheCachedTextBlock()
    {
        Text text = CreateText("Hello");
        text.FontName = "Arial";
        TextBlock block = text.GetCachedTextBlock();

        text.FontName = "Arial";
        text.FontSize = text.FontSize;
        text.BoldWeight = text.BoldWeight;
        text.Color = text.Color;

        text.GetCachedTextBlock().ShouldBeSameAs(block);
    }

    [Theory]
    [InlineData("FontName")]
    [InlineData("FontSize")]
    [InlineData("BoldWeight")]
    [InlineData("Color")]
    public void ChangingAFontValue_BuildsANewTextBlock(string property)
    {
        Text text = CreateText("Hello");
        text.FontName = "Arial";
        TextBlock block = text.GetCachedTextBlock();

        switch (property)
        {
            case "FontName":
                text.FontName = "Courier New";
                break;
            case "FontSize":
                text.FontSize = text.FontSize + 2;
                break;
            case "BoldWeight":
                text.BoldWeight = 1.5f;
                break;
            case "Color":
                text.Color = SKColors.Blue;
                break;
        }

        text.GetCachedTextBlock().ShouldNotBeSameAs(block);
    }

    // Layout measures a size-to-children text unbounded, then sets Width to the measured width. A left-aligned
    // block that already fits lays out the same at that width, so it is kept (#7818).
    [Fact]
    public void GetCachedTextBlock_LeftAlignedWidthSetToMeasuredWidth_KeepsTheBlock()
    {
        Text text = CreateText("Hello world");
        text.Width = null;
        TextBlock unbounded = text.GetCachedTextBlock();

        text.Width = unbounded.MeasuredWidth;

        text.GetCachedTextBlock().ShouldBeSameAs(unbounded);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Center, 0)]
    [InlineData(HorizontalAlignment.Right, 0)]
    [InlineData(HorizontalAlignment.Left, -1)]
    public void GetCachedTextBlock_WidthNeedsANewLayout_BuildsANewBlock(HorizontalAlignment alignment, float widthChange)
    {
        Text text = CreateText("Hello world");
        text.HorizontalAlignment = alignment;
        text.Width = null;
        TextBlock unbounded = text.GetCachedTextBlock();

        text.Width = unbounded.MeasuredWidth + widthChange;

        text.GetCachedTextBlock().ShouldNotBeSameAs(unbounded);
    }

    [Theory]
    [InlineData("Hello world")]
    [InlineData("Trailing space ")]
    [InlineData("Two\nlines")]
    [InlineData("[Color=Red]Hello[/Color] [FontSize=40]big[/FontSize] world")]
    public void Render_LeftAlignedWidthSetToMeasuredWidth_MatchesAFreshLayoutAtThatWidth(string rawText)
    {
        Text sized = CreateText(rawText);
        sized.Width = null;
        float measuredWidth = sized.GetCachedTextBlock().MeasuredWidth;
        sized.Width = measuredWidth;
        Text fresh = CreateText(rawText);
        fresh.Width = measuredWidth;

        RenderFrames(sized, frames: 1).ShouldBe(RenderFrames(fresh, frames: 1));
    }

    [Fact]
    public void Render_WrappingTextWidthSetToFractionalMeasuredWidth_MatchesAFreshLayoutAtThatWidth()
    {
        const string rawText = "Hello wide world";
        Text sized = CreateText(rawText);
        sized.Width = null;
        float measuredWidth = sized.GetCachedTextBlock().MeasuredWidth;
        measuredWidth.ShouldNotBe(MathF.Floor(measuredWidth));
        sized.Width = measuredWidth;
        Text fresh = CreateText(rawText);
        fresh.Width = measuredWidth;

        LineBreaks(sized).ShouldBe(LineBreaks(fresh));
        RenderFrames(sized, frames: 1).ShouldBe(RenderFrames(fresh, frames: 1));
    }

    [Fact]
    public void Render_WidthSetBelowMeasuredWidth_RebuildsAndWrapsLikeAFreshLayout()
    {
        const string rawText = "Hello wide world";
        Text sized = CreateText(rawText);
        sized.Width = null;
        TextBlock unbounded = sized.GetCachedTextBlock();
        float narrowerWidth = unbounded.MeasuredWidth - 0.5f;
        sized.Width = narrowerWidth;
        Text fresh = CreateText(rawText);
        fresh.Width = narrowerWidth;

        sized.GetCachedTextBlock().ShouldNotBeSameAs(unbounded);
        sized.GetCachedTextBlock().Lines.Count.ShouldBeGreaterThan(1);
        LineBreaks(sized).ShouldBe(LineBreaks(fresh));
        RenderFrames(sized, frames: 1).ShouldBe(RenderFrames(fresh, frames: 1));
    }

    private static (int Start, int Length)[] LineBreaks(Text text) =>
        text.GetCachedTextBlock().Lines.Select(line => (line.Start, line.Length)).ToArray();

    [Fact]
    public void GetPaintTextBlock_RevealUnchanged_ReturnsTheSameBlock()
    {
        Text text = CreateText("Hello world, wrapped over lines");
        text.MaxLettersToShow = 5;
        TextBlock full = text.GetCachedTextBlock();
        TextBlock revealed = text.GetPaintTextBlock(full);

        text.GetPaintTextBlock(full).ShouldBeSameAs(revealed);
    }

    [Fact]
    public void Render_RevealAdvancedAfterFirstFrames_DrawsTheNewReveal()
    {
        Text changed = CreateText("Hello world, wrapped over lines");
        changed.MaxLettersToShow = 3;
        RenderFrames(changed, frames: 3);
        changed.MaxLettersToShow = 9;

        byte[] actual = RenderFrames(changed, frames: 1);

        Text expected = CreateText("Hello world, wrapped over lines");
        expected.MaxLettersToShow = 9;
        actual.ShouldBe(RenderFrames(expected, frames: 1));
    }

    [Fact]
    public void Render_TextChangedEveryFrame_DrawsTheLatestText()
    {
        Text changed = CreateText("Frame 1");
        RenderFrames(changed, frames: 1);
        changed.RawText = "Frame 2";
        RenderFrames(changed, frames: 1);
        changed.RawText = "Frame 3";

        byte[] actual = RenderFrames(changed, frames: 2);

        actual.ShouldBe(RenderFrames(CreateText("Frame 3"), frames: 1));
    }

    [Fact]
    public void Clone_ChangingTheCloneText_LeavesTheSourceDrawingItsOwnText()
    {
        Text source = CreateText("Original", hasDropshadow: true, shadowOffsetX: 6);
        RenderFrames(source, frames: 2);
        Text clone = source.Clone();

        clone.RawText = "Changed";
        clone.DropshadowOffsetX = 18;
        RenderFrames(clone, frames: 2);
        byte[] actual = RenderFrames(source, frames: 1);

        actual.ShouldBe(RenderFrames(CreateText("Original", hasDropshadow: true, shadowOffsetX: 6), frames: 1));
    }
}
