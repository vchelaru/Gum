using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using System.Linq;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that SkiaGum's <see cref="Text"/> honors <see cref="Text.MaxLettersToShow"/> for
/// typewriter-style reveal (issue #3678) without disturbing the full-text layout the caret /
/// measurement APIs depend on. The visible reveal is exposed through
/// <see cref="Text.GetVisibleWrappedText"/>, which truncates each wrapped line to the remaining
/// letter budget (matching the per-line reveal of the MonoGame/Raylib/Sokol backends).
/// </summary>
public class TextMaxLettersToShowTests
{
    /// <summary>
    /// Builds a Text whose content wraps to several lines at the given width, so a small
    /// MaxLettersToShow forces the reveal to stop partway through a wrapped line.
    /// </summary>
    private static Text MakeWrappingText()
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = 80;
        text.RawText = "Hello world this is a long line of text that wraps across many lines";
        return text;
    }

    [Fact]
    public void GetVisibleWrappedText_Null_ReturnsFullWrappedText()
    {
        Text text = MakeWrappingText();
        text.MaxLettersToShow = null;

        text.GetVisibleWrappedText().ShouldBe(text.WrappedText);
    }

    [Fact]
    public void GetVisibleWrappedText_PartialCount_RevealsExactlyThatManyLetters()
    {
        Text text = MakeWrappingText();
        text.MaxLettersToShow = 8;

        string revealed = string.Concat(text.GetVisibleWrappedText());

        revealed.Length.ShouldBe(8);
    }

    [Fact]
    public void GetVisibleWrappedText_PartialCount_TruncatesFirstLine_WhenSingleLine()
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = 1000;
        text.RawText = "Hello World";
        text.MaxLettersToShow = 5;

        text.GetVisibleWrappedText().ShouldBe(new[] { "Hello" });
    }

    [Fact]
    public void GetVisibleWrappedText_RevealSpansWrappedLines_MatchesPerLineTruncation()
    {
        Text text = MakeWrappingText();
        System.Collections.Generic.List<string> full = text.WrappedText;
        int firstLineLength = full[0].Length;
        text.MaxLettersToShow = firstLineLength + 2;

        System.Collections.Generic.List<string> revealed = text.GetVisibleWrappedText();

        revealed.Count.ShouldBe(2);
        revealed[0].ShouldBe(full[0]);
        revealed[1].ShouldBe(full[1].Substring(0, 2));
    }

    [Fact]
    public void GetVisibleWrappedText_Zero_RevealsNothing()
    {
        Text text = MakeWrappingText();
        text.MaxLettersToShow = 0;

        text.GetVisibleWrappedText().ShouldBeEmpty();
    }

    [Fact]
    public void MaxLettersToShow_DoesNotAffectMeasuredWidth()
    {
        Text text = MakeWrappingText();
        float widthWithoutLimit = text.WrappedTextWidth;

        text.MaxLettersToShow = 3;

        text.WrappedTextWidth.ShouldBe(widthWithoutLimit);
    }

    [Fact]
    public void MaxLettersToShow_DoesNotAffectWrappedText()
    {
        Text text = MakeWrappingText();
        System.Collections.Generic.List<string> fullWrapped = text.WrappedText.ToList();

        text.MaxLettersToShow = 3;

        text.WrappedText.ShouldBe(fullWrapped);
    }
}
