using RenderingLibrary.Graphics;
using Shouldly;

namespace SkiaGum.Tests.Renderables;

/// <summary>WrappedText follows every input that changes the wrap, and reading it allocates nothing beyond the layout (#8218).</summary>
public class TextWrappedTextTests : BaseTestClass
{
    private const string WrappingText = "Alpha beta gamma delta epsilon zeta";

    private static Text CreateText(string rawText, float? width = 120)
    {
        Text text = new Text();
        text.RawText = rawText;
        text.FontSize = 18;
        text.Width = width;
        return text;
    }

    private static long MeasureAllocatedBytes(Action action)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Clone_TextChangedOnClone_LeavesSourceWrappedTextUnchanged()
    {
        Text source = CreateText(WrappingText);
        List<string> expected = new List<string>(source.WrappedText);

        Text clone = source.Clone();
        clone.RawText = "Other";
        clone.WrappedText.ShouldBe(CreateText("Other").WrappedText);

        source.WrappedText.ShouldBe(expected);
    }

    [Fact]
    public void WrappedText_FontSizeChanged_MatchesFresh()
    {
        Text text = CreateText(WrappingText);
        _ = text.WrappedText;

        text.FontSize = 30;

        Text fresh = CreateText(WrappingText);
        fresh.FontSize = 30;
        text.WrappedText.ShouldBe(fresh.WrappedText);
    }

    [Fact]
    public void WrappedText_ReadTwiceWithNoChange_AllocatesNothing()
    {
        Text text = CreateText(WrappingText);
        _ = text.WrappedText;

        long bytes = MeasureAllocatedBytes(() =>
        {
            _ = text.WrappedText;
            _ = text.WrappedText;
        });

        bytes.ShouldBe(0);
    }

    [Fact]
    public void WrappedText_ReadAfterSingleLineTextChange_AllocatesNothingBeyondLayout()
    {
        // Single-line texts, so each line is the whole layout string and needs no Substring.
        Text text = CreateText("Score: 1", width: null);
        for (int i = 0; i < 4; i++)
        {
            text.RawText = i % 2 == 0 ? "Score: 22" : "Score: 1";
            _ = text.WrappedText;
        }

        text.RawText = "Score: 22";
        // RichTextKit lays the block out lazily on the first Lines read; do that outside the measured read.
        _ = text.GetCachedTextBlock().Lines.Count;
        long bytes = MeasureAllocatedBytes(() => _ = text.WrappedText);

        bytes.ShouldBe(0);
        text.WrappedText.ShouldBe(new[] { "Score: 22" });
    }

    [Fact]
    public void WrappedText_WidthChanged_MatchesFresh()
    {
        Text text = CreateText(WrappingText, width: 300);
        int wideLineCount = text.WrappedText.Count;

        text.Width = 80;

        text.WrappedText.ShouldBe(CreateText(WrappingText, width: 80).WrappedText);
        text.WrappedText.Count.ShouldBeGreaterThan(wideLineCount);
    }
}
