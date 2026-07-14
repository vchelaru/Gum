using Shouldly;
using SkiaGum;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that SkiaGum's <see cref="Text"/> honors inline BBCode styling (issue #3679) by parsing
/// the markup into styled runs and feeding RichTextKit one <see cref="Style"/> per run, matching the
/// per-run color / font-size / font-scale / bold / italic styling the MonoGame and Raylib backends
/// already support. Plain (no-markup) text must remain a single-run fast path.
/// </summary>
public class TextBbCodeTests
{
    private static Text MakeText()
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = 1000;
        text.Color = SKColors.Black;
        return text;
    }

    [Fact]
    public void GetStyledRuns_BoldTag_ProducesBoldRun()
    {
        Text text = MakeText();
        text.RawText = "plain [IsBold=true]bold[/IsBold] plain";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "bold").Style.FontWeight.ShouldBe(700);
        runs.First(r => r.Text.StartsWith("plain")).Style.FontWeight.ShouldBe(400);
    }

    [Fact]
    public void GetStyledRuns_ColorTag_ProducesRunWithParsedColor()
    {
        Text text = MakeText();
        text.RawText = "plain [Color=Red]red[/Color] plain";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "red").Style.TextColor.ShouldBe(new SKColor(255, 0, 0, 255));
        runs.First(r => r.Text.StartsWith("plain")).Style.TextColor.ShouldBe(SKColors.Black);
    }

    [Fact]
    public void GetStyledRuns_FontScaleTag_ScalesRunFontSize()
    {
        Text text = MakeText();
        float baseFontSize = text.GetStyle().FontSize;
        text.RawText = "plain [FontScale=2]big[/FontScale] plain";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "big").Style.FontSize.ShouldBe(baseFontSize * 2);
    }

    [Fact]
    public void GetStyledRuns_FontSizeTag_OverridesRunFontSize()
    {
        Text text = MakeText();
        // Base FontSize is 20, so [FontSize=40] is exactly double once the shared
        // GlobalTextScale / FontScale factors (identical between base and run) cancel out.
        float baseFontSize = text.GetStyle().FontSize;
        text.RawText = "plain [FontSize=40]big[/FontSize] plain";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "big").Style.FontSize.ShouldBe(baseFontSize * 2);
    }

    [Fact]
    public void GetStyledRuns_ItalicTag_ProducesItalicRun()
    {
        Text text = MakeText();
        text.RawText = "plain [IsItalic=true]slanted[/IsItalic] plain";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "slanted").Style.FontItalic.ShouldBeTrue();
        runs.First(r => r.Text.StartsWith("plain")).Style.FontItalic.ShouldBeFalse();
    }

    [Fact]
    public void GetStyledRuns_PlainText_ProducesSingleUnchangedRun()
    {
        Text text = MakeText();
        text.RawText = "just plain text";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Count.ShouldBe(1);
        runs[0].Text.ShouldBe("just plain text");
    }

    [Fact]
    public void GetTextBlock_BbCodeColor_ProducesMultipleFontRuns()
    {
        Text text = MakeText();
        text.RawText = "plain [Color=Red]red[/Color] plain";

        TextBlock block = text.GetTextBlock();

        block.FontRuns.Select(r => r.Style.TextColor).Distinct().Count().ShouldBeGreaterThan(1);
        block.FontRuns.Any(r => r.Style.TextColor == new SKColor(255, 0, 0, 255)).ShouldBeTrue();
    }

    [Fact]
    public void StoredMarkupText_IsNull_WhenPlainText()
    {
        Text text = MakeText();
        text.RawText = "just plain text";

        text.StoredMarkupText.ShouldBeNull();
    }

    [Fact]
    public void StoredMarkupText_ReturnsMarkup_WhenTagsPresent()
    {
        Text text = MakeText();
        text.RawText = "plain [Color=Red]red[/Color] plain";

        text.StoredMarkupText.ShouldBe("plain [Color=Red]red[/Color] plain");
    }
}
