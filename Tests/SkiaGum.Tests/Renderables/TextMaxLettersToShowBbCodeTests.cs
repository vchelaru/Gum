using Shouldly;
using SkiaGum;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that SkiaGum's <see cref="Text"/> keeps per-run BBCode styling (issue #3679) during the
/// <see cref="Text.MaxLettersToShow"/> typewriter reveal (issue #3701), instead of falling back to the
/// tag-stripped plain text the reveal path used before.
/// </summary>
public class TextMaxLettersToShowBbCodeTests
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
    public void GetVisibleStyledRuns_PartialReveal_PreservesRunStyling()
    {
        Text text = MakeText();
        text.RawText = "plain [Color=Red]red[/Color] plain";
        text.MaxLettersToShow = 8;

        List<Text.StyledTextRun> runs = text.GetVisibleStyledRuns();

        string.Concat(runs.Select(r => r.Text)).ShouldBe("plain re");
        runs.Last().Style.TextColor.ShouldBe(new SKColor(255, 0, 0, 255));
        runs.First().Style.TextColor.ShouldBe(SKColors.Black);
    }

    [Fact]
    public void GetVisibleStyledRuns_RevealSpansWrappedLines_ConcatenatesToVisibleWrappedTextAndKeepsStyle()
    {
        Text text = MakeText();
        text.Width = 80;
        text.RawText = "[Color=Red]Hello world this is a long line of text that wraps across many lines[/Color]";
        List<string> fullWrapped = text.WrappedText;
        text.MaxLettersToShow = fullWrapped[0].Length + 2;

        List<Text.StyledTextRun> runs = text.GetVisibleStyledRuns();

        string.Concat(runs.Select(r => r.Text)).Replace("\n", "").ShouldBe(string.Concat(text.GetVisibleWrappedText()));
        runs.ShouldContain(r => r.Text == "\n");
        runs.Where(r => r.Text != "\n").ShouldAllBe(r => r.Style.TextColor == new SKColor(255, 0, 0, 255));
    }

    [Fact]
    public void GetPaintTextBlock_PartialReveal_ProducesMultipleFontRunColors()
    {
        Text text = MakeText();
        text.RawText = "plain [Color=Red]red[/Color] plain";
        text.MaxLettersToShow = 8;

        TextBlock fullBlock = text.GetTextBlock();
        TextBlock paintBlock = text.GetPaintTextBlock(fullBlock);

        paintBlock.FontRuns.Select(r => r.Style.TextColor).Distinct().Count().ShouldBeGreaterThan(1);
        paintBlock.FontRuns.Any(r => r.Style.TextColor == new SKColor(255, 0, 0, 255)).ShouldBeTrue();
    }
}
