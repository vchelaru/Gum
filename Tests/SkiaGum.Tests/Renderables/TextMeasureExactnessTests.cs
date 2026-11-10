using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Tests.Forms.Linked;
using SkiaSharp;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Pins the numbers Skia's <c>Text.MeasureString</c> and <c>LineHeightInPixels</c> return (#7863) against a
/// reference built the way they were before the TextBox stopped measuring through a new <c>TextBlock</c> per
/// call: a block of its own and a new style for every string. The caret, selection and hit-testing positions
/// of a TextBox are sums of these numbers, so the caret and selection tests below follow from them.
/// </summary>
public class TextMeasureExactnessTests : SkiaFormsTestBase
{
    private static readonly string[] Strings =
    {
        "a", "hello world", "ab   ", "   lead", "a\tb", "a\U0001F600b\U0001F600c", "WWW iii AV To fi ffl", "ij́k",
        "This little shack is where the Smith stays. I should not go in if I am not invited",
    };

    private static float ReferenceWidth(Text text, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }
        TextBlock block = new TextBlock();
        block.AddText(value, ReferenceStyle(text));
        return block.MeasuredWidth;
    }

    private static int ReferenceLineHeight(Text text)
    {
        TextBlock block = new TextBlock();
        block.AddText("My", ReferenceStyle(text));
        return block.Lines.Count > 0 ? (int)MathF.Ceiling(block.Lines[0].Height) : (int)MathF.Ceiling(text.FontSize);
    }

    private static Style ReferenceStyle(Text text) => new Style()
    {
        FontFamily = text.FontName,
        FontSize = text.FontSize * (float)Text.GlobalTextScale,
        TextColor = text.Color,
        FontItalic = text.IsItalic,
        FontWeight = (int)(400 * text.BoldWeight),
    };

    private static IEnumerable<Text> BuildFonts()
    {
        foreach (string name in new[] { "Arial", "Times New Roman" })
        foreach (float size in new[] { 12f, 18f, 33.5f })
        foreach (bool italic in new[] { false, true })
        foreach (float bold in new[] { 1f, 1.5f })
        {
            Text text = new Text();
            text.FontName = name;
            text.FontSize = size;
            text.IsItalic = italic;
            text.BoldWeight = bold;
            yield return text;
        }
    }

    [Fact]
    public void MeasureString_EveryFontAndString_MatchesAFreshBlock()
    {
        foreach (Text text in BuildFonts())
        {
            foreach (string value in Strings)
            {
                text.MeasureString(value).ShouldBe(ReferenceWidth(text, value),
                    $"{text.FontName} {text.FontSize} italic {text.IsItalic} bold {text.BoldWeight}: '{value}'");
            }
            text.MeasureString("").ShouldBe(0f);
        }
    }

    [Fact]
    public void MeasureString_LongTextBetweenShortOnes_DoesNotLeakIntoTheNextMeasure()
    {
        Text text = new Text();
        string longText = string.Concat(Enumerable.Repeat("The quick brown fox jumps. ", 60));

        float shortBefore = text.MeasureString("abc");
        float longWidth = text.MeasureString(longText);
        float shortAfter = text.MeasureString("abc");

        shortBefore.ShouldBe(ReferenceWidth(text, "abc"));
        longWidth.ShouldBe(ReferenceWidth(text, longText));
        shortAfter.ShouldBe(shortBefore);
    }

    [Fact]
    public void MeasureString_FontChangedBetweenCalls_UsesTheNewFont()
    {
        Text text = new Text();
        text.FontSize = 12;
        float small = text.MeasureString("hello world");

        text.FontSize = 40;
        text.IsItalic = true;
        float large = text.MeasureString("hello world");
        text.FontSize = 12;
        text.IsItalic = false;

        large.ShouldBe(ReferenceWidth(new Text { FontSize = 40, IsItalic = true }, "hello world"));
        text.MeasureString("hello world").ShouldBe(small);
        large.ShouldBeGreaterThan(small);
    }

    [Fact]
    public void LineHeightInPixels_EveryFont_MatchesAFreshBlock()
    {
        foreach (Text text in BuildFonts())
        {
            text.LineHeightInPixels.ShouldBe(ReferenceLineHeight(text),
                $"{text.FontName} {text.FontSize} italic {text.IsItalic} bold {text.BoldWeight}");
            // Asked again, the cached answer is the same.
            text.LineHeightInPixels.ShouldBe(ReferenceLineHeight(text));
        }
    }

    [Fact]
    public void LineHeightInPixels_FontChanged_ReturnsTheNewFontsHeight()
    {
        Text text = new Text();
        text.FontSize = 12;
        int small = text.LineHeightInPixels;

        text.FontSize = 60;

        text.LineHeightInPixels.ShouldBe(ReferenceLineHeight(text));
        text.LineHeightInPixels.ShouldBeGreaterThan(small);
    }

    static TextBoxBaseVisual VisualOf(TextBox textBox) => (TextBoxBaseVisual)textBox.Visual;

    static TextBox CreateBox(string text, HorizontalAlignment alignment, bool multiLine)
    {
        TextBox textBox = new();
        textBox.Width = 180;
        if (multiLine)
        {
            textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
            textBox.AcceptsReturn = true;
            textBox.Height = 200;
        }
        VisualOf(textBox).TextInstance.HorizontalAlignment = alignment;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    // The caret moves by exactly the drawn width of the text before it, whatever the alignment, so every
    // position the box reports is the reference measurement of the same prefix.
    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void CaretX_SingleLine_MovesByTheReferenceWidthOfTheTextBeforeIt(HorizontalAlignment alignment)
    {
        foreach (string value in new[] { "hello world", "ab   ", "WWW AV To", "a\U0001F600b" })
        {
            TextBox textBox = CreateBox(value, alignment, multiLine: false);
            Text core = (Text)VisualOf(textBox).TextInstance.RenderableComponent!;
            textBox.CaretIndex = 0;
            float origin = VisualOf(textBox).CaretInstance.AbsoluteLeft;

            for (int i = 1; i <= value.Length; i++)
            {
                if (char.IsLowSurrogate(value[i - 1]))
                {
                    continue;
                }
                textBox.CaretIndex = i;
                float moved = VisualOf(textBox).CaretInstance.AbsoluteLeft - origin;
                moved.ShouldBe(ReferenceWidth(core, value.Substring(0, i)) * core.FontScale, 0.001f, $"{alignment} '{value}' at {i}");
            }
        }
    }

    // Centered and right-aligned lines start where the leftover width puts them; a left-aligned line starts at
    // the left edge, so its offset is the same as no alignment at all.
    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void CaretX_AtTheStartOfALine_IsOffsetByTheLeftoverWidthOfThatLine(HorizontalAlignment alignment)
    {
        string value = "one\nhello world\n\nab   ";
        TextBox aligned = CreateBox(value, alignment, multiLine: true);
        TextBox left = CreateBox(value, HorizontalAlignment.Left, multiLine: true);
        Text core = (Text)VisualOf(aligned).TextInstance.RenderableComponent!;
        float boxWidth = VisualOf(aligned).TextInstance.AbsoluteWidth;

        int lineStart = 0;
        foreach (string line in core.WrappedText.ToList())
        {
            aligned.CaretIndex = lineStart;
            left.CaretIndex = lineStart;
            float width = ReferenceWidth(core, line.TrimEnd('\n', '\r')) * core.FontScale;
            float expected = alignment switch
            {
                HorizontalAlignment.Center => (boxWidth - width) / 2f,
                HorizontalAlignment.Right => boxWidth - width,
                _ => 0f,
            };

            float shift = VisualOf(aligned).CaretInstance.AbsoluteLeft - VisualOf(left).CaretInstance.AbsoluteLeft;
            shift.ShouldBe(expected, 0.01f, $"{alignment} line '{line}'");
            lineStart += line.Length;
        }
    }

    // A selection's highlight spans the reference widths of its two ends.
    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void SelectionWidth_SingleLine_IsTheReferenceWidthOfTheSelectedText(HorizontalAlignment alignment)
    {
        string value = "hello world WWW";
        TextBox textBox = CreateBox(value, alignment, multiLine: false);
        Text core = (Text)VisualOf(textBox).TextInstance.RenderableComponent!;

        foreach ((int start, int length) in new[] { (0, 5), (3, 4), (6, 9), (0, 15) })
        {
            textBox.SelectionStart = start;
            textBox.SelectionLength = length;
            float expected = (ReferenceWidth(core, value.Substring(0, start + length))
                - ReferenceWidth(core, value.Substring(0, start))) * core.FontScale;
            VisualOf(textBox).ClipContainer.Children
                .OfType<Gum.Wireframe.GraphicalUiElement>()
                .Single(c => c.Name == "SelectionInstance" && c.Visible)
                .AbsoluteWidth.ShouldBe(expected, 0.01f, $"{alignment} {start}+{length}");
        }
    }
}
