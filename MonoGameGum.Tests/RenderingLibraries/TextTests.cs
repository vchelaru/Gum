using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.RenderingLibraries;

public class TextTests
{
    const string basicBMFontFileData =
@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""Font18Arial_0.png""
chars count=5
char id=32   x=206   y=102   width=3     height=1     xoffset=-1    yoffset=20    xadvance=5     page=0  chnl=15
char id=33   x=247   y=74    width=4     height=13    xoffset=1     yoffset=4     xadvance=6     page=0  chnl=15
char id=34   x=113   y=103   width=6     height=5     xoffset=0     yoffset=4     xadvance=6     page=0  chnl=15
char id=35   x=200   y=48    width=11    height=13    xoffset=-1    yoffset=4     xadvance=10    page=0  chnl=15
char id=36   x=165   y=18    width=10    height=16    xoffset=0     yoffset=3     xadvance=10    page=0  chnl=15
char id=37   x=161   y=0     width=22    height=20    xoffset=1     yoffset=6     xadvance=24    page=0  chnl=15
";

    private static BitmapFont CreateBitmapFontWithDivergentLastChar()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);
        font.SetFontPattern(256, 256);

        // Force the last character's XAdvance to differ from its pixel extents
        // so Full and TrimRight styles produce different results.
        BitmapCharacterInfo character = font.Characters['!'];
        character.XAdvance = 20;
        character.PixelLeft = 0;
        character.PixelRight = 5;
        character.XOffsetInPixels = 1;

        // BitmapFont.MeasureString only honors TrimRight when Texture != null
        // (see the guard in BitmapFont.MeasureString). We can't construct a
        // real Texture2D without a GraphicsDevice in a unit test, so we plant
        // an uninitialized placeholder directly into the internal texture
        // array via reflection. MeasureString only reads the reference, never
        // dereferences it, so this is safe.
        Texture2D placeholderTexture = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        FieldInfo mTexturesField = typeof(BitmapFont).GetField("mTextures", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Texture2D[] textures = (Texture2D[])mTexturesField.GetValue(font)!;
        textures[0] = placeholderTexture;

        return font;
    }

    // Pins the per-line vertical growth the inline-text draw relies on: a styled line's rendered height
    // must grow to the tallest run's scale, so a following wrapped line is advanced BELOW an enlarged run
    // instead of drawn on top of it. This is the DRAW-side computation (ComputeStyledLineHeight, consumed
    // by DrawWithInlineVariables to advance topOfLine) — distinct from the measurement path
    // (WrappedTextHeight), which the raylib #3532 bug proved can be correct while the draw advance is
    // wrong. A [FontScale=2] run must double the line height; the ratio keeps the assert independent of
    // GlobalFontScale.
    [Fact]
    public void ComputeStyledLineHeight_ShouldGrowToTallestRunScale()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, AbcFontData(xadvance: 10, lineHeight: 20));
        font.SetFontPattern(256, 256);

        Text baseLine = new Text();
        baseLine.BitmapFont = font;
        baseLine.InlineVariables.Add(new InlineVariable
        {
            VariableName = "FontScale",
            Value = 1f,
            StartIndex = 0,
            CharacterCount = 2
        });
        baseLine.RawText = "AA";
        List<StyledSubstring> baseSubstrings = baseLine.GetStyledSubstrings(0, baseLine.WrappedText[0]);
        float baseHeight = baseLine.ComputeStyledLineHeight(baseLine.BitmapFont, baseSubstrings, out float _);
        baseHeight.ShouldBeGreaterThan(0);

        Text scaledLine = new Text();
        scaledLine.BitmapFont = font;
        scaledLine.InlineVariables.Add(new InlineVariable
        {
            VariableName = "FontScale",
            Value = 2f,
            StartIndex = 0,
            CharacterCount = 2
        });
        scaledLine.RawText = "AA";
        List<StyledSubstring> scaledSubstrings = scaledLine.GetStyledSubstrings(0, scaledLine.WrappedText[0]);
        float scaledHeight = scaledLine.ComputeStyledLineHeight(scaledLine.BitmapFont, scaledSubstrings, out float _);

        scaledHeight.ShouldBe(baseHeight * 2,
            "because the tallest run's [FontScale=2] doubles the line's rendered height, which is what advances the next line past it");
    }

    // Regression guard for the #3372 fix: when the height is a real, independent constraint,
    // TextOverflowVerticalMode.TruncateLine MUST still clip to the number of fully-fitting
    // lines. The fix only removes the content-derived (RelativeToChildren) height->lines
    // feedback; it must not disable height-based truncation for fixed-height text (the
    // DialogBox pagination path relies on this). Tested at the renderable level because this
    // is the pure UpdateLines contract — the lineHeight is 21 (from basicBMFontFileData), so
    // a height of 52 leaves room for two full lines and a partial third.
    [Fact]
    public void UpdateLines_TruncateLine_WithFixedHeight_ClipsToFittingLines()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);
        font.SetFontPattern(256, 256);

        Text text = new Text();
        text.BitmapFont = font;
        text.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        text.Width = 1000; // wide enough that the explicit lines never wrap
        text.Height = 52;  // two full 21px lines plus a partial third

        text.RawText = "Line1\nLine2\nLine3";

        text.WrappedText.Count.ShouldBe(2,
            "Because TruncateLine with a fixed height must clip to the number of fully-fitting lines");
    }

    // The #3372 fix at the renderable level: when the layout marks the Height as content-derived
    // (IsHeightDependentOnLines = true, set by GraphicalUiElement for RelativeToChildren height),
    // TruncateLine must NOT cap the lines by Height — every line renders even when Height is smaller
    // than the content. Same arrangement as the clip guard above, only the flag differs.
    [Fact]
    public void UpdateLines_TruncateLine_WithHeightDependentOnLines_DoesNotClip()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, basicBMFontFileData);
        font.SetFontPattern(256, 256);

        Text text = new Text();
        text.BitmapFont = font;
        text.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        text.IsHeightDependentOnLines = true;
        text.Width = 1000;
        text.Height = 52; // smaller than the three 21px lines, but content-derived so it must not clip

        text.RawText = "Line1\nLine2\nLine3";

        text.WrappedText.Count.ShouldBe(3,
            "Because a content-derived Height must not be used to truncate the very lines it was derived from");
    }

    // Two-char ("ab") BMFont with every glyph at a caller-chosen xadvance, so a test can build a
    // "base" font and a deliberately-wider "swapped" font and assert measurement honors each run's font.
    private static string TwoCharFontData(int xadvance, int lineHeight) =>
$@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={lineHeight} base={lineHeight} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=2
char id=97 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=98 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
";

    // #3520: a [FontSize=N] run becomes a "BitmapFont" inline variable pointing at a font generated at
    // that size (CustomSetPropertyOnRenderable). The reported width must measure that run with ITS font,
    // not the base font — otherwise a RelativeToChildren Text is sized too narrow and the larger run
    // overflows / mis-wraps. Mirrors how DrawWithInlineVariables draws the run with the swapped font.
    [Fact]
    public void WrappedTextWidth_ShouldMeasureFontSwapRun_WithItsOwnFont()
    {
        const int baseAdvance = 10;
        const int swappedAdvance = 20;

        BitmapFont baseFont = new BitmapFont((Texture2D)null!, TwoCharFontData(baseAdvance, lineHeight: 20));
        baseFont.SetFontPattern(256, 256);
        BitmapFont swappedFont = new BitmapFont((Texture2D)null!, TwoCharFontData(swappedAdvance, lineHeight: 20));
        swappedFont.SetFontPattern(256, 256);

        Text plain = new Text();
        plain.BitmapFont = baseFont;
        plain.RawText = "ab";
        float plainWidth = plain.WrappedTextWidth; // baseAdvance + baseAdvance

        Text swapped = new Text();
        swapped.BitmapFont = baseFont;
        // 'b' (index 1) is rendered with the larger font, like [FontSize] swaps a run's font.
        swapped.InlineVariables.Add(new InlineVariable
        {
            VariableName = "BitmapFont",
            Value = swappedFont,
            StartIndex = 1,
            CharacterCount = 1
        });
        swapped.RawText = "ab";
        float swappedWidth = swapped.WrappedTextWidth; // want: baseAdvance + swappedAdvance

        // Ratio, not absolute, so the assert is independent of GlobalFontScale (both widths scale by it).
        double expectedRatio = (baseAdvance + swappedAdvance) / (2.0 * baseAdvance);
        ((double)swappedWidth).ShouldBe(plainWidth * expectedRatio, plainWidth * 0.02,
            "because the font-swap run must be measured with its own (larger) font, not the base font");
    }

    // BMFont with space + A/B/C, every glyph at a caller-chosen xadvance.
    private static string AbcFontData(int xadvance, int lineHeight) =>
$@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={lineHeight} base={lineHeight} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=4
char id=32 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=65 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=66 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
char id=67 x=0 y=0 width={xadvance} height=13 xoffset=0 yoffset=4 xadvance={xadvance} page=0 chnl=15
";

    // #1934 allocation pass: the RelativeToChildren natural-size measurement re-wraps a Text every
    // layout frame by toggling its Width (unconstrained to read natural size, then back). For a Text
    // that fits on a single line, the word-by-word wrapping algorithm reproduces the raw text verbatim,
    // so re-wrapping must short-circuit to zero managed allocation rather than churning a List/Split/
    // per-word string concatenation on every frame. This is the dominant full-relayout allocation source.
    [Fact]
    public void UpdateWrappedText_WhenTextFitsOnOneLine_DoesNotAllocate()
    {
        const int advance = 10;
        BitmapFont font = new BitmapFont((Texture2D)null!, AbcFontData(advance, lineHeight: 20));
        font.SetFontPattern(256, 256);

        Text text = new Text();
        text.BitmapFont = font;
        text.Width = 1000;            // "AB AB" is 5 chars * 10 = 50 wide, far under 1000 -> one line
        text.RawText = "AB AB";
        text.WrappedText.Count.ShouldBe(1); // guard: the scenario really is single-line

        AllocationResult result = AllocationMeasurer.Measure(
            () => text.UpdateWrappedText(),
            warmupIterations: 50,
            measuredIterations: 500);

        result.TotalBytes.ShouldBe(0);
    }

    // A genuine wrap should allocate only its output line strings; the tokenizer scratch and per-word
    // measurement concats are pooled/eliminated.
    [Fact]
    public void UpdateWrappedText_WhenTextWrapsToMultipleLines_AllocatesOnlyOutputLines()
    {
        const int advance = 10;
        BitmapFont font = new BitmapFont((Texture2D)null!, AbcFontData(advance, lineHeight: 20));
        font.SetFontPattern(256, 256);

        Text text = new Text();
        text.BitmapFont = font;
        // Width 25 fits one two-char word (20) but not two words with a space ("AA BB" = 50), so the
        // three words wrap to three lines: "AA ", "BB ", "CC".
        text.Width = 25;
        text.RawText = "AA BB CC";
        text.WrappedText.Count.ShouldBe(3); // liveness guard: the scenario really does wrap word-by-word

        AllocationResult result = AllocationMeasurer.Measure(
            () => text.UpdateWrappedText(),
            warmupIterations: 50,
            measuredIterations: 500);

        // Only the output line strings plus the tokenizer's word substrings remain; headroom for runner variance.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(200);
    }

    // Re-measuring an unchanged wrapped Text must be allocation-free: WrappedText is a List<string>, and
    // routing to the List overload of GetRequiredWidthAndHeight avoids boxing its enumerator.
    [Fact]
    public void UpdatePreRenderDimensions_DoesNotAllocate()
    {
        const int advance = 10;
        BitmapFont font = new BitmapFont((Texture2D)null!, AbcFontData(advance, lineHeight: 20));
        font.SetFontPattern(256, 256);

        Text text = new Text();
        text.BitmapFont = font;
        text.Width = 25;
        text.RawText = "AA BB CC"; // wraps to three lines, so measurement iterates a multi-entry List
        text.WrappedText.Count.ShouldBe(3);

        AllocationResult result = AllocationMeasurer.Measure(
            () => text.UpdatePreRenderDimensions(),
            warmupIterations: 50,
            measuredIterations: 500);

        result.TotalBytes.ShouldBe(0);
    }

    // #3520: a base run that FOLLOWS a font-swap run (like " text." after [FontSize=40]big[/FontSize])
    // must be measured at the base size. Reproduces the residual under-measurement the manual test
    // showed: the mid-line swap grows the box, but the trailing run was still being dropped/mismeasured.
    [Fact]
    public void WrappedTextWidth_ShouldMeasureTrailingBaseRun_AfterFontSwapRun()
    {
        const int baseAdvance = 10;
        const int swappedAdvance = 20;

        BitmapFont baseFont = new BitmapFont((Texture2D)null!, AbcFontData(baseAdvance, lineHeight: 20));
        baseFont.SetFontPattern(256, 256);
        BitmapFont swappedFont = new BitmapFont((Texture2D)null!, AbcFontData(swappedAdvance, lineHeight: 20));
        swappedFont.SetFontPattern(256, 256);

        Text plain = new Text();
        plain.BitmapFont = baseFont;
        plain.RawText = "AA BB CC";
        float plainWidth = plain.WrappedTextWidth; // 8 chars * baseAdvance

        Text swapped = new Text();
        swapped.BitmapFont = baseFont;
        // Swap the MIDDLE run "BB" (indices 3-4); "AA " before and " CC" after stay base.
        swapped.InlineVariables.Add(new InlineVariable
        {
            VariableName = "BitmapFont",
            Value = swappedFont,
            StartIndex = 3,
            CharacterCount = 2
        });
        swapped.RawText = "AA BB CC";
        float swappedWidth = swapped.WrappedTextWidth; // want: 6 base chars + 2 swapped chars

        double expectedRatio = (6 * baseAdvance + 2 * swappedAdvance) / (8.0 * baseAdvance);
        ((double)swappedWidth).ShouldBe(plainWidth * expectedRatio, plainWidth * 0.02,
            "because a base run after a font-swap run must still be fully measured at the base size");
    }

    // #3520 (the wrapping half): line wrapping must measure each inline run at its own size, not the
    // base size. A fixed-width line that fits "AA BB CC" at the base size must wrap once "BB" is
    // enlarged via a font-swap run (like [FontSize=40]) — otherwise the wrap packs all three words at
    // the base size and the enlarged run spills past the width. This is the renderable-level (UpdateLines)
    // contract; InlineVariables are populated before RawText here, so it isolates the wrap seam from the
    // re-wrap sequencing exercised end-to-end in TextRuntimeTests.
    [Fact]
    public void UpdateLines_WithFontSwapRun_WrapsWhereTheEnlargedRunNoLongerFits()
    {
        const int baseAdvance = 10;
        const int swappedAdvance = 20;

        BitmapFont baseFont = new BitmapFont((Texture2D)null!, AbcFontData(baseAdvance, lineHeight: 20));
        baseFont.SetFontPattern(256, 256);
        BitmapFont swappedFont = new BitmapFont((Texture2D)null!, AbcFontData(swappedAdvance, lineHeight: 20));
        swappedFont.SetFontPattern(256, 256);

        Text text = new Text();
        text.BitmapFont = baseFont;
        // "BB" (indices 3-4) is drawn with the larger font, like [FontSize] swaps a run's font.
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "BitmapFont",
            Value = swappedFont,
            StartIndex = 3,
            CharacterCount = 2
        });

        // A wrap width (base units) that fits all three words at the base size (8 chars * 10 = 80) but not
        // once "BB" is measured at the swapped size (6*10 + 2*20 = 100). 85 sits between the two. Width is
        // in the Text's own space (UpdateLines divides by FontScale), so scale the base-unit target back up
        // to stay correct regardless of GlobalFontScale.
        float fontScale = ((IText)text).FontScale;
        text.Width = 85 * fontScale;

        text.RawText = "AA BB CC";

        text.WrappedText.Count.ShouldBe(2,
            "because the enlarged font-swap run must be measured at its own size when wrapping, forcing an earlier break");
    }

    // Font where the space advances 10px but is only 1px wide, and letters A/B/C are 10px wide with a
    // 10px advance. The narrow space is what exposes per-run TrimRight trimming: an interior run ending
    // in a space loses 9px if measured with TrimRight instead of Full.
    private static string NarrowSpaceFontData(int lineHeight) =>
$@"info face=""Arial"" size=-18 bold=0 italic=0 charset="""" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0
common lineHeight={lineHeight} base={lineHeight} scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4
page id=0 file=""x.png""
chars count=4
char id=32 x=0 y=0 width=1 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=65 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=66 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
char id=67 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15
";

    private static BitmapFont CreateNarrowSpaceFontWithTexture()
    {
        BitmapFont font = new BitmapFont((Texture2D)null!, NarrowSpaceFontData(lineHeight: 20));
        font.SetFontPattern(256, 256);

        // BitmapFont.MeasureString only honors TrimRight when Texture != null (see the guard in
        // MeasureString). Real fonts always have a texture, so plant a placeholder to match the runtime;
        // MeasureString only reads the reference, never dereferences it, so this is safe.
        Texture2D placeholderTexture = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        FieldInfo mTexturesField = typeof(BitmapFont).GetField("mTextures", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Texture2D[] textures = (Texture2D[])mTexturesField.GetValue(font)!;
        textures[0] = placeholderTexture;
        return font;
    }

    // #3520 (the real-font manifestation the uniform-font tests missed): splitting a line into runs at
    // inline-variable boundaries must NOT change its measured width when the runs carry no size change.
    // BitmapFont.MeasureString defaults to TrimRight, which trims the last glyph of each measured string
    // to its pixel extent. Measuring per-run trims at EVERY run boundary instead of once per line, so an
    // interior run ending in a space (e.g. "This is " before a styled word) loses that space's advance
    // and the line is under-measured — leaving a RelativeToChildren container too narrow and the tail
    // spilling past it. A uniform font (width == xadvance) hides this, which is why it went unnoticed.
    [Fact]
    public void WrappedTextWidth_WithInteriorRunBeforeSpace_ShouldMatchPlainLineWidth()
    {
        BitmapFont font = CreateNarrowSpaceFontWithTexture();

        Text plain = new Text();
        plain.BitmapFont = font;
        plain.RawText = "AA BB CC";
        float plainWidth = plain.WrappedTextWidth;

        Text styled = new Text();
        styled.BitmapFont = font;
        // A Color run over "BB" (indices 3-4) splits the line into "AA ", "BB", " CC" without changing
        // any glyph size, so the measured width MUST equal the plain (single-run) line's width.
        styled.InlineVariables.Add(new InlineVariable
        {
            VariableName = "Color",
            Value = System.Drawing.Color.Red,
            StartIndex = 3,
            CharacterCount = 2
        });
        styled.RawText = "AA BB CC";
        float styledWidth = styled.WrappedTextWidth;

        styledWidth.ShouldBe(plainWidth,
            "because inline runs that carry no size change must not alter the measured line width");
    }

    // Full must use XAdvance for the last glyph even when it is a trailing space (its documented
    // contract). TrimRight instead trims a trailing space to its pixel extent, so end-of-line spaces add
    // no width. This divergence is what lets a line be measured run-by-run for inline styling without
    // losing interior spaces at the run boundaries (#3520).
    [Fact]
    public void MeasureString_Full_IncludesTrailingSpaceAdvance_UnlikeTrimRight()
    {
        BitmapFont font = CreateNarrowSpaceFontWithTexture(); // space: 1px wide, 10px advance

        int full = font.MeasureString("AA ", HorizontalMeasurementStyle.Full);
        int trimRight = font.MeasureString("AA ", HorizontalMeasurementStyle.TrimRight);

        full.ShouldBe(30, "because Full counts the trailing space's full 10px XAdvance (10 + 10 + 10)");
        trimRight.ShouldBe(21, "because TrimRight trims the trailing space to its 1px pixel width (10 + 10 + 1)");
    }

    [Fact]
    public void MeasureString_WithStyleAndNoBitmapFont_DoesNotThrow()
    {
        Text text = new Text();
        // No BitmapFont set on the instance. We also expect no DefaultBitmapFont in a
        // plain unit-test run; the overload must not throw in that case.
        BitmapFont? savedDefault = Text.DefaultBitmapFont;
        Text.DefaultBitmapFont = null!;
        try
        {
            Should.NotThrow(() => text.MeasureString("hi", HorizontalMeasurementStyle.Full));
        }
        finally
        {
            Text.DefaultBitmapFont = savedDefault!;
        }
    }

    [Fact]
    public void MeasureString_WithStyleFull_MatchesBitmapFontFull()
    {
        BitmapFont font = CreateBitmapFontWithDivergentLastChar();
        Text text = new Text();
        text.BitmapFont = font;

        float expected = (float)font.MeasureString("hi!", HorizontalMeasurementStyle.Full);
        float actual = text.MeasureString("hi!", HorizontalMeasurementStyle.Full);

        actual.ShouldBe(expected);
    }

    [Fact]
    public void MeasureString_WithStyleTrimRight_MatchesBitmapFontTrimRight()
    {
        BitmapFont font = CreateBitmapFontWithDivergentLastChar();
        Text text = new Text();
        text.BitmapFont = font;

        float expectedFull = (float)font.MeasureString("hi!", HorizontalMeasurementStyle.Full);
        float expectedTrim = (float)font.MeasureString("hi!", HorizontalMeasurementStyle.TrimRight);
        float actualTrim = text.MeasureString("hi!", HorizontalMeasurementStyle.TrimRight);

        actualTrim.ShouldBe(expectedTrim);
        // Sanity: the two styles should diverge for this font, otherwise the test
        // would pass even if style were ignored.
        expectedTrim.ShouldNotBe(expectedFull);
    }
}
