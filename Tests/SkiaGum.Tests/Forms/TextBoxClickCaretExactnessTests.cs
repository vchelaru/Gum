using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using MonoGameGum.Tests;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Tests.Forms.Linked;
using SkiaSharp;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// Pins the caret index a click on a Skia <c>TextBox</c> lands on (#7872). The hit test walks a line one character
/// at a time, adding each character's advance until the sum passes the click; the reference here is the same walk
/// with every advance measured the way it was before any caching: a fresh <c>TextBlock</c> holding that one
/// character. So kerning, ligatures and shaping across neighbors play no part in the answer, and an answer that
/// came from a cache, or from a measure of anything longer than the one character, would differ somewhere in the
/// sweep. The reference reuses the shared line-walking code and replaces only the source of the advances.
/// </summary>
public class TextBoxClickCaretExactnessTests : SkiaFormsTestBase
{
    private static readonly string[] Texts =
    {
        "Hello world, the quick brown fox",
        "AVA To WAVE Yo fi ffl fl office",
        "tab\there\tand more",
        "emoji \U0001F600 and \U0001F468‍\U0001F469 mixed \U0001F600\U0001F600 end",
        "trailing spaces   \n   leading\n\nafter an empty line",
        "ij́k combining and שלום hebrew",
        "A long paragraph that wraps over several lines in a narrow box because it keeps on going and going.",
    };

    private static readonly Dictionary<(string?, float, int, bool, char), float> ReferenceAdvances = new();

    private static float ReferenceAdvance(Text text, char character)
    {
        (string?, float, int, bool, char) key = (text.FontName, text.FontSize * (float)Text.GlobalTextScale, (int)(400 * text.BoldWeight), text.IsItalic, character);
        if (!ReferenceAdvances.TryGetValue(key, out float advance))
        {
            TextBlock block = new TextBlock();
            block.AddText(character.ToString(), new Style()
            {
                FontFamily = text.FontName,
                FontSize = key.Item2,
                TextColor = text.Color,
                FontItalic = text.IsItalic,
                FontWeight = key.Item3,
            });
            advance = block.MeasuredWidth;
            ReferenceAdvances[key] = advance;
        }
        return advance;
    }

    private static TextBoxBaseVisual VisualOf(TextBox textBox) => (TextBoxBaseVisual)textBox.Visual;

    private static TextBox CreateBox(string text, HorizontalAlignment alignment, float fontSize, float fontScale, bool multiLine)
    {
        TextBox textBox = new();
        textBox.Width = 180;
        textBox.Height = 300;
        if (multiLine)
        {
            textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
            textBox.AcceptsReturn = true;
        }
        Text core = (Text)VisualOf(textBox).TextInstance.RenderableComponent!;
        core.FontSize = fontSize;
        core.FontScale = fontScale;
        VisualOf(textBox).TextInstance.HorizontalAlignment = alignment;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    private static float VerticalOffset(Text core) => TextHitTesting.GetVerticalAlignmentOffset(
        core.VerticalAlignment, core.GetAbsoluteBottom() - core.GetAbsoluteTop(), core.WrappedTextHeight);

    // What the hit test must answer, from the reference advances.
    private static int ExpectedIndex(TextBox textBox, float screenX, float screenY)
    {
        Text core = (Text)VisualOf(textBox).TextInstance.RenderableComponent!;
        var textInstance = VisualOf(textBox).TextInstance;
        float scale = ((IFormsText)core).WordWrapFontScale;
        float cursorOffset = screenX - textInstance.AbsoluteLeft;

        if (core.WrappedText.Count <= 1)
        {
            string line = textBox.Text ?? string.Empty;
            return TextHitTesting.GetIndexInLine(line, cursorOffset - textBox.GetLineXOffsetForHorizontalAlignment(line),
                character => ReferenceAdvance(core, character) * scale);
        }

        return TextBoxBase.WrappedToTextIndex(textBox.Text, TextHitTesting.GetIndexInWrappedLines(
            core.WrappedText,
            cursorOffset,
            screenY - (textInstance.AbsoluteTop + VerticalOffset(core)),
            ((IFormsText)core).GetDrawnLineTop,
            textBox.GetLineXOffsetForHorizontalAlignment,
            (offset, line) => TextHitTesting.GetIndexInLine(line, offset, character => ReferenceAdvance(core, character) * scale)));
    }

    // Every pixel from just left of the box to just right of the widest line, on every line, plus a click above
    // the first line and below the last.
    private static void AssertEveryClickMatchesTheReference(TextBox textBox, string description, ref int checkedClicks)
    {
        Text core = (Text)VisualOf(textBox).TextInstance.RenderableComponent!;
        float left = VisualOf(textBox).TextInstance.AbsoluteLeft;
        float top = VisualOf(textBox).TextInstance.AbsoluteTop;
        float boxWidth = VisualOf(textBox).TextInstance.AbsoluteWidth;
        int lineCount = Math.Max(1, core.WrappedText.Count);

        for (int line = -1; line <= lineCount; line++)
        {
            float y = line < 0 ? top - 5 : top + VerticalOffset(core) + ((IFormsText)core).GetDrawnLineTop(Math.Min(line, lineCount)) + 1;
            for (int x = -5; x <= (int)boxWidth + 5; x++)
            {
                if (!SweepScale.Includes(x * 31 + line))
                {
                    continue;
                }
                int actual = textBox.GetCaretIndexAtPosition(left + x, y);
                int expected = ExpectedIndex(textBox, left + x, y);
                actual.ShouldBe(expected, $"{description} line {line} x {x}");
                checkedClicks++;
            }
        }
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void ClickInWrappedBox_EveryTextAndFontScale_MatchesTheOneCharacterAdvanceWalk(HorizontalAlignment alignment)
    {
        int checkedClicks = 0;
        foreach (string text in Texts)
        {
            foreach ((float size, float scale) in new[] { (18f, 1f), (12f, 1.5f), (33f, 0.75f) })
            {
                TextBox textBox = CreateBox(text, alignment, size, scale, multiLine: true);
                AssertEveryClickMatchesTheReference(textBox, $"{alignment} size {size} scale {scale} '{text}'", ref checkedClicks);
            }
        }

        checkedClicks.ShouldBeGreaterThan(300);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void ClickInSingleLineBox_EveryText_MatchesTheOneCharacterAdvanceWalk(HorizontalAlignment alignment)
    {
        int checkedClicks = 0;
        foreach (string text in new[] { Texts[0], Texts[1], Texts[2], Texts[3], "trailing spaces   ", "" })
        {
            TextBox textBox = CreateBox(text, alignment, 18f, 1f, multiLine: false);
            AssertEveryClickMatchesTheReference(textBox, $"{alignment} single line '{text}'", ref checkedClicks);
        }

        checkedClicks.ShouldBeGreaterThan(100);
    }

    [Fact]
    public void ClickInWrappedBox_MoreThanOnePlace_ReachesDifferentCarets()
    {
        TextBox textBox = CreateBox(Texts[0], HorizontalAlignment.Left, 18f, 1f, multiLine: true);
        float left = VisualOf(textBox).TextInstance.AbsoluteLeft;
        float top = VisualOf(textBox).TextInstance.AbsoluteTop;

        HashSet<int> carets = new HashSet<int>();
        for (int x = 0; x < 180; x += 4)
        {
            carets.Add(textBox.GetCaretIndexAtPosition(left + x, top + 2));
        }

        carets.Count.ShouldBeGreaterThan(8);
    }
}
