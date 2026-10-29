using Gum;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies the <c>[Custom=Name]</c> per-letter callback (issue #3692) mirrors the MonoGame/Raylib
/// <c>Text.Customizations</c>/<c>ContextCustomizations</c> mechanism: one styled run per character,
/// each resolved against the registered callback for Color (a normal RichTextKit <see cref="Style"/>
/// override) and XOffset/YOffset (applied as an actual glyph-position nudge via RichTextKit's
/// <c>FontRun.MoveGlyphs</c>, since <see cref="Style"/> has no offset concept).
/// </summary>
public class TextCustomizationTests : BaseTestClass
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
    public void GetStyledRuns_CustomTag_ProducesOneRunPerCharacter()
    {
        Text.Customizations["Identity"] = (index, block) => new LetterCustomization();
        Text text = MakeText();
        text.RawText = "[Custom=Identity]AB[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Count(r => r.Text == "A" || r.Text == "B").ShouldBe(2);
    }

    [Fact]
    public void GetStyledRuns_CustomTag_AppliesCallbackColor()
    {
        Text.Customizations["Red"] = (index, block) => new LetterCustomization
        {
            Color = Color.Red,
        };
        Text text = MakeText();
        text.RawText = "[Custom=Red]X[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "X").Style.TextColor.ShouldBe(new SKColor(255, 0, 0, 255));
    }

    [Fact]
    public void GetStyledRuns_CustomTag_AppliesCallbackYOffset()
    {
        Text.Customizations["Bump"] = (index, block) => new LetterCustomization
        {
            YOffset = 12,
        };
        Text text = MakeText();
        text.RawText = "[Custom=Bump]X[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "X").YOffset.ShouldBe(12f);
    }

    [Fact]
    public void GetStyledRuns_CustomTag_AppliesReplacementCharacter()
    {
        Text.Customizations["Star"] = (index, block) => new LetterCustomization
        {
            ReplacementCharacter = '*',
        };
        Text text = MakeText();
        text.RawText = "[Custom=Star]X[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.ShouldContain(r => r.Text == "*");
    }

    [Fact]
    public void GetTextBlock_CustomTag_MovesGlyphPositionByYOffset()
    {
        Text.Customizations["BigBump"] = (index, block) => new LetterCustomization
        {
            YOffset = 50,
        };
        Text plain = MakeText();
        plain.RawText = "X";
        float baselineY = plain.GetTextBlock().FontRuns[0].GlyphPositions[0].Y;

        Text customized = MakeText();
        customized.RawText = "[Custom=BigBump]X[/Custom]";
        float bumpedY = customized.GetTextBlock().FontRuns[0].GlyphPositions[0].Y;

        (bumpedY - baselineY).ShouldBe(50f, tolerance: 0.01);
    }

    [Fact]
    public void GetStyledRuns_CustomTag_ContextFunctionReceivesPriorColor()
    {
        Text.ContextCustomizations["Darken"] = (index, block, context) => new LetterCustomization
        {
            Color = Color.FromArgb(context.Color!.Value.A, 10, 10, 10),
        };
        Text text = MakeText();
        text.RawText = "[Custom=Darken]X[/Custom]";

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        runs.Single(r => r.Text == "X").Style.TextColor.ShouldBe(new SKColor(10, 10, 10, 255));
    }

    private static (float Left, float Top, float Right, float Bottom) RenderInkBounds(string rawText, float fontSize = 40, Gum.RenderingLibrary.Blend? blend = null)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 200));
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new() { Canvas = surface.Canvas };
        Text text = MakeText();
        text.FontSize = fontSize;
        text.X = 80;
        text.Y = 80;
        text.RawText = rawText;
        text.Blend = blend;

        text.Render(managers);

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 128)
                {
                    left = System.Math.Min(left, x);
                    top = System.Math.Min(top, y);
                    right = System.Math.Max(right, x + 1);
                    bottom = System.Math.Max(bottom, y + 1);
                }
            }
        }
        return (left, top, right, bottom);
    }

    [Fact]
    public void Render_CustomTagWithNoTransform_DrawsSamePixelsAsPlainText()
    {
        Text.Customizations["NoTransform"] = (index, block) => new LetterCustomization();

        var plain = RenderInkBounds("I");
        var customized = RenderInkBounds("[Custom=NoTransform]I[/Custom]");

        customized.ShouldBe(plain);
    }

    [Fact]
    public void Render_CustomTagScale_ScalesLetterAboutItsCenter()
    {
        Text.Customizations["Double"] = (index, block) => new LetterCustomization
        {
            ScaleX = 2,
            ScaleY = 2,
        };

        var plain = RenderInkBounds("I");
        var scaled = RenderInkBounds("[Custom=Double]I[/Custom]");

        (scaled.Right - scaled.Left).ShouldBe(2 * (plain.Right - plain.Left), tolerance: 2);
        (scaled.Bottom - scaled.Top).ShouldBe(2 * (plain.Bottom - plain.Top), tolerance: 2);
        ((scaled.Left + scaled.Right) / 2f).ShouldBe((plain.Left + plain.Right) / 2f, tolerance: 1.5f);
    }

    [Fact]
    public void Render_CustomTagScaleWithLeftTopOrigin_KeepsCellTopLeftFixed()
    {
        Text.Customizations["DoubleFromTopLeft"] = (index, block) => new LetterCustomization
        {
            ScaleX = 2,
            ScaleY = 2,
            ScaleXOrigin = HorizontalAlignment.Left,
            ScaleYOrigin = VerticalAlignment.Top,
        };

        var plain = RenderInkBounds("I");
        var scaled = RenderInkBounds("[Custom=DoubleFromTopLeft]I[/Custom]");

        // The cell's top-left is the text's origin (80, 80), so every ink edge doubles its distance from it.
        (scaled.Left - 80).ShouldBe(2 * (plain.Left - 80), tolerance: 2);
        (scaled.Top - 80).ShouldBe(2 * (plain.Top - 80), tolerance: 2);
        (scaled.Right - 80).ShouldBe(2 * (plain.Right - 80), tolerance: 2);
        (scaled.Bottom - 80).ShouldBe(2 * (plain.Bottom - 80), tolerance: 2);
    }

    public static IEnumerable<object[]> OriginsAndScales()
    {
        foreach (HorizontalAlignment xOrigin in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
        foreach (VerticalAlignment yOrigin in new[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom, VerticalAlignment.TextBaseline })
        foreach ((float scaleX, float scaleY) in new[] { (2f, 3f), (0.5f, 0.25f), (1f, 1.5f), (-1f, 2f) })
        {
            yield return new object[] { xOrigin, yOrigin, scaleX, scaleY };
        }
    }

    [Theory]
    [MemberData(nameof(OriginsAndScales))]
    public void GetDrawMatrix_ScaleAboutOrigin_KeepsTheNamedCellPointFixed(
        HorizontalAlignment xOrigin, VerticalAlignment yOrigin, float scaleX, float scaleY)
    {
        SKRect cell = new(10, 20, 18, 32);
        const float baseline = 29;
        float anchorX = xOrigin switch
        {
            HorizontalAlignment.Left => cell.Left,
            HorizontalAlignment.Right => cell.Right,
            _ => cell.MidX,
        };
        float anchorY = yOrigin switch
        {
            VerticalAlignment.Top => cell.Top,
            VerticalAlignment.Bottom => cell.Bottom,
            VerticalAlignment.TextBaseline => baseline,
            _ => cell.MidY,
        };

        SKMatrix matrix = new Text.LetterDrawTransform(0, 0, scaleX, scaleY, xOrigin, yOrigin, 0)
            .GetDrawMatrix(cell, baseline);

        SKPoint anchor = matrix.MapPoint(anchorX, anchorY);
        anchor.X.ShouldBe(anchorX, tolerance: 0.0001f);
        anchor.Y.ShouldBe(anchorY, tolerance: 0.0001f);
        SKPoint topLeft = matrix.MapPoint(cell.Left, cell.Top);
        SKPoint bottomRight = matrix.MapPoint(cell.Right, cell.Bottom);
        (bottomRight.X - topLeft.X).ShouldBe(cell.Width * scaleX, tolerance: 0.0001f);
        (bottomRight.Y - topLeft.Y).ShouldBe(cell.Height * scaleY, tolerance: 0.0001f);

        // Rotation turns the letter about the scaled cell's top-left corner, so that corner does not move.
        SKPoint rotatedTopLeft = new Text.LetterDrawTransform(0, 0, scaleX, scaleY, xOrigin, yOrigin, 30)
            .GetDrawMatrix(cell, baseline).MapPoint(cell.Left, cell.Top);
        rotatedTopLeft.X.ShouldBe(topLeft.X, tolerance: 0.0001f);
        rotatedTopLeft.Y.ShouldBe(topLeft.Y, tolerance: 0.0001f);
    }

    [Fact]
    public void Render_CustomTagRotation_RotatesLetterCounterclockwiseAboutCellTopLeft()
    {
        Text.Customizations["Quarter"] = (index, block) => new LetterCustomization
        {
            RotationDegrees = 90,
        };

        var plain = RenderInkBounds("I");
        var rotated = RenderInkBounds("[Custom=Quarter]I[/Custom]");

        // Like MonoGame, the letter turns about its cell's top-left (the text origin, 80, 80): a
        // counterclockwise quarter turn maps (dx, dy) to (dy, -dx), so the letter lies above the origin.
        rotated.Left.ShouldBe(80 + (plain.Top - 80), tolerance: 2);
        rotated.Right.ShouldBe(80 + (plain.Bottom - 80), tolerance: 2);
        rotated.Top.ShouldBe(80 - (plain.Right - 80), tolerance: 2);
        rotated.Bottom.ShouldBe(80 - (plain.Left - 80), tolerance: 2);
    }

    [Fact]
    public void GetTextBlock_CustomTagScaleAndRotation_DoesNotMoveFollowingLetters()
    {
        Text.Customizations["BigTurn"] = (index, block) => new LetterCustomization
        {
            ScaleX = 3,
            ScaleY = 3,
            RotationDegrees = 45,
        };
        Text plain = MakeText();
        plain.RawText = "AB";
        Text customized = MakeText();
        customized.RawText = "[Custom=BigTurn]A[/Custom]B";

        TextBlock plainBlock = plain.GetTextBlock();
        TextBlock customizedBlock = customized.GetTextBlock();

        customizedBlock.MeasuredWidth.ShouldBe(plainBlock.MeasuredWidth, tolerance: 0.01);
        customizedBlock.MeasuredHeight.ShouldBe(plainBlock.MeasuredHeight, tolerance: 0.01);
        customizedBlock.FontRuns.Last().GlyphPositions[0].X
            .ShouldBe(plainBlock.FontRuns[0].GlyphPositions[1].X, tolerance: 0.01);
    }

    [Fact]
    public void Render_CustomTagScaleWithBlend_IsNotClippedToTheBlock()
    {
        Text.Customizations["TripleForBlend"] = (index, block) => new LetterCustomization
        {
            ScaleX = 3,
            ScaleY = 3,
        };

        var unblended = RenderInkBounds("[Custom=TripleForBlend]I[/Custom]");
        var blended = RenderInkBounds("[Custom=TripleForBlend]I[/Custom]", blend: Gum.RenderingLibrary.Blend.Normal);

        blended.ShouldBe(unblended);
    }

    [Fact]
    public void Render_CustomTagRotationWithBlend_IsNotClippedToTheBlock()
    {
        Text.Customizations["QuarterForBlend"] = (index, block) => new LetterCustomization
        {
            RotationDegrees = 90,
        };

        var unblended = RenderInkBounds("[Custom=QuarterForBlend]I[/Custom]");
        var blended = RenderInkBounds("[Custom=QuarterForBlend]I[/Custom]", blend: Gum.RenderingLibrary.Blend.Normal);

        blended.ShouldBe(unblended);
    }

    [Theory]
    [InlineData("\U0001F600[Custom=EmojiBump]X[/Custom]", 1)]
    [InlineData("a\U0001F600b[Custom=EmojiBump]cd[/Custom]", 3)]
    public void GetTextBlock_CustomTagAfterEmoji_MovesOnlyTheTaggedGlyphs(string rawText, int firstTaggedCodePoint)
    {
        Text.Customizations["EmojiBump"] = (index, block) => new LetterCustomization
        {
            YOffset = 50,
        };
        Text plain = MakeText();
        plain.RawText = rawText.Replace("[Custom=EmojiBump]", "").Replace("[/Custom]", "");
        Text customized = MakeText();
        customized.RawText = rawText;

        List<float> plainYs = GlyphYsInOrder(plain.GetTextBlock());
        List<float> customizedYs = GlyphYsInOrder(customized.GetTextBlock());

        customizedYs.Count.ShouldBe(plainYs.Count);
        for (int i = 0; i < plainYs.Count; i++)
        {
            float expected = i >= firstTaggedCodePoint ? 50f : 0f;
            (customizedYs[i] - plainYs[i]).ShouldBe(expected, tolerance: 0.01, $"code point {i}");
        }
    }

    // Issue #7108: a [Custom] span splits per character, and a surrogate pair (an emoji) must stay one run so it
    // draws as one glyph. The callback gets the UTF-16 index of the pair's first unit and offsets Y by 10 + index,
    // so each glyph's Y shift against the same text without the tag says which callback call moved it.
    [Theory]
    [InlineData("a[Custom=SpanOffset]\U0001F600b[/Custom]", "0,11,13")]
    [InlineData("[Custom=SpanOffset]\U0001F600[/Custom]", "10")]
    [InlineData("[Custom=SpanOffset]\U0001F600a\U0001F600[/Custom]", "10,12,13")]
    [InlineData("a\U0001F600[Custom=SpanOffset]b\U0001F600[/Custom]c", "0,0,13,14,0")]
    public void GetTextBlock_CustomSpanContainingEmoji_KeepsTheEmojiOneGlyph(string rawText, string expectedYShifts)
    {
        Text.Customizations["SpanOffset"] = (index, block) => new LetterCustomization { YOffset = 10 + index };

        (List<SKPoint> plain, List<SKPoint> customized) = MeasureAgainstPlain(rawText, width: 1000);

        customized.Count.ShouldBe(plain.Count);
        for (int i = 0; i < plain.Count; i++)
        {
            (customized[i].X - plain[i].X).ShouldBe(0f, tolerance: 0.01, $"glyph {i} x");
        }
        string.Join(",", customized.Select((p, i) => (p.Y - plain[i].Y).ToString("0.#"))).ShouldBe(expectedYShifts);
    }

    [Fact]
    public void GetTextBlock_CustomSpanContainingEmojiAndWrapping_KeepsTheEmojiOneGlyph()
    {
        List<int> indexes = new();
        Text.Customizations["SpanOffset"] = (index, block) =>
        {
            indexes.Add(index);
            return new LetterCustomization { YOffset = 10 + index };
        };

        // "aaaa " is 5 units, so the tagged letters start at UTF-16 index 5; the emoji is the 5th tagged letter (index 9, 2 units).
        (List<SKPoint> plain, List<SKPoint> customized) =
            MeasureAgainstPlain("aaaa [Custom=SpanOffset]bbbb\U0001F600cccc dddd[/Custom]", width: 90);

        plain.Select(p => p.Y).Distinct().Count().ShouldBeGreaterThan(1, "the text must wrap for this test to mean anything");
        customized.Count.ShouldBe(plain.Count);
        List<int> expectedIndexes = new() { 5, 6, 7, 8, 9, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        indexes.Distinct().ToList().ShouldBe(expectedIndexes);
        for (int i = 0; i < plain.Count; i++)
        {
            (customized[i].X - plain[i].X).ShouldBe(0f, tolerance: 0.01, $"glyph {i} x");
            // Glyph i is code point i; the first 5 are untagged, the rest sit at expectedIndexes[i - 5].
            float expectedY = i < 5 ? 0f : 10 + expectedIndexes[i - 5];
            (customized[i].Y - plain[i].Y).ShouldBe(expectedY, tolerance: 0.01, $"glyph {i} y");
        }
    }

    // A replacement character replaces a whole code point, so the layout (whose lines count code points) is unchanged:
    // an emoji becomes one replacement character, not two.
    [Fact]
    public void GetStyledRuns_CustomSpanReplacingAnEmoji_ReplacesThePairWithOneCharacter()
    {
        Text.Customizations["Mask"] = (index, block) => new LetterCustomization { ReplacementCharacter = '*' };
        Text text = MakeText();
        text.RawText = "[Custom=Mask]a\U0001F600b[/Custom]";

        text.GetStyledRuns().Select(r => r.Text).ToList().ShouldBe(new List<string> { "*", "*", "*" });
        text.WrappedText.ShouldBe(new List<string> { "a\U0001F600b" });
    }

    [Theory]
    [InlineData("a\U0001F600b", false)]
    [InlineData("[Custom=Plain]a\U0001F600b[/Custom]", false)]
    [InlineData("[Custom=Mask]a\U0001F600b[/Custom]", true)]
    public void GetVisibleStyledRuns_EmojiWithMaxLettersToShow_RevealsTheSameLettersAsTheWrappedLines(string rawText, bool isMasked)
    {
        Text.Customizations["Mask"] = (index, block) => new LetterCustomization { ReplacementCharacter = '*' };
        Text.Customizations["Plain"] = (index, block) => new LetterCustomization();
        string full = "a\U0001F600b";

        for (int maxLetters = 0; maxLetters <= 4; maxLetters++)
        {
            Text text = MakeText();
            text.RawText = rawText;
            text.MaxLettersToShow = maxLetters;

            // Run on a worker so a loop that stops advancing fails the test instead of hanging the run.
            var task = System.Threading.Tasks.Task.Run(() =>
                (string.Concat(text.GetVisibleStyledRuns().Select(r => r.Text)), string.Concat(text.GetVisibleWrappedText())));
            task.Wait(10000).ShouldBeTrue($"MaxLettersToShow {maxLetters} did not finish");

            // The emoji is never cut in half, so 2 letters already reveal it whole.
            (string runs, string wrapped) = task.Result;
            wrapped.ShouldBe(full.Substring(0, new[] { 0, 1, 3, 3, 4 }[maxLetters]), $"MaxLettersToShow {maxLetters}");
            string expectedRuns = isMasked ? new string('*', new[] { 0, 1, 2, 2, 3 }[maxLetters]) : wrapped;
            runs.ShouldBe(expectedRuns, $"MaxLettersToShow {maxLetters}");
        }
    }
    // A lone surrogate has nothing to pair with, so it stays one run per UTF-16 unit as before.
    [Fact]
    public void GetStyledRuns_CustomSpanWithLoneSurrogate_StaysOneRunPerCharacter()
    {
        Text.Customizations["SpanOffset"] = (index, block) => new LetterCustomization();
        Text text = MakeText();
        text.RawText = "[Custom=SpanOffset]\uD83Db\uDE00c[/Custom]";

        text.GetStyledRuns().Select(r => r.Text).ToList().ShouldBe(new List<string> { "\uD83D", "b", "\uDE00", "c" });
    }

    // Grapheme clusters beyond a surrogate pair (flags, ZWJ sequences) are not joined: each code point is its own run.
    [Fact]
    public void GetStyledRuns_CustomSpanWithFlagAndZwjSequence_SplitsPerCodePoint()
    {
        Text.Customizations["SpanOffset"] = (index, block) => new LetterCustomization();
        Text text = MakeText();
        text.RawText = "[Custom=SpanOffset]\U0001F1FA\U0001F1F8\U0001F468‍\U0001F469[/Custom]";

        text.GetStyledRuns().Select(r => r.Text).ToList().ShouldBe(
            new List<string> { "\U0001F1FA", "\U0001F1F8", "\U0001F468", "‍", "\U0001F469" });
    }

    [Fact]
    public void GetStyledRuns_CustomSpanWithEmoji_CallsBackOncePerCodePointWithTheFirstUnitIndex()
    {
        List<int> indexes = new();
        Text.Customizations["SpanOffset"] = (index, block) =>
        {
            indexes.Add(index);
            return new LetterCustomization();
        };
        Text text = MakeText();
        text.RawText = "a[Custom=SpanOffset]\U0001F600b[/Custom]";

        text.GetStyledRuns();

        indexes.ShouldBe(new List<int> { 1, 3 });
    }

    private static (List<SKPoint> Plain, List<SKPoint> Customized) MeasureAgainstPlain(string rawText, float width)
    {
        Text plain = MakeText();
        plain.Width = width;
        plain.RawText = rawText.Replace("[Custom=SpanOffset]", "").Replace("[/Custom]", "");
        Text customized = MakeText();
        customized.Width = width;
        customized.RawText = rawText;
        return (GlyphPointsInOrder(plain.GetTextBlock()), GlyphPointsInOrder(customized.GetTextBlock()));
    }

    private static List<SKPoint> GlyphPointsInOrder(TextBlock block) =>
        block.FontRuns.OrderBy(r => r.Start).SelectMany(r => r.GlyphPositions).ToList();

    private static List<float> GlyphYsInOrder(TextBlock block) =>
        GlyphPointsInOrder(block).Select(p => p.Y).ToList();
}
