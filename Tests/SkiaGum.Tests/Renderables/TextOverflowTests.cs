using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using System.Linq;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that SkiaGum's <see cref="Text"/> honors the existing ellipsis
/// (<see cref="Text.IsTruncatingWithEllipsisOnLastLine"/>) and vertical overflow
/// (<see cref="Text.TextOverflowVerticalMode"/>) properties through RichTextKit's
/// <c>TextBlock</c> truncation (<c>MaxHeight</c> / <c>EllipsisEnabled</c>), issue #3677.
/// </summary>
public class TextOverflowTests
{
    /// <summary>
    /// Builds a Text whose content wraps to several lines at the given width, so a small
    /// Height forces vertical truncation. Callers set Height / overflow mode / ellipsis and
    /// assert against the resulting <c>TextBlock</c>.
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
    public void GetTextBlock_EmitsEllipsisRun_WhenTruncatingWithEllipsisAndVerticalTruncation()
    {
        Text text = MakeWrappingText();
        text.Height = 30;
        text.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        text.IsTruncatingWithEllipsisOnLastLine = true;

        TextBlock block = text.GetTextBlock();

        block.Truncated.ShouldBeTrue();
        block.Lines.SelectMany(line => line.Runs).Any(run => run.RunKind == FontRunKind.Ellipsis).ShouldBeTrue();
    }

    [Fact]
    public void GetTextBlock_OmitsEllipsisRun_WhenNotTruncatingWithEllipsis()
    {
        Text text = MakeWrappingText();
        text.Height = 30;
        text.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        text.IsTruncatingWithEllipsisOnLastLine = false;

        TextBlock block = text.GetTextBlock();

        block.Truncated.ShouldBeTrue();
        block.Lines.SelectMany(line => line.Runs).Any(run => run.RunKind == FontRunKind.Ellipsis).ShouldBeFalse();
    }

    [Fact]
    public void GetTextBlock_SpillOver_RendersAllLinesUnbounded()
    {
        Text text = MakeWrappingText();
        text.Height = 30;
        text.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;

        TextBlock block = text.GetTextBlock();

        block.Truncated.ShouldBeFalse();
        block.MaxHeight.ShouldBeNull();
    }

    [Fact]
    public void GetTextBlock_TruncateLine_ClipsToHeight()
    {
        Text spillOver = MakeWrappingText();
        spillOver.Height = 30;
        spillOver.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
        int unboundedLineCount = spillOver.GetTextBlock().Lines.Count;

        Text truncated = MakeWrappingText();
        truncated.Height = 30;
        truncated.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;

        TextBlock block = truncated.GetTextBlock();

        block.Lines.Count.ShouldBeLessThan(unboundedLineCount);
        block.MaxHeight.ShouldBe(30f);
    }
}
