using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Verifies that the Raylib <see cref="Text"/> renderable stores BBCode / markup inline
/// styling (Color / FontScale runs) when text is assigned through the property pipeline,
/// mirroring the MonoGame runtime's behavior. Issue #3471.
/// </summary>
public class TextMarkupTests
{
    // Issue #3532: the font-aware wrap seam MeasureString(string, int) must fall back to the base
    // MeasureString when the line carries no inline runs, so plain-text wrapping stays byte-identical.
    // Pins the fallback directly (independent of any font metrics) so the override can't regress plain
    // wrapping.
    [Fact]
    public void MeasureString_WithStartIndex_ShouldMatchBaseMeasure_WhenNoInlineVariables()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "Hello World";

        Text internalText = (Text)textRuntime.RenderableComponent;
        internalText.InlineVariables.ShouldBeEmpty();

        float baseMeasure = internalText.MeasureString("Hello World");
        float indexedMeasure = internalText.MeasureString("Hello World", 0);

        indexedMeasure.ShouldBe(baseMeasure);
    }

    // #3624: nested [IsBold][FontSize] WITHOUT a font creator. [IsBold] needs a rasterized bold atlas, which
    // only a wired creator can produce, so with no creator the bold wrapper stays unapplied (produces no run)
    // and only the inner [FontSize] survives - now under the shared "BitmapFont" font-run marker (the same
    // name the MonoGame stack model emits), with the raw size as a float because no crisp font could be built.
    // The crisp-nesting behavior is covered by Text_WithBoldWrappingFontSize_AppliesNestedResolvedRuns_WhenFontCreatorWired.
    [Fact]
    public void Text_WithBoldWrappingFontSize_StripsBothTagsButOnlyFontSizeBecomesInlineVariable()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "[IsBold=true][FontSize=20]inner[/FontSize][/IsBold]";

        Text internalText = (Text)textRuntime.RenderableComponent;

        internalText.RawText.ShouldBe("inner");
        internalText.InlineVariables.Count.ShouldBe(1);
        internalText.InlineVariables[0].VariableName.ShouldBe("BitmapFont");
        internalText.InlineVariables[0].Value.ShouldBe(20f);
        internalText.InlineVariables[0].StartIndex.ShouldBe(0);
        internalText.InlineVariables[0].CharacterCount.ShouldBe(5);
    }

    // #3624 (the feature): with a font creator wired, nested [IsBold][FontSize=20] resolves the inner run to a
    // crisp font rasterized at 20 within the bold context - the whole reason the stack model exists. The run
    // covering all of "inner" is a resolved-font ("BitmapFont") run whose font is BaseSize 20 (the inner size
    // applied on top of the outer bold). Restores the creator in finally so the no-creator tests stay unaffected.
    [Fact]
    public void Text_WithBoldWrappingFontSize_AppliesNestedResolvedRuns_WhenFontCreatorWired()
    {
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 21;
            textRuntime.Text = "[IsBold=true][FontSize=20]inner[/FontSize][/IsBold]";

            Text internalText = (Text)textRuntime.RenderableComponent;

            internalText.RawText.ShouldBe("inner");
            InlineVariable innerRun = internalText.InlineVariables.Single(v => v.CharacterCount == 5);
            innerRun.VariableName.ShouldBe("BitmapFont");
            Raylib_cs.Font innerFont = innerRun.Value.ShouldBeOfType<Raylib_cs.Font>();
            innerFont.BaseSize.ShouldBe(20);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_WithColorMarkup_PopulatesInlineVariablesAndStripsTags()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "[Color=Green]Hello[/Color] World";

        Text internalText = (Text)textRuntime.RenderableComponent;

        internalText.RawText.ShouldBe("Hello World");
        internalText.StoredMarkupText.ShouldBe("[Color=Green]Hello[/Color] World");
        internalText.InlineVariables.Count.ShouldBe(1);
        internalText.InlineVariables[0].VariableName.ShouldBe("Color");
        internalText.InlineVariables[0].StartIndex.ShouldBe(0);
        internalText.InlineVariables[0].CharacterCount.ShouldBe(5);
    }

    [Fact]
    public void Text_WithFontScaleMarkup_PopulatesInlineVariable()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "Big [FontScale=2]text[/FontScale]";

        Text internalText = (Text)textRuntime.RenderableComponent;

        internalText.RawText.ShouldBe("Big text");
        internalText.InlineVariables.Count.ShouldBe(1);
        internalText.InlineVariables[0].VariableName.ShouldBe("FontScale");
        internalText.InlineVariables[0].Value.ShouldBe(2f);
        internalText.InlineVariables[0].StartIndex.ShouldBe(4);
        internalText.InlineVariables[0].CharacterCount.ShouldBe(4);
    }

    // No font creator is wired in the test harness, so [FontSize=N] can't re-rasterize a crisp font and
    // falls back to storing the raw size as a float (Text then scales the base atlas to it). The run is
    // emitted under the shared "BitmapFont" font-run marker (the same name the MonoGame stack model uses;
    // a misnomer on Raylib, where the value is a Raylib_cs.Font or a float, not a BitmapFont). The crisp
    // swap path is covered by Text_WithFontSizeMarkup_SwapsToCrispFont_WhenFontCreatorWired.
    [Fact]
    public void Text_WithFontSizeMarkup_PopulatesInlineVariable()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "Big [FontSize=40]text[/FontSize]";

        Text internalText = (Text)textRuntime.RenderableComponent;

        internalText.RawText.ShouldBe("Big text");
        internalText.InlineVariables.Count.ShouldBe(1);
        internalText.InlineVariables[0].VariableName.ShouldBe("BitmapFont");
        internalText.InlineVariables[0].Value.ShouldBe(40f);
        internalText.InlineVariables[0].StartIndex.ShouldBe(4);
        internalText.InlineVariables[0].CharacterCount.ShouldBe(4);
    }

    // With a font creator wired, [FontSize=N] re-rasterizes a crisp Raylib_cs.Font AT size N (BaseSize == N)
    // emitted under the shared "BitmapFont" font-run marker - the actual font-swap, not an atlas scale-up.
    // Mirrors the MonoGame BitmapFont swap: the stack model also emits a second (empty) run when the tag
    // closes and the font pops back to the base size (21 here). Restores the creator in finally so the
    // no-creator fallback tests stay unaffected.
    [Fact]
    public void Text_WithFontSizeMarkup_SwapsToCrispFont_WhenFontCreatorWired()
    {
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 21;
            textRuntime.Text = "Big [FontSize=40]text[/FontSize]";

            Text internalText = (Text)textRuntime.RenderableComponent;

            internalText.InlineVariables.Count.ShouldBe(2);
            internalText.InlineVariables[0].VariableName.ShouldBe("BitmapFont");
            internalText.InlineVariables[0].StartIndex.ShouldBe(4);
            internalText.InlineVariables[0].CharacterCount.ShouldBe(4);
            Raylib_cs.Font swapFont = internalText.InlineVariables[0].Value.ShouldBeOfType<Raylib_cs.Font>();
            swapFont.BaseSize.ShouldBe(40);

            // The close tag pops the font size back to the base (21); that pop-back run covers no
            // characters (it sits at the end of "Big text") but mirrors the MonoGame stack output.
            internalText.InlineVariables[1].VariableName.ShouldBe("BitmapFont");
            internalText.InlineVariables[1].CharacterCount.ShouldBe(0);
            Raylib_cs.Font baseRun = internalText.InlineVariables[1].Value.ShouldBeOfType<Raylib_cs.Font>();
            baseRun.BaseSize.ShouldBe(21);
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // #3624 (no-creator characterization): [IsBold], [IsItalic], [OutlineThickness], and [Font=Name] are
    // recognized tags, so their markup IS stripped from the visible RawText, but applying them requires a
    // rasterized atlas that only a wired font creator can produce - so with no creator they remain unapplied
    // (produce no run), exactly as before. This preserves the pre-#3624 no-creator behavior; the applied
    // behavior is covered by Text_WithStyleMarkup_AppliesResolvedFontRuns_WhenFontCreatorWired.
    [Theory]
    [InlineData("[IsBold=true]Hello[/IsBold] World")]
    [InlineData("[IsItalic=true]Hello[/IsItalic] World")]
    [InlineData("[OutlineThickness=2]Hello[/OutlineThickness] World")]
    [InlineData("[Font=SomeName]Hello[/Font] World")]
    public void Text_WithUnappliedStyleMarkup_StripsTagsButAddsNoInlineVariable_WhenNoFontCreator(string markup)
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = markup;

        Text internalText = (Text)textRuntime.RenderableComponent;

        internalText.RawText.ShouldBe("Hello World");
        internalText.InlineVariables.ShouldBeEmpty();
    }

    // #3624 (the feature): with a font creator wired, [IsBold], [IsItalic], [OutlineThickness], and
    // [Font=Name] are now APPLIED - each produces a resolved-font ("BitmapFont") run over its wrapped text,
    // instead of being stripped-but-ignored. The run covering "Hello" (5 chars) is the resolved font for that
    // styled range. Restores the creator in finally so the no-creator tests stay unaffected.
    [Theory]
    [InlineData("[IsBold=true]Hello[/IsBold] World")]
    [InlineData("[IsItalic=true]Hello[/IsItalic] World")]
    [InlineData("[OutlineThickness=2]Hello[/OutlineThickness] World")]
    [InlineData("[Font=Arial]Hello[/Font] World")]
    public void Text_WithStyleMarkup_AppliesResolvedFontRuns_WhenFontCreatorWired(string markup)
    {
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            TextRuntime textRuntime = new();
            textRuntime.Font = "Arial";
            textRuntime.FontSize = 20;
            textRuntime.Text = markup;

            Text internalText = (Text)textRuntime.RenderableComponent;

            internalText.RawText.ShouldBe("Hello World");
            InlineVariable helloRun = internalText.InlineVariables.Single(v => v.CharacterCount == 5);
            helloRun.VariableName.ShouldBe("BitmapFont");
            helloRun.Value.ShouldBeOfType<Raylib_cs.Font>();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // #3624 regression: a Text owned by a GraphicalUiElement that is NOT a TextRuntime has unseeded font
    // stacks (SetBbCodeText seeds them only for a TextRuntime). With a font creator wired and font-family
    // markup, the resolution loop would otherwise peek/pop those unseeded stacks - a wrong font, or an
    // uncaught InvalidOperationException. The single guard must skip per-run font resolution in that case
    // while still stripping the tags and wrapping the text. Restores the creator in finally.
    [Fact]
    public void SetBbCodeText_WithNonTextRuntimeOwner_DoesNotThrowOrResolveFonts_WhenFontCreatorWired()
    {
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            // A real Text renderable, but driven through a base GraphicalUiElement (not a TextRuntime).
            TextRuntime source = new();
            Text text = (Text)source.RenderableComponent;
            GraphicalUiElement nonTextRuntimeOwner = new();

            Should.NotThrow(() => CustomSetPropertyOnRenderable.SetPropertyOnRenderable(
                text, nonTextRuntimeOwner, "Text", "[IsBold=true]Hello[/IsBold] World"));

            text.RawText.ShouldBe("Hello World");
            // No TextRuntime to seed the stacks, so no resolved-font ("BitmapFont") runs are produced.
            text.InlineVariables.Any(v => v.VariableName == "BitmapFont").ShouldBeFalse();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void Text_ChangingFromMarkupToPlain_ClearsInlineVariablesAndStoredMarkup()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "[Color=Green]Hello[/Color] World";

        Text internalText = (Text)textRuntime.RenderableComponent;
        internalText.InlineVariables.Count.ShouldBe(1);

        textRuntime.Text = "Plain text";

        internalText.RawText.ShouldBe("Plain text");
        internalText.StoredMarkupText.ShouldBeNull();
        internalText.InlineVariables.ShouldBeEmpty();
    }

    // Issue #3481: an inline [FontScale=N] run renders larger but the Text used to report its size
    // to the layout as if every line were at the base scale, so the tall run overflowed its slot
    // and overlapped the next stacked sibling. These pin that the *reported* size now grows by the
    // per-line max inline FontScale on the Raylib runtime too (kept in parity with MonoGame).
    // RelativeToChildren width 0 keeps each line un-wrapped so line counts are deterministic.

    [Fact]
    public void WrappedTextHeight_ShouldDouble_WhenEntireLineHasFontScale2()
    {
        TextRuntime plain = new();
        plain.WidthUnits = DimensionUnitType.RelativeToChildren;
        plain.Width = 0;
        plain.Text = "BIG";
        float plainHeight = ((Text)plain.RenderableComponent).WrappedTextHeight;
        plainHeight.ShouldBeGreaterThan(0);

        TextRuntime scaled = new();
        scaled.WidthUnits = DimensionUnitType.RelativeToChildren;
        scaled.Width = 0;
        scaled.Text = "[FontScale=2]BIG[/FontScale]";
        float scaledHeight = ((Text)scaled.RenderableComponent).WrappedTextHeight;

        scaledHeight.ShouldBe(plainHeight * 2,
            "Because a whole line scaled 2x should report twice the height");
    }

    [Fact]
    public void WrappedTextHeight_ShouldDouble_WhenPartOfLineHasFontScale2()
    {
        // Default (unscaled) text and a scaled run share the same line: the line's height is
        // driven by the tallest run, so the whole line reports 2x even though only "BIG" is scaled.
        TextRuntime plain = new();
        plain.WidthUnits = DimensionUnitType.RelativeToChildren;
        plain.Width = 0;
        plain.Text = "small BIG";
        float plainHeight = ((Text)plain.RenderableComponent).WrappedTextHeight;
        plainHeight.ShouldBeGreaterThan(0);

        TextRuntime scaled = new();
        scaled.WidthUnits = DimensionUnitType.RelativeToChildren;
        scaled.Width = 0;
        scaled.Text = "small [FontScale=2]BIG[/FontScale]";
        float scaledHeight = ((Text)scaled.RenderableComponent).WrappedTextHeight;

        scaledHeight.ShouldBe(plainHeight * 2,
            "Because the tallest run on the line drives the line height, even when mixed with base-scale text");
    }

    [Fact]
    public void WrappedTextHeight_ShouldGrowOnlyScaledLine_WhenMultiLine()
    {
        // Only line 1 carries the scaled run; line 2 must stay at base height. Expected total is
        // 2x (line 1) + 1x (line 2) = 3x a single plain line — proving the growth is per-line.
        TextRuntime plain = new();
        plain.WidthUnits = DimensionUnitType.RelativeToChildren;
        plain.Width = 0;
        plain.Text = "BIG";
        float plainSingleLineHeight = ((Text)plain.RenderableComponent).WrappedTextHeight;
        plainSingleLineHeight.ShouldBeGreaterThan(0);

        TextRuntime scaled = new();
        scaled.WidthUnits = DimensionUnitType.RelativeToChildren;
        scaled.Width = 0;
        scaled.Text = "[FontScale=2]BIG[/FontScale]\nsmall";
        float scaledHeight = ((Text)scaled.RenderableComponent).WrappedTextHeight;

        scaledHeight.ShouldBe(plainSingleLineHeight * 3,
            "Because only the first line is scaled 2x; the second line stays at base height");
    }

    [Fact]
    public void WrappedTextHeight_ShouldNotGrow_WhenLineHasOnlyColorMarkup()
    {
        // A non-scale inline variable (Color) intertwined with the line must not change the
        // reported height — only FontScale runs grow it.
        TextRuntime plain = new();
        plain.WidthUnits = DimensionUnitType.RelativeToChildren;
        plain.Width = 0;
        plain.Text = "Hello World";
        float plainHeight = ((Text)plain.RenderableComponent).WrappedTextHeight;
        plainHeight.ShouldBeGreaterThan(0);

        TextRuntime colored = new();
        colored.WidthUnits = DimensionUnitType.RelativeToChildren;
        colored.Width = 0;
        colored.Text = "[Color=Red]Hello[/Color] World";
        float coloredHeight = ((Text)colored.RenderableComponent).WrappedTextHeight;

        coloredHeight.ShouldBe(plainHeight,
            "Because a Color-only run carries no scale and must not affect the reported height");
    }

    [Fact]
    public void WrappedTextWidth_ShouldGrow_WhenLineHasFontScale2()
    {
        TextRuntime plain = new();
        plain.WidthUnits = DimensionUnitType.RelativeToChildren;
        plain.Width = 0;
        plain.Text = "BIG";
        float plainWidth = ((Text)plain.RenderableComponent).WrappedTextWidth;
        plainWidth.ShouldBeGreaterThan(0);

        TextRuntime scaled = new();
        scaled.WidthUnits = DimensionUnitType.RelativeToChildren;
        scaled.Width = 0;
        scaled.Text = "[FontScale=2]BIG[/FontScale]";
        float scaledWidth = ((Text)scaled.RenderableComponent).WrappedTextWidth;

        scaledWidth.ShouldBe(plainWidth * 2, 1.5,
            "Because a scaled run is measured at its own scale, so the reported width grows with it");
    }

    // Issue #3524: a [FontSize=N] run is an ABSOLUTE per-run pixel size (unlike [FontScale], which
    // is a multiplier). Raylib draws by scaling the base font atlas to N px, so a run requested at
    // twice the base size must ALSO be measured at twice the width — otherwise a RelativeToChildren
    // Text is sized too narrow and the enlarged run spills past its background (the exact bug fixed
    // for MonoGame in #3520 / #3523). baseSize is read from the actual font so the expectation stays
    // self-contained regardless of which default font the test harness loads.
    [Fact]
    public void WrappedTextWidth_ShouldGrow_WhenLineHasFontSizeLargerThanBase()
    {
        TextRuntime plain = new();
        plain.WidthUnits = DimensionUnitType.RelativeToChildren;
        plain.Width = 0;
        plain.Text = "BIG";
        Text plainText = (Text)plain.RenderableComponent;
        float plainWidth = plainText.WrappedTextWidth;
        plainWidth.ShouldBeGreaterThan(0);

        int baseSize = plainText.Font.BaseSize;
        int targetFontSize = baseSize * 2;

        TextRuntime sized = new();
        sized.WidthUnits = DimensionUnitType.RelativeToChildren;
        sized.Width = 0;
        sized.Text = $"[FontSize={targetFontSize}]BIG[/FontSize]";
        float sizedWidth = ((Text)sized.RenderableComponent).WrappedTextWidth;

        sizedWidth.ShouldBe(plainWidth * 2, 1.5,
            "Because [FontSize=2xBase] renders the run at twice the base pixel size, so the measured width doubles");
    }

    // Issue #3524: the HEIGHT half - a [FontSize=N] run must grow the reported line HEIGHT too, or a
    // RelativeToChildren box is too short and the enlarged run spills above/below it (the vertical spill
    // MonoGame had). Raylib gets this right by construction (one layout scale drives both width and
    // height), but pin it so a future refactor can't silently regress it - the exact gap that let the
    // MonoGame height bug ship uncaught.
    [Fact]
    public void WrappedTextHeight_ShouldGrow_WhenLineHasFontSizeLargerThanBase()
    {
        TextRuntime plain = new();
        plain.WidthUnits = DimensionUnitType.RelativeToChildren;
        plain.Width = 0;
        plain.Text = "BIG";
        Text plainText = (Text)plain.RenderableComponent;
        float plainHeight = plainText.WrappedTextHeight;
        plainHeight.ShouldBeGreaterThan(0);

        int baseSize = plainText.Font.BaseSize;
        int targetFontSize = baseSize * 2;

        TextRuntime sized = new();
        sized.WidthUnits = DimensionUnitType.RelativeToChildren;
        sized.Width = 0;
        sized.Text = $"[FontSize={targetFontSize}]BIG[/FontSize]";
        float sizedHeight = ((Text)sized.RenderableComponent).WrappedTextHeight;

        sizedHeight.ShouldBe(plainHeight * 2, plainHeight * 0.05,
            "Because a [FontSize=2xBase] run is twice the base pixel size, so the measured line height doubles");
    }

    // Issue #3532 (vertical stacking): a wrapped line that follows an enlarged inline run must be drawn
    // below that run's full height, not one base line-height down (which overlapped it). raylib advanced
    // every line by a uniform base line height; XNA already grows the advance per line. An explicit
    // newline makes the two-line split deterministic (independent of wrap width / font metrics). The
    // plain control derives the base per-line advance; the scaled version's second line must sit twice
    // as far down because its first line carries a [FontScale=2] run.
    [Fact]
    public void GetLineTopOffsets_ShouldPushLineBelowEnlargedRun_NotOverlapIt()
    {
        TextRuntime plain = new();
        plain.Text = "BIG\nsmall";
        Text plainText = (Text)plain.RenderableComponent;
        IReadOnlyList<float> plainOffsets = plainText.GetLineTopOffsets();
        plainOffsets.Count.ShouldBe(2);
        plainOffsets[0].ShouldBe(0);
        float baseLineAdvance = plainOffsets[1];
        baseLineAdvance.ShouldBeGreaterThan(0);

        TextRuntime scaled = new();
        scaled.Text = "[FontScale=2]BIG[/FontScale]\nsmall";
        Text scaledText = (Text)scaled.RenderableComponent;
        IReadOnlyList<float> scaledOffsets = scaledText.GetLineTopOffsets();
        scaledOffsets.Count.ShouldBe(2);
        scaledOffsets[0].ShouldBe(0);
        scaledOffsets[1].ShouldBe(baseLineAdvance * 2, 0.01,
            "because the first line's [FontScale=2] run doubles its height, so the second line sits twice as far down");
    }

    // Issue #3532 (the wrapping half, end-to-end): a fixed-width Text that fits "AA BB CC" at the base
    // size must re-wrap once the inline [FontScale=2] run enlarges "BB" past the wrap width. The BBCode
    // path assigns RawText (which wraps) BEFORE the InlineVariables populate, so the first wrap is blind
    // to the runs; the fix re-wraps font-aware afterward. raylib measures with a real font atlas, so the
    // wrap width can't be hardcoded — it's derived to sit strictly between the plain and enlarged
    // single-line widths (fits the plain line, forces the enlarged line to break). Without the fix the
    // enlarged text stays one line and spills past the width.
    [Fact]
    public void WrappedText_ShouldRewrapFontAware_WhenBbCodeFontScaleRunWidensLine()
    {
        // Plain single-line width (huge Absolute width -> no wrap).
        TextRuntime plainUnwrapped = new();
        plainUnwrapped.WidthUnits = DimensionUnitType.Absolute;
        plainUnwrapped.Width = 100000;
        plainUnwrapped.Text = "AA BB CC";
        Text plainUnwrappedText = (Text)plainUnwrapped.RenderableComponent;
        plainUnwrappedText.WrappedText.Count.ShouldBe(1);
        float plainWidth = plainUnwrappedText.WrappedTextWidth;

        // Enlarged single-line width: the [FontScale=2] run widens the line (#3524 size reporting).
        TextRuntime scaledUnwrapped = new();
        scaledUnwrapped.WidthUnits = DimensionUnitType.Absolute;
        scaledUnwrapped.Width = 100000;
        scaledUnwrapped.Text = "AA [FontScale=2]BB[/FontScale] CC";
        Text scaledUnwrappedText = (Text)scaledUnwrapped.RenderableComponent;
        scaledUnwrappedText.WrappedText.Count.ShouldBe(1);
        float scaledWidth = scaledUnwrappedText.WrappedTextWidth;

        scaledWidth.ShouldBeGreaterThan(plainWidth);

        // A width that fits the plain line but not the enlarged line.
        float wrapWidth = (plainWidth + scaledWidth) / 2f;

        // Control: plain text at this width still fits on one line, proving the width is not itself the
        // cause of the extra break.
        TextRuntime plainWrapped = new();
        plainWrapped.WidthUnits = DimensionUnitType.Absolute;
        plainWrapped.Width = wrapWidth;
        plainWrapped.Text = "AA BB CC";
        ((Text)plainWrapped.RenderableComponent).WrappedText.Count.ShouldBe(1);

        // Subject: width set BEFORE text so RawText wraps blind, then the inline run populates. The
        // font-aware re-wrap must break the enlarged line.
        TextRuntime scaled = new();
        scaled.WidthUnits = DimensionUnitType.Absolute;
        scaled.Width = wrapWidth;
        scaled.Text = "AA [FontScale=2]BB[/FontScale] CC";
        ((Text)scaled.RenderableComponent).WrappedText.Count.ShouldBeGreaterThan(1,
            "because after the inline [FontScale] run populates, the text must re-wrap measuring the enlarged run at its own size");
    }

    // Issue #3532: the FontSize-swap half of the wrap fix. With a font creator wired, [FontSize=N] swaps
    // in a crisp font rasterized at N px whose glyphs are wider than the base, so a fixed-width Text must
    // re-wrap around it. Mirrors WrappedText_ShouldRewrapFontAware_WhenBbCodeFontScaleRunWidensLine but
    // exercises the absolute-swap-font run path (ResolveRunFont's swap-font branch) instead of the
    // [FontScale] multiplier path.
    [Fact]
    public void WrappedText_ShouldRewrapFontAware_WhenBbCodeFontSizeRunWidensLine()
    {
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

            // Plain single-line width (huge Absolute width -> no wrap).
            TextRuntime plainUnwrapped = new();
            plainUnwrapped.Font = "Arial";
            plainUnwrapped.FontSize = 20;
            plainUnwrapped.WidthUnits = DimensionUnitType.Absolute;
            plainUnwrapped.Width = 100000;
            plainUnwrapped.Text = "AA BB CC";
            Text plainUnwrappedText = (Text)plainUnwrapped.RenderableComponent;
            plainUnwrappedText.WrappedText.Count.ShouldBe(1);
            float plainWidth = plainUnwrappedText.WrappedTextWidth;
            int targetFontSize = plainUnwrappedText.Font.BaseSize * 2;

            // Enlarged single-line width via a size-2xBase crisp swap font (#3524 size reporting).
            TextRuntime swappedUnwrapped = new();
            swappedUnwrapped.Font = "Arial";
            swappedUnwrapped.FontSize = 20;
            swappedUnwrapped.WidthUnits = DimensionUnitType.Absolute;
            swappedUnwrapped.Width = 100000;
            swappedUnwrapped.Text = $"AA [FontSize={targetFontSize}]BB[/FontSize] CC";
            Text swappedUnwrappedText = (Text)swappedUnwrapped.RenderableComponent;
            swappedUnwrappedText.WrappedText.Count.ShouldBe(1);
            float swappedWidth = swappedUnwrappedText.WrappedTextWidth;

            swappedWidth.ShouldBeGreaterThan(plainWidth);

            // A width that fits the plain line but not the enlarged line.
            float wrapWidth = (plainWidth + swappedWidth) / 2f;

            // Control: plain text at this width still fits on one line.
            TextRuntime plainWrapped = new();
            plainWrapped.Font = "Arial";
            plainWrapped.FontSize = 20;
            plainWrapped.WidthUnits = DimensionUnitType.Absolute;
            plainWrapped.Width = wrapWidth;
            plainWrapped.Text = "AA BB CC";
            ((Text)plainWrapped.RenderableComponent).WrappedText.Count.ShouldBe(1);

            // Subject: width set BEFORE text so RawText wraps blind, then the swap run populates. The
            // font-aware re-wrap must break the enlarged line.
            TextRuntime swapped = new();
            swapped.Font = "Arial";
            swapped.FontSize = 20;
            swapped.WidthUnits = DimensionUnitType.Absolute;
            swapped.Width = wrapWidth;
            swapped.Text = $"AA [FontSize={targetFontSize}]BB[/FontSize] CC";
            ((Text)swapped.RenderableComponent).WrappedText.Count.ShouldBeGreaterThan(1,
                "because after the inline [FontSize] swap run populates, the text must re-wrap measuring the enlarged run at its own size");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }
}
