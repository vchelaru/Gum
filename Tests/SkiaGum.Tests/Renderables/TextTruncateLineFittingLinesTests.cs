using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// <see cref="TextOverflowVerticalMode.TruncateLine"/> keeps every line that fits the Height before the first line
/// that does not, whatever size run makes a later line taller, as MonoGame/raylib do (issue #8341).
/// </summary>
public class TextTruncateLineFittingLinesTests
{
    private static Text MakeText(string rawText, float width, float baseLinesTall, bool ellipsis)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = width;
        text.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        text.IsTruncatingWithEllipsisOnLastLine = ellipsis;
        text.RawText = rawText;
        text.Height = text.LineHeightInPixels * baseLinesTall;
        return text;
    }

    [Theory]
    // A later line taller than what is left of the box: the lines before it stay.
    [InlineData("a\n[FontScale=2]b[/FontScale]", 1000f, 1f, false, 1)]
    [InlineData("a\n[FontScale=2]b[/FontScale]", 1000f, 1f, true, 1)]
    [InlineData("a\n[FontSize=40]b[/FontSize]", 1000f, 1f, false, 1)]
    [InlineData("a\nb\n[FontScale=3]c[/FontScale]", 1000f, 2f, false, 2)]
    [InlineData("a\nb\n[FontScale=3]c[/FontScale]", 1000f, 2f, true, 2)]
    // Wrapped (no hard newline): the box is narrow enough that each word takes its own line.
    [InlineData("a [FontScale=2]b[/FontScale]", 5f, 1f, false, 1)]
    // Already agreeing with MonoGame/raylib: pinned so the fix leaves them alone.
    [InlineData("a\n[FontScale=2]b[/FontScale]", 1000f, 2f, false, 1)]
    [InlineData("a\nb\nc", 1000f, 2f, false, 2)]
    [InlineData("a\n[FontScale=0.5]b[/FontScale]", 1000f, 1f, false, 1)]
    [InlineData("a\n[FontScale=2]b[/FontScale]", 1000f, 0.5f, false, 0)]
    public void GetTextBlock_TruncateLine_KeepsLinesThatFitBeforeTheFirstThatDoesNot(
        string rawText, float width, float baseLinesTall, bool ellipsis, int expectedLines)
    {
        Text text = MakeText(rawText, width, baseLinesTall, ellipsis);

        TextBlock block = text.GetTextBlock();

        block.Lines.Count.ShouldBe(expectedLines);
        if (expectedLines > 0)
        {
            block.Lines[0].Start.ShouldBe(0);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetTextBlock_TruncateLine_EllipsisFollowsSetting_WhenALaterTallerLineIsDropped(bool ellipsis)
    {
        Text text = MakeText("a\n[FontScale=2]b[/FontScale]", 1000f, 1f, ellipsis);

        TextBlock block = text.GetTextBlock();

        block.Truncated.ShouldBeTrue();
        block.Lines.SelectMany(line => line.Runs).Any(run => run.RunKind == FontRunKind.Ellipsis).ShouldBe(ellipsis);
    }
}
