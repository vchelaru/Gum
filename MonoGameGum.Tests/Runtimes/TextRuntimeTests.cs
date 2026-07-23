using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using Gum.Localization;
using Moq;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class TextRuntimeTests : BaseTestClass
{

    const string fontPattern =
$"info face=\"Arial\" size=-18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
$"common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\r\n" +
$"chars count=223\r\n";
    Mock<ILocalizationService> _localizationService;


    public TextRuntimeTests()
    {
        _localizationService = new();

    }

    #region AbsoluteWidth

    [Fact]
    public void AbsoluteWidth_ShouldBeChangedByText_IfRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        sut.Width = 0;
        sut.Text = "Short";
        float shortWidth = sut.GetAbsoluteWidth();

        sut.Text = "This is much longer";
        float longWidth = sut.GetAbsoluteWidth();

        longWidth.ShouldBeGreaterThan(shortWidth);
    }

    [Fact]
    public void AbsoluteWidth_ShouldNotIncludeNewlines()
    {
        TextRuntime textRuntime = new();

        var character = textRuntime.Typeface.Characters['\n'];
        character.XAdvance = 10;

        textRuntime.Text = "Hello";

        var widthBefore = textRuntime.GetAbsoluteWidth();

        textRuntime.Text = "Hello\na";

        var widthAfter = textRuntime.GetAbsoluteWidth();

        widthBefore.ShouldBe(widthAfter, "Because a trailing newline should not affect the width of a text, regardless of its XAdavance");
    }

    #endregion

    #region Clone
    [Fact]
    public void Clone_ShouldCreateClonedText()
    {
        Text sut = new();
        var clone = sut.Clone();
        clone.ShouldNotBeNull();
    }

    #endregion

    #region Color

    [Fact]
    public void Color_ShouldRoundTrip_ThroughContainedRenderable()
    {
        // Pins the System.Drawing <-> XNA conversion in the Color property (ToUserColor/
        // ToContainerColor on XNALIKE). Distinct channels catch any R/B swap or dropped alpha.
        TextRuntime sut = new();

        sut.Color = new Microsoft.Xna.Framework.Color(10, 20, 30, 40);

        sut.Color.R.ShouldBe((byte)10);
        sut.Color.G.ShouldBe((byte)20);
        sut.Color.B.ShouldBe((byte)30);
        sut.Color.A.ShouldBe((byte)40);
    }

    #endregion

    #region FontScale Inline Measurement (issue #3481)

    // Issue #3481: an inline [FontScale=N] run renders larger but the Text used to report its
    // size to the layout as if every line were at the base scale, so the tall run overflowed its
    // slot and overlapped the next stacked sibling. These pin that the *reported* size now grows
    // by the per-line max inline FontScale. RelativeToChildren width 0 keeps each line un-wrapped
    // so line counts are deterministic.

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

    [Fact]
    public void WrappedTextHeight_ShouldScaleBy1Point5_WhenFontScaleHasDecimalPoint_RegardlessOfCurrentCulture()
    {
        // FontScale parsing used to rely on the current thread culture. Under a culture where '.' is the
        // thousands separator (e.g. de-DE), float.TryParse("1.5") does not fail - it silently parses as
        // 15 (the '.' is consumed as a group separator), applying a 10x-too-large scale. BBCode markup is
        // always written with '.' as the decimal point, so parsing must be culture-invariant.
        var originalCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
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
            scaled.Text = "[FontScale=1.5]BIG[/FontScale]";
            float scaledHeight = ((Text)scaled.RenderableComponent).WrappedTextHeight;

            scaledHeight.ShouldBe(plainHeight * 1.5f, 1.5,
                "Because FontScale must parse '.' as a decimal point (scale of 1.5x) regardless of the thread's current culture, not as a thousands separator (scale of 15x)");
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region BbCode FontSize Inline Measurement (issue #3520)

    // BMFont with space + A/B/C, every glyph at a caller-chosen xadvance. Used by the stub font
    // creator so a generated [FontSize=N] font has predictable, uniform metrics.
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

    // Stubs in-memory font generation so a [FontSize=N] run produces a font whose glyph advance is
    // N/2 — i.e. the generated size-40 font is exactly 2x the base size-20 font. This is what lets the
    // integration test exercise the real markup path (parse -> GetAndCreateFontIfNecessary ->
    // InMemoryFontCreator -> "BitmapFont" inline var -> measure) with no .fnt files on disk.
    private class SizeProportionalFontCreator : IInMemoryFontCreator
    {
        public BitmapFont? TryCreateFont(BmfcSave bmfcSave)
        {
            int advance = bmfcSave.FontSize / 2;
            BitmapFont font = new BitmapFont((Texture2D)null!, AbcFontData(advance, lineHeight: bmfcSave.FontSize));
            font.SetFontPattern(256, 256);
            return font;
        }
    }

    // End-to-end repro for #3520: a [FontSize=40] run under WidthUnits=RelativeToChildren must be
    // measured with the generated size-40 font, not the base font. The isolated Text-level tests pin the
    // measurement math; this one drives the FULL runtime path a game uses — real BBCode parsing, real
    // font generation (via the stub) — so it proves the fix holds where the manual test showed it failing.
    // Assumption-free: it reads the actual base and swapped glyph advances off the live objects, so it
    // pins the exact width delta from swapping "BB" to the larger font regardless of font resolution.
    [Fact]
    public void WrappedTextWidth_ShouldMeasureBbCodeFontSizeRun_WithGeneratedFont()
    {
        var previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new SizeProportionalFontCreator();
        try
        {
            TextRuntime plain = new();
            plain.WidthUnits = DimensionUnitType.RelativeToChildren;
            plain.Width = 0;
            plain.Font = "StubFont3520";
            plain.FontSize = 20;
            plain.Text = "AA BB CC";
            Text plainText = (Text)plain.RenderableComponent;
            float plainWidth = plainText.WrappedTextWidth;
            float baseBAdvance = plainText.BitmapFont.Characters['B'].XAdvance;

            TextRuntime swapped = new();
            swapped.WidthUnits = DimensionUnitType.RelativeToChildren;
            swapped.Width = 0;
            swapped.Font = "StubFont3520";
            swapped.FontSize = 20;
            // Same visible text "AA BB CC"; only "BB" is enlarged via a generated size-40 font.
            swapped.Text = "AA [FontSize=40]BB[/FontSize] CC";
            Text swappedText = (Text)swapped.RenderableComponent;
            float swappedWidth = swappedText.WrappedTextWidth;

            InlineVariable fontSwapVariable =
                swappedText.InlineVariables.First(v => v.VariableName == "BitmapFont");
            float swappedBAdvance = ((BitmapFont)fontSwapVariable.Value).Characters['B'].XAdvance;
            swappedBAdvance.ShouldBeGreaterThan(baseBAdvance,
                "Because the [FontSize=40] run must generate a larger font than the size-20 base");

            // Swapping the two 'B' glyphs from the base font to the larger font widens the line by
            // exactly 2 * (swapped - base). Everything else ("AA ", " CC") is identical base text.
            double expectedWidth = plainWidth + 2 * (swappedBAdvance - baseBAdvance);
            ((double)swappedWidth).ShouldBe(expectedWidth, expectedWidth * 0.02,
                "because the [FontSize=40] run must be measured end-to-end with the generated size-40 font");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
        }
    }

    // #3524 (the height half of #3520): a [FontSize=N] font-swap run must also drive the reported line
    // HEIGHT, not just the width. #3523 fixed width (measure each run with its swapped font) but left
    // height on the FontScale-only path, so the tallest run's taller generated font was ignored and a
    // RelativeToChildren box stayed base-height while the enlarged run spilled above/below it. The stub
    // font's lineHeight == FontSize, so the size-40 swap font is exactly 2x the size-20 base.
    [Fact]
    public void WrappedTextHeight_ShouldGrow_WhenLineHasBbCodeFontSizeRun()
    {
        var previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new SizeProportionalFontCreator();
        try
        {
            TextRuntime plain = new();
            plain.WidthUnits = DimensionUnitType.RelativeToChildren;
            plain.Width = 0;
            plain.Font = "StubFont3520";
            plain.FontSize = 20;
            plain.Text = "AA BB CC";
            Text plainText = (Text)plain.RenderableComponent;
            float plainHeight = plainText.WrappedTextHeight;
            plainHeight.ShouldBeGreaterThan(0);

            TextRuntime swapped = new();
            swapped.WidthUnits = DimensionUnitType.RelativeToChildren;
            swapped.Width = 0;
            swapped.Font = "StubFont3520";
            swapped.FontSize = 20;
            // Same visible text; only "BB" is enlarged via a generated size-40 font (lineHeight 40).
            swapped.Text = "AA [FontSize=40]BB[/FontSize] CC";
            Text swappedText = (Text)swapped.RenderableComponent;
            float swappedHeight = swappedText.WrappedTextHeight;

            swappedHeight.ShouldBe(plainHeight * 2, plainHeight * 0.02,
                "because the [FontSize=40] run's taller size-40 font (lineHeight 40 vs the base's 20) must drive the measured line height");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
        }
    }

    // #3520 (the wrapping half, end-to-end): a fixed-width Text that fits "AA BB CC" at the base size must
    // re-wrap once the inline [FontSize=40] run enlarges "BB". The real BBCode path sets RawText (which
    // wraps) BEFORE the InlineVariables are populated, so the first wrap is blind to the runs; the fix must
    // re-wrap after the runs exist. Without that re-wrap this stays one line and the enlarged run spills
    // past the width. Widths use the stub creator's advance = FontSize/2 (base 20 -> 10, run 40 -> 20).
    [Fact]
    public void WrappedText_ShouldRewrapFontAware_WhenBbCodeFontSizeRunWidensLine()
    {
        var previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new SizeProportionalFontCreator();
        try
        {
            TextRuntime rt = new();
            rt.Font = "StubFont3520";
            rt.FontSize = 20;
            rt.WidthUnits = DimensionUnitType.Absolute;
            // Fits "AA BB CC" at the base size (8 chars * 10 = 80) but not with "BB" at size 40
            // (6*10 + 2*20 = 100). 85 sits between the two, so only a font-aware re-wrap breaks the line.
            rt.Width = 85;
            rt.Text = "AA [FontSize=40]BB[/FontSize] CC";

            var wrapped = ((Text)rt.RenderableComponent).WrappedText;
            wrapped.Count.ShouldBe(2,
                "because after the inline [FontSize] runs populate, the text must re-wrap measuring the enlarged run at its own size");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
        }
    }

    // Repro attempt for the manual-test structure the earlier tests missed: the box also holds a
    // RelativeToParent background sibling (added BEFORE the text). FontScale needs no font generation, so
    // this isolates whether the RelativeToParent sibling breaks the box's RelativeToChildren sizing.
    [Fact]
    public void RelativeToChildrenContainer_WithRelativeToParentBackground_ShouldContainFontScaleRun()
    {
        ContainerRuntime box = new();
        box.WidthUnits = DimensionUnitType.RelativeToChildren;
        box.Width = 0;
        box.Height = 100;
        box.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime background = new();
        background.WidthUnits = DimensionUnitType.RelativeToParent;
        background.Width = 0;
        background.HeightUnits = DimensionUnitType.RelativeToParent;
        background.Height = 0;
        box.Children.Add(background);

        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Text = "This is [FontScale=2]big[/FontScale] text.";
        box.Children.Add(text);

        box.AddToRoot();

        float textContentWidth = ((Text)text.RenderableComponent).WrappedTextWidth;
        textContentWidth.ShouldBeGreaterThan(0);
        box.GetAbsoluteWidth().ShouldBeGreaterThanOrEqualTo(textContentWidth,
            "because the RelativeToChildren box must be at least as wide as its text, even with a RelativeToParent background sibling");
        // #3645: containment alone isn't enough - a container that "correctly" grows to fit a wrapped
        // 2-line block still passes the containment assertion above even though the wrap itself is the
        // bug. (This assertion is a forward-looking regression guard, not a pin: the bug this issue fixed
        // - a [FontScale] run's rounded self-measured width coming out narrower than its own unrounded
        // content, forcing a self-inflicted wrap - reproduces on Raylib (real MeasureTextEx sub-pixel
        // widths; see RaylibGum.Tests TextRuntimeTests) but not through this MonoGame harness even with a
        // real generated font, matching the caveat in #3645 that the MonoGame path may not exercise the
        // same fractional-width case.)
        ((Text)text.RenderableComponent).WrappedText.Count.ShouldBe(1,
            "because RelativeToChildren on both the Text and its container means the content should size to fit on one line, not wrap");
    }

    // The #3520 bug as the user reported it: a RelativeToChildren-width container wrapping a
    // RelativeToChildren-width Text must grow to the FULL measured width of a [FontSize=40] run, so a
    // background filling the container also contains the text (no tail spill). The Text-level test above
    // pins WrappedTextWidth; this one pins that the parent container actually picks that width up through
    // layout propagation — the container-containment invariant the manual test showed failing.
    [Fact]
    public void RelativeToChildrenContainer_ShouldContainBbCodeFontSizeRun()
    {
        var previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new SizeProportionalFontCreator();
        try
        {
            ContainerRuntime plainContainer = new();
            plainContainer.WidthUnits = DimensionUnitType.RelativeToChildren;
            plainContainer.Width = 0;
            TextRuntime plainText = new();
            plainText.WidthUnits = DimensionUnitType.RelativeToChildren;
            plainText.Width = 0;
            plainText.Font = "StubFont3520";
            plainText.FontSize = 20;
            plainText.Text = "AA BB CC";
            plainContainer.Children.Add(plainText);
            plainContainer.AddToRoot();
            float plainContainerWidth = plainContainer.GetAbsoluteWidth();
            float baseBAdvance = plainText.BitmapFont.Characters['B'].XAdvance;

            ContainerRuntime swappedContainer = new();
            swappedContainer.WidthUnits = DimensionUnitType.RelativeToChildren;
            swappedContainer.Width = 0;
            TextRuntime swappedText = new();
            swappedText.WidthUnits = DimensionUnitType.RelativeToChildren;
            swappedText.Width = 0;
            swappedText.Font = "StubFont3520";
            swappedText.FontSize = 20;
            swappedText.Text = "AA [FontSize=40]BB[/FontSize] CC";
            swappedContainer.Children.Add(swappedText);
            swappedContainer.AddToRoot();
            float swappedContainerWidth = swappedContainer.GetAbsoluteWidth();

            InlineVariable fontSwapVariable = ((Text)swappedText.RenderableComponent)
                .InlineVariables.First(v => v.VariableName == "BitmapFont");
            float swappedBAdvance = ((BitmapFont)fontSwapVariable.Value).Characters['B'].XAdvance;

            // The container must wrap its child exactly — this is the "background contains the text" case.
            swappedContainerWidth.ShouldBe(swappedText.GetAbsoluteWidth(),
                "because a RelativeToChildren container must size to its child's full measured width");

            // And that width must include the enlarged run: swapping the two 'B' glyphs to the size-40
            // font widens the container by 2 * (swapped - base) versus the all-base plain container.
            double expectedWidth = plainContainerWidth + 2 * (swappedBAdvance - baseBAdvance);
            ((double)swappedContainerWidth).ShouldBe(expectedWidth, expectedWidth * 0.02,
                "because the container must grow to contain the [FontSize=40] run, not just the base-size text");
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousCreator;
        }
    }

    #endregion

    #region GetStyledSubstrings

    [Fact]
    public void GetStyledSubstrings_ShouldReturnTwoEntries_IfTextHasCodeAtEnd()
    {
        // Arrange
        var text = new Text();
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "IsBold",
            Value = true,
            StartIndex = 6,
            CharacterCount = 5
        });

        // Act
        var substrings = text.GetStyledSubstrings(0, "Hello World");
        // Assert
        substrings.Count.ShouldBe(2);
        substrings[0].Substring.ShouldBe("Hello ");
        substrings[0].Variables.Count.ShouldBe(0);

        substrings[1].Substring.ShouldBe("World");
        substrings[1].Variables.Count.ShouldBe(1);
        substrings[1].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[1].Variables[0].Value.ShouldBe(true);
    }

    [Fact]
    public void GetStyledSubstrings_ShouldReturnThreeEntries_IfTextHasCodeInMiddle()
    {
        // Arrange
        var text = new Text();
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "IsBold",
            Value = true,
            StartIndex = 1,
            CharacterCount = 1
        });

        // Act
        var substrings = text.GetStyledSubstrings(0, "012");
        // Assert
        substrings.Count.ShouldBe(3);
        substrings[0].Substring.ShouldBe("0");
        substrings[0].Variables.Count.ShouldBe(0);

        substrings[1].Substring.ShouldBe("1");
        substrings[1].Variables.Count.ShouldBe(1);
        substrings[1].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[1].Variables[0].Value.ShouldBe(true);

        substrings[2].Substring.ShouldBe("2");
        substrings[2].Variables.Count.ShouldBe(0);
    }

    [Fact]
    public void GetStyledSubstrings_ShouldRespectOverlappingCodes()
    {
        var text = new Text();
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "IsBold",
            Value = true,
            StartIndex = 1,
            CharacterCount = 3
        });

        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "FontScale",
            Value = 2,
            StartIndex = 2,
            CharacterCount = 1
        });

        // Act
        var substrings = text.GetStyledSubstrings(0, "01234");
        // Assert
        substrings.Count.ShouldBe(5);

        substrings[0].Substring.ShouldBe("0");
        substrings[0].Variables.Count.ShouldBe(0);

        substrings[1].Substring.ShouldBe("1");
        substrings[1].Variables.Count.ShouldBe(1);
        substrings[1].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[1].Variables[0].Value.ShouldBe(true);

        substrings[2].Substring.ShouldBe("2");
        substrings[2].Variables.Count.ShouldBe(2);
        substrings[2].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[2].Variables[0].Value.ShouldBe(true);
        substrings[2].Variables[1].VariableName.ShouldBe("FontScale");
        substrings[2].Variables[1].Value.ShouldBe(2);

        substrings[3].Substring.ShouldBe("3");
        substrings[3].Variables.Count.ShouldBe(1);
        substrings[3].Variables[0].VariableName.ShouldBe("IsBold");
        substrings[3].Variables[0].Value.ShouldBe(true);

        substrings[4].Substring.ShouldBe("4");
        substrings[4].Variables.Count.ShouldBe(0);
    }

    [Fact]
    public void GetStyledSubstrings_ShouldRespectOverlappingCodes_OfSameVariable()
    {
        var text = new Text();
        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "FontScale",
            Value = 2,
            StartIndex = 1,
            CharacterCount = 3
        });

        text.InlineVariables.Add(new InlineVariable
        {
            VariableName = "FontScale",
            Value = 3,
            StartIndex = 2,
            CharacterCount = 1
        });

        // Act
        var substrings = text.GetStyledSubstrings(0, "01234");
        // Assert
        substrings.Count.ShouldBe(5);

        substrings[0].Substring.ShouldBe("0");
        substrings[0].Variables.Count.ShouldBe(0);

        substrings[1].Substring.ShouldBe("1");
        substrings[1].Variables.Count.ShouldBe(1);
        substrings[1].Variables[0].VariableName.ShouldBe("FontScale");
        substrings[1].Variables[0].Value.ShouldBe(2);

        substrings[2].Substring.ShouldBe("2");
        substrings[2].Variables.Count.ShouldBe(1);
        substrings[2].Variables[0].VariableName.ShouldBe("FontScale");
        substrings[2].Variables[0].Value.ShouldBe(3);

        substrings[3].Substring.ShouldBe("3");
        substrings[3].Variables.Count.ShouldBe(1);
        substrings[3].Variables[0].VariableName.ShouldBe("FontScale");
        substrings[3].Variables[0].Value.ShouldBe(2);

        substrings[4].Substring.ShouldBe("4");
        substrings[4].Variables.Count.ShouldBe(0);
    }

    #endregion

    #region GlobalFontScale

    [Fact]
    public void GlobalFontScale_ShouldAffectAbsoluteHeight_WhenRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        sut.Height = 0;
        sut.Text = "Hello";

        var baseHeight = sut.GetAbsoluteHeight();
        baseHeight.ShouldBeGreaterThan(0);

        GraphicalUiElement.GlobalFontScale = 2;
        sut.UpdateLayout();

        sut.GetAbsoluteHeight().ShouldBe(baseHeight * 2);
    }

    [Fact]
    public void GlobalFontScale_ShouldAffectAbsoluteWidth_WhenRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        sut.Width = 0;
        sut.Text = "Hello";

        var baseWidth = sut.GetAbsoluteWidth();
        baseWidth.ShouldBeGreaterThan(0);

        GraphicalUiElement.GlobalFontScale = 2;
        sut.UpdateLayout();

        sut.GetAbsoluteWidth().ShouldBe(baseWidth * 2);
    }

    [Fact]
    public void GlobalFontScale_WrappedTextHeight_ShouldDoubleWhenScaleIsTwo()
    {
        TextRuntime sut = new();
        sut.Text = "Hello";

        var containedText = (Text)sut.RenderableComponent;
        var baseHeight = containedText.WrappedTextHeight;
        baseHeight.ShouldBeGreaterThan(0);

        GraphicalUiElement.GlobalFontScale = 2;

        containedText.WrappedTextHeight.ShouldBe(baseHeight * 2);
    }

    [Fact]
    public void GlobalFontScale_WrappedTextWidth_ShouldDoubleWhenScaleIsTwo()
    {
        TextRuntime sut = new();
        sut.Text = "Hello";

        var containedText = (Text)sut.RenderableComponent;
        var baseWidth = containedText.WrappedTextWidth;
        baseWidth.ShouldBeGreaterThan(0);

        GraphicalUiElement.GlobalFontScale = 2;

        containedText.WrappedTextWidth.ShouldBe(baseWidth * 2);
    }

    #endregion

    #region HasEvents

    [Fact]
    public void HasEvents_ShouldDefaultToFalse()
    {
        TextRuntime sut = new();
        sut.HasEvents.ShouldBeFalse();
    }

    #endregion

    #region IsBold

    [Fact]
    public void IsBold_ShouldChangeFont_OnFontPropertiesSet()
    {
        // file name is:
        // FontCache\Font18SomeFont_Italic_Bold.fnt
        var italicBoldFont = new BitmapFont((Texture2D)null!, fontPattern);
        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
        string fileName = FileManager.Standardize("FontCache\\Font18SomeFont_Italic_Bold.fnt", preserveCase: true, makeAbsolute: true);
        loaderManager.AddDisposable(fileName, italicBoldFont);

        TextRuntime sut = new();
        sut.UseCustomFont = true;
        // set up all the properties:
        sut.FontSize = 18;
        sut.Font = "SomeFont";
        sut.IsItalic = true;

        sut.UseCustomFont = false;

        sut.IsBold = true;

        sut.Typeface.ShouldBe(italicBoldFont);
    }

    #endregion

    #region MaxWidth

    [Fact]
    public void MaxWidth_ShouldWrapText_IfTextExceedsMaxWidth()
    {
        TextRuntime textRuntime = new();
        textRuntime.Width = 0;
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        textRuntime.MaxWidth = 50; // Set a max width
        textRuntime.Text = "a a a a a a a a a a a a a a a a a";

        textRuntime.GetAbsoluteWidth().ShouldBeLessThanOrEqualTo(50);
        var innerText = (Text)textRuntime.RenderableComponent;
        innerText.WrappedText.Count.ShouldBeGreaterThan(1);
        var lineCount = innerText.WrappedText.Count;

        var absoluteHeight = textRuntime.GetAbsoluteHeight();
        absoluteHeight.ShouldBe(lineCount * textRuntime.Typeface.LineHeightInPixels);
    }

    #endregion

    #region PropertyChanged

    [Fact]
    public void PropertyChanged_ShouldRaise_WhenTextChanges()
    {
        bool wasChanged = false;
        TextRuntime textRuntime = new();
        textRuntime.PropertyChanged += (_, _) =>
        {
            wasChanged = true;
        };

        textRuntime.Text = "Hello 1234";

        wasChanged.ShouldBeTrue();
    }

    #endregion

    #region TrySetPropertyOnText public API (FRB Glue compat)

    // #3641 regression: FlatRedBall's Glue tool generates FRB game projects' TextRuntime.Generated.cs
    // with a hardcoded direct call to
    // global::Gum.Wireframe.CustomSetPropertyOnRenderable.TrySetPropertyOnText(...). Since FRB1
    // compiles Gum/Wireframe/CustomSetPropertyOnRenderable.cs as source (via GumCoreShared.projitems,
    // not a compiled DLL), narrowing this method to private breaks every FRB game's build with
    // CS0117, even though nothing inside this repo calls it directly. Pin its accessibility so a
    // future refactor of CustomSetPropertyOnRenderable doesn't silently re-narrow it.
    [Fact]
    public void TrySetPropertyOnText_ShouldStayPublic_ForFrbGlueGeneratedCallers()
    {
        MethodInfo? method = typeof(CustomSetPropertyOnRenderable).GetMethod(
            "TrySetPropertyOnText", BindingFlags.Public | BindingFlags.Static);

        method.ShouldNotBeNull();
    }

    // Issue #3706 (ADR 0009): TrySetPropertyOnText's "TextOverflowHorizontalMode" branch
    // never set handled = true, so every string-path assignment (a saved Gum-project state using
    // this variable) fell through to AdditionalPropertyOnRenderable/reflection after already being
    // applied -- redundant at best, and would let a registered AdditionalPropertyOnRenderable hook
    // reprocess a property that was already handled.
    [Fact]
    public void TrySetPropertyOnText_TextOverflowHorizontalMode_ShouldReturnHandledTrue()
    {
        TextRuntime sut = new();
        Text textRenderable = (Text)sut.RenderableComponent;

        bool handled = CustomSetPropertyOnRenderable.TrySetPropertyOnText(
            textRenderable, sut, nameof(TextOverflowHorizontalMode), TextOverflowHorizontalMode.EllipsisLetter);

        handled.ShouldBeTrue();
    }

    #endregion

    #region Text (including bbcode and localization)

    [Fact]
    public void Text_WithSlashRSlashN_ShouldSetBbCodeCorrectly()
    {
        string text = $"[Color=Green]0[/Color]1\r\n[Color=Green]0[/Color]1";

        TextRuntime textRuntime = new ();
        textRuntime.Text = text;

        var internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        var inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(2);
        inlineVariables[0].StartIndex.ShouldBe(0);
        inlineVariables[0].CharacterCount.ShouldBe(1);

        inlineVariables[1].StartIndex.ShouldBe(3, "Because \\r character should not be included, so the newline 0 character starts at index 3");
        inlineVariables[1].CharacterCount.ShouldBe(1);
    }

    [Fact]
    public void Text_WithCustomAfterSlashRSlashN_ShouldUseNormalizedIndexes()
    {
        Func<int, string, LetterCustomization> method = (int index, string block) =>
        {
            return new LetterCustomization();
        };
        Text.Customizations["CustomAfterCRLF"] = method;

        string text = $"AB\r\n[Custom=CustomAfterCRLF]CD[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        var internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        var inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(2);
        foreach (InlineVariable variable in inlineVariables)
        {
            var asCall = (ParameterizedLetterCustomizationCall)variable.Value;
            asCall.TextBlock.ShouldBe("CD", "Because \\r should be stripped before indexes are computed, so the substring must not include it");
        }

        ((ParameterizedLetterCustomizationCall)inlineVariables[0].Value).CharacterIndex.ShouldBe(3,
            "Because \\r character should not be included, so the C character starts at index 3");
        ((ParameterizedLetterCustomizationCall)inlineVariables[1].Value).CharacterIndex.ShouldBe(4);
    }

    [Fact]
    public void Text_WithCustom_ShouldAssignMethodCall()
    {
        Func<int, string, LetterCustomization> method = (int index, string block) =>
        {
            return new LetterCustomization();
        };
        Text.Customizations["CustomMethod"] = method;

        string text = $"Hello [Custom=CustomMethod]custom[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        RenderingLibrary.Graphics.Text internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        List<InlineVariable> inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(6);
        foreach(InlineVariable variable in inlineVariables)
        {
            ParameterizedLetterCustomizationCall asCall = (ParameterizedLetterCustomizationCall)variable.Value;
            asCall.Function.ShouldBe(method);
        }
    }

    [Fact]
    public void Text_WithCustom_ShouldAssignMethods()
    {
        string text = $"Hello [Custom=CustomMethod]custom[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        Func<int, string, LetterCustomization> method = (int index, string block) =>
        {
            return new LetterCustomization();
        };
        Text.Customizations["CustomMethod"] = method;

        RenderingLibrary.Graphics.Text internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        List<InlineVariable> inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(6);
        foreach (InlineVariable variable in inlineVariables)
        {
            ParameterizedLetterCustomizationCall asCall = (ParameterizedLetterCustomizationCall)variable.Value;
            asCall.Function.ShouldBe(method);
        }
    }

    [Fact]
    public void Text_WithContextCustom_ShouldResolveContextFunction()
    {
        Func<int, string, LetterCustomization, LetterCustomization> method = (int index, string block, LetterCustomization context) =>
        {
            return context;
        };
        Text.ContextCustomizations["ContextMethod"] = method;

        string text = $"[Custom=ContextMethod]a[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        RenderingLibrary.Graphics.Text internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        List<InlineVariable> inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(1);
        ParameterizedLetterCustomizationCall asCall = (ParameterizedLetterCustomizationCall)inlineVariables[0].Value;
        asCall.ContextFunction.ShouldBe(method);
        asCall.Function.ShouldBeNull();
    }

    [Fact]
    public void Text_WithMultipleCustomTags_ShouldCreateStackedInlineVariables()
    {
        Func<int, string, LetterCustomization> methodA = (int index, string block) =>
        {
            return new LetterCustomization();
        };
        Func<int, string, LetterCustomization> methodB = (int index, string block) =>
        {
            return new LetterCustomization();
        };
        Text.Customizations["A"] = methodA;
        Text.Customizations["B"] = methodB;

        string text = $"[Custom=A][Custom=B]ab[/Custom][/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        RenderingLibrary.Graphics.Text internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        List<InlineVariable> inlineVariables = internalText.InlineVariables;

        // 2 characters x 2 custom tags = 4 inline variables
        inlineVariables.Count.ShouldBe(4);
        List<InlineVariable> variablesForFirstChar = inlineVariables.Where(v => v.StartIndex == 0).ToList();
        variablesForFirstChar.Count.ShouldBe(2);
    }

    [Fact]
    public void Text_WithContextCustom_ContextFunctionTakesPriorityOverSimple()
    {
        Func<int, string, LetterCustomization> simpleMethod = (int index, string block) =>
        {
            return new LetterCustomization();
        };
        Func<int, string, LetterCustomization, LetterCustomization> contextMethod = (int index, string block, LetterCustomization context) =>
        {
            return context;
        };
        // Register same name in both dictionaries
        Text.Customizations["Dual"] = simpleMethod;
        Text.ContextCustomizations["Dual"] = contextMethod;

        string text = $"[Custom=Dual]a[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        RenderingLibrary.Graphics.Text internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        List<InlineVariable> inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(1);
        ParameterizedLetterCustomizationCall asCall = (ParameterizedLetterCustomizationCall)inlineVariables[0].Value;
        // ContextFunction should resolve since it takes priority
        asCall.ContextFunction.ShouldBe(contextMethod);
        // Simple should also resolve, but the render path picks ContextFunction first
        asCall.Function.ShouldBe(simpleMethod);
    }

    [Fact]
    public void Text_WithSimpleCustom_ShouldReturnExpectedValues()
    {
        Func<int, string, LetterCustomization> method = (int index, string block) =>
        {
            return new LetterCustomization
            {
                XOffset = index * 10,
                Color = System.Drawing.Color.Blue,
            };
        };
        Text.Customizations["Offset"] = method;

        string text = $"[Custom=Offset]ab[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        RenderingLibrary.Graphics.Text internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        List<InlineVariable> inlineVariables = internalText.InlineVariables;

        ParameterizedLetterCustomizationCall firstCall = (ParameterizedLetterCustomizationCall)inlineVariables[0].Value;
        LetterCustomization firstResult = firstCall.Function!(firstCall.CharacterIndex, firstCall.TextBlock);
        firstResult.XOffset.ShouldBe((float?)0);
        firstResult.Color.ShouldBe(System.Drawing.Color.Blue);

        ParameterizedLetterCustomizationCall secondCall = (ParameterizedLetterCustomizationCall)inlineVariables[1].Value;
        LetterCustomization secondResult = secondCall.Function!(secondCall.CharacterIndex, secondCall.TextBlock);
        secondResult.XOffset.ShouldBe((float?)10);
        secondResult.Color.ShouldBe(System.Drawing.Color.Blue);
    }

    [Fact]
    public void Text_WithContextCustom_ShouldReceiveAndModifyContext()
    {
        Func<int, string, LetterCustomization, LetterCustomization> darken = (int index, string block, LetterCustomization context) =>
        {
            System.Drawing.Color c = context.Color ?? System.Drawing.Color.White;
            return new LetterCustomization
            {
                Color = System.Drawing.Color.FromArgb(c.A, c.R / 2, c.G / 2, c.B / 2),
            };
        };
        Text.ContextCustomizations["Darken"] = darken;

        string text = $"[Custom=Darken]a[/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        RenderingLibrary.Graphics.Text internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        List<InlineVariable> inlineVariables = internalText.InlineVariables;

        ParameterizedLetterCustomizationCall call = (ParameterizedLetterCustomizationCall)inlineVariables[0].Value;

        // Simulate context as if [Color=Red] had been applied before this Custom tag
        LetterCustomization context = new LetterCustomization
        {
            Color = System.Drawing.Color.Red,
        };
        LetterCustomization result = call.ContextFunction!(call.CharacterIndex, call.TextBlock, context);
        result.Color.ShouldNotBeNull();
        result.Color.Value.R.ShouldBe((byte)127);
        result.Color.Value.G.ShouldBe((byte)0);
        result.Color.Value.B.ShouldBe((byte)0);
    }

    [Fact]
    public void Text_WithStackedCustoms_SecondCanReadFirstOutput()
    {
        Func<int, string, LetterCustomization> setRed = (int index, string block) =>
        {
            return new LetterCustomization
            {
                Color = System.Drawing.Color.Red,
            };
        };
        Func<int, string, LetterCustomization, LetterCustomization> halveColor = (int index, string block, LetterCustomization context) =>
        {
            System.Drawing.Color c = context.Color ?? System.Drawing.Color.White;
            return new LetterCustomization
            {
                Color = System.Drawing.Color.FromArgb(c.A, c.R / 2, c.G / 2, c.B / 2),
            };
        };
        Text.Customizations["SetRed"] = setRed;
        Text.ContextCustomizations["HalveColor"] = halveColor;

        string text = $"[Custom=SetRed][Custom=HalveColor]a[/Custom][/Custom]";

        TextRuntime textRuntime = new();
        textRuntime.Text = text;

        RenderingLibrary.Graphics.Text internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        List<InlineVariable> inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(2);

        // Simulate the render loop's chaining: first function runs, its output becomes context for second
        ParameterizedLetterCustomizationCall firstCall = (ParameterizedLetterCustomizationCall)inlineVariables[0].Value;
        ParameterizedLetterCustomizationCall secondCall = (ParameterizedLetterCustomizationCall)inlineVariables[1].Value;

        // First function (SetRed) sets color to Red
        LetterCustomization firstResult = firstCall.Function!(firstCall.CharacterIndex, firstCall.TextBlock);
        firstResult.Color.ShouldBe(System.Drawing.Color.Red);

        // Second function (HalveColor) receives Red as context and halves it.
        // Simulate what the render loop does: use concrete defaults for null fields.
        LetterCustomization chainedContext = new LetterCustomization
        {
            Color = firstResult.Color ?? System.Drawing.Color.White,
            XOffset = firstResult.XOffset ?? 0,
            YOffset = firstResult.YOffset ?? 0,
            ScaleX = firstResult.ScaleX ?? 1,
            ScaleY = firstResult.ScaleY ?? 1,
        };
        LetterCustomization secondResult = secondCall.ContextFunction!(secondCall.CharacterIndex, secondCall.TextBlock, chainedContext);
        secondResult.Color.ShouldNotBeNull();
        secondResult.Color.Value.R.ShouldBe((byte)127);
        secondResult.Color.Value.G.ShouldBe((byte)0);
        secondResult.Color.Value.B.ShouldBe((byte)0);
    }

    [Fact]
    public void Text_ShouldUseLocalization()
    {
        TextRuntime textRuntime = new();

        CustomSetPropertyOnRenderable.LocalizationService = _localizationService.Object;
        _localizationService.Setup(x => x.Translate("T_StringId")).Returns("This is a localized string");
        textRuntime.Text = "T_StringId";

        textRuntime.Text.ShouldBe("This is a localized string");
    }

    [Fact]
    public void Text_WithLocalization_ShouldSetBbCodeCorrectly()
    {
        CustomSetPropertyOnRenderable.LocalizationService = _localizationService.Object;
        _localizationService
            .Setup(x => x.Translate("T_StringId"))
            .Returns("[Color=Green]0[/Color]1\r\n[Color=Green]0[/Color]1");

        TextRuntime textRuntime = new();
        textRuntime.Text = "T_StringId";

        var internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        var inlineVariables = internalText.InlineVariables;

        inlineVariables.Count.ShouldBe(2);
        inlineVariables[0].StartIndex.ShouldBe(0);
        inlineVariables[0].CharacterCount.ShouldBe(1);

        inlineVariables[1].StartIndex.ShouldBe(3, "Because \\r character should not be included, so the newline 0 character starts at index 3");
        inlineVariables[1].CharacterCount.ShouldBe(1);
    }

    [Fact]
    public void Text_WithBbCode_ShouldReParseInlineVariables_WhenFontSizeChanges()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "[Color=Green]Hello[/Color] World";

        var internalText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;
        var originalCount = internalText.InlineVariables.Count;
        originalCount.ShouldBe(1);

        var originalVariable = internalText.InlineVariables[0];

        // Act — change a font property, which should re-parse the BBCode
        textRuntime.FontSize = 24;

        // Assert — InlineVariables should be rebuilt, not accumulated or stale
        internalText.InlineVariables.Count.ShouldBe(originalCount);
        internalText.InlineVariables[0].ShouldNotBeSameAs(originalVariable);
        internalText.InlineVariables[0].VariableName.ShouldBe("Color");
        internalText.InlineVariables[0].StartIndex.ShouldBe(0);
        internalText.InlineVariables[0].CharacterCount.ShouldBe(5);
    }

    #endregion

    #region WrappedText

    [Fact]
    public void WrappedText_ShouldWrap_WithFixedWidth()
    {
        Text.IsMidWordLineBreakEnabled = false;

        TextRuntime textRuntime = new ();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.Width = 100; // Set a fixed width

        textRuntime.Text = "This is a long text that should wrap within the fixed width of 100 units.";

        textRuntime.WrappedText.Count.ShouldBeGreaterThan(1);

        textRuntime.WrappedText[0].ShouldStartWith("This is a");
        textRuntime.WrappedText[1].ShouldNotStartWith("This is a");
    }

    [Fact]
    public void WrappedText_ShouldNotBreakWords_IfBreakWordsWithNoWhitespaceIsFalse()
    {
        Text.IsMidWordLineBreakEnabled = false;
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.Width = 100; // Set a fixed width
        textRuntime.Text = "abcdefghijklmnopqrstuvwxyz 1abcdefghijklmnopqrstuvwxyz 12abcdefghijklmnopqrstuvwxyz";
        
        textRuntime.WrappedText.Count.ShouldBe(3);
        textRuntime.WrappedText[0].ShouldBe("abcdefghijklmnopqrstuvwxyz ");
        textRuntime.WrappedText[1].ShouldBe("1abcdefghijklmnopqrstuvwxyz ");
        textRuntime.WrappedText[2].ShouldBe("12abcdefghijklmnopqrstuvwxyz");
    }

    [Fact]
    public void WrappedText_ShouldPreserveLength_IfTextEndsWithSpace()
    {
        // Repro for https://github.com/vchelaru/Gum/issues/2617 — a trailing
        // space caused WrappedText to gain a phantom extra space, throwing off
        // TextBox caret math (caretIndex landed past the end of Text and a
        // subsequent backspace tried to remove past the string end).
        Text.IsMidWordLineBreakEnabled = false;
        var text = new Text();
        text.Width = 1000;

        text.RawText = "abc ";

        var totalLength = text.WrappedText.Sum(line => line.Length);
        totalLength.ShouldBe(text.RawText.Length);
    }

    [Fact]
    public void WrappedText_ShouldWrap_IfOnlyLettersExist()
    {
        Text text = new();
        Text.IsMidWordLineBreakEnabled = true;
        text.Width = 100;

        text.RawText = "abcdefghijklmnopqrstuvwxyz";

        text.WrappedText.Count.ShouldBeGreaterThan(1);
        text.WrappedText[0].ShouldStartWith("abc");
        text.WrappedText[1].ShouldNotStartWith("abc");
        text.WrappedText[1].ShouldStartWith("mno");
        char lastLine0 = text.WrappedText[0].Last();
        char firstCharacterInSecondLine = text.WrappedText[1][0];
        firstCharacterInSecondLine.ShouldBe((char)(lastLine0 + 1));
    }

    [Fact]
    public void WrappedText_ShouldWrapMidWord_WithMultipleLines()
    {
        // bypassing TextRuntime to test this directly:
        var text = new Text();
        text.Width = 14;
        Text.IsMidWordLineBreakEnabled = true;

        text.RawText = "01\n01";

        text.WrappedText.Count.ShouldBe(4);
        text.WrappedText[0].ShouldBe("0");
        text.WrappedText[1].ShouldBe("1\n");
        text.WrappedText[2].ShouldBe("0");
        text.WrappedText[3].ShouldBe("1");
    }

    [Fact]
    public void WrappedText_ShouldWrapMidWord_WithMultipleWords()
    {
        // bypassing TextRuntime to test this directly:
        var text = new Text();
        text.Width = 14;
        Text.IsMidWordLineBreakEnabled = true;

        text.RawText = "01 01";

        text.WrappedText.Count.ShouldBe(4);
        text.WrappedText[0].ShouldBe("0");
        text.WrappedText[1].ShouldBe("1 ");
        text.WrappedText[2].ShouldBe("0");
        text.WrappedText[3].ShouldBe("1");
    }

    [Fact]
    public void WrappedText_ShouldWrapMidWord_IfWidthMatchesLetterWidthExactly()
    {
        // each letter is 10 wide, so let's set a width that is a multiple of that:
        Text text = new();
        Text.IsMidWordLineBreakEnabled = true;
        text.Width = 30;

        text.RawText = "abcdefghijklmnopqrstuvwxyz";

        text.WrappedText.Count.ShouldBe(9);
        text.WrappedText[0].ShouldNotBeEmpty("abc");
        text.WrappedText[1].ShouldNotBeEmpty("def");
        text.WrappedText[2].ShouldNotBeEmpty("ghi");
        text.WrappedText[3].ShouldNotBeEmpty("jkl");
    }

    [Fact]
    public void WrappedText_ShouldPreferZeroWidthSpace_WhenBreakingMidWord()
    {
        // bypassing TextRuntime to test this directly:
        var text = new Text();
        text.Width = 86; 
        Text.IsMidWordLineBreakEnabled = true;

        // Create a long word with zero-width space at a preferred break point
        // "abcde\u200Bfghijklmno" - the zero-width space is after 'e'
        text.RawText = "abcde\u200Bfghijklmno";

        // Should break at the zero-width space position
        text.WrappedText.Count.ShouldBe(2);
        text.WrappedText[0].ShouldBe("abcde");
        text.WrappedText[1].ShouldBe("fghijklmno");
    }

    [Fact]
    public void WrappedText_ShouldRemoveZeroWidthSpace_WhenBreakingAtIt()
    {
        var text = new Text();
        text.Width = 50;
        Text.IsMidWordLineBreakEnabled = true;

        text.RawText = "abc\u200Bdef";

        // The zero-width space should be removed from output
        text.WrappedText.Count.ShouldBe(2);
        text.WrappedText[0].ShouldBe("abc");
        text.WrappedText[1].ShouldBe("def");

        // Neither line should contain the zero-width space character
        text.WrappedText[0].ShouldNotContain("\u200B");
        text.WrappedText[1].ShouldNotContain("\u200B");
    }

    [Fact]
    public void WrappedText_ShouldIgnoreZeroWidthSpace_IfItExceedsWrappingWidth()
    {
        var text = new Text();
        text.Width = 40; // Only fits about 4 characters
        Text.IsMidWordLineBreakEnabled = true;

        // Zero-width space is at position 7, but line can only fit 4 chars
        // Should break at regular position instead
        text.RawText = "abcdefg\u200Bhijklmnop";

        text.WrappedText.Count.ShouldBeGreaterThan(2);
        // First line should be less than the zero-width space position
        text.WrappedText[0].Length.ShouldBeLessThan(7);
    }

    [Fact]
    public void WrappedText_ShouldHandleMultipleZeroWidthSpaces_InSameWord()
    {
        var text = new Text();
        text.Width = 55; // Enough for about 5 characters
        Text.IsMidWordLineBreakEnabled = true;

        // Multiple zero-width spaces: "abc\u200Bdef\u200Bghi"
        // Should prefer the last one before exceeding width
        text.RawText = "abc\u200Bdef\u200Bghijklmno";

        text.WrappedText.Count.ShouldBe(4);
        // Should break at second zero-width space (after "def")
        text.WrappedText[0].ShouldBe("abc");
        text.WrappedText[1].ShouldBe("def");
    }

    #endregion

    #region UseCustomnFont


    [Fact]
    public void UseCustomFont_ShouldChangeFont_OnFontPropertiesSet()
    {
        // file name is:
        // FontCache\Font18SomeFont_Italic_Bold.fnt
        var italicBoldFont = new BitmapFont((Texture2D)null!, fontPattern);
        var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
        string fileName = FileManager.Standardize("FontCache\\Font18SomeFont_Italic_Bold.fnt", preserveCase:true, makeAbsolute:true);
        loaderManager.AddDisposable(fileName, italicBoldFont);

        TextRuntime sut = new();
        sut.UseCustomFont = true;
        // set up all the properties:
        sut.FontSize = 18;
        sut.Font = "SomeFont";
        sut.IsBold = true;
        sut.IsItalic = true;

        sut.UseCustomFont = false;

        sut.Typeface.ShouldBe(italicBoldFont);
    }

    #endregion

    #region Typeface

    // Typeface (issue #3708): unifies XNALIKE's BitmapFont / Raylib's CustomFont / Skia's new
    // Typeface under one name. BitmapFont/DefaultCustomFont are kept as [Obsolete] forwarders.

    [Fact]
    public void Typeface_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        BitmapFont font = new BitmapFont((Texture2D)null!, fontPattern);

        sut.Typeface = font;

        sut.Typeface.ShouldBe(font);
    }

    [Fact]
    public void BitmapFont_ObsoleteAlias_ShouldForwardToTypeface()
    {
        TextRuntime sut = new();
        BitmapFont font = new BitmapFont((Texture2D)null!, fontPattern);

#pragma warning disable CS0618 // intentionally exercising the obsolete forwarder
        sut.BitmapFont = font;

        sut.Typeface.ShouldBe(font);
        sut.BitmapFont.ShouldBe(font);
#pragma warning restore CS0618
    }

    [Fact]
    public void Typeface_SetProperty_ShouldForwardToTypeface()
    {
        TextRuntime sut = new();
        BitmapFont font = new BitmapFont((Texture2D)null!, fontPattern);

        sut.SetProperty("Typeface", font);

        sut.Typeface.ShouldBe(font);
    }

    [Fact]
    public void BitmapFont_LegacyStringName_SetProperty_ShouldForwardToTypeface()
    {
        // "BitmapFont" is kept as a legacy string alias in the dispatch bridge for any
        // persisted/generated code still calling SetProperty("BitmapFont", ...).
        TextRuntime sut = new();
        BitmapFont font = new BitmapFont((Texture2D)null!, fontPattern);

        sut.SetProperty("BitmapFont", font);

        sut.Typeface.ShouldBe(font);
    }

    [Fact]
    public void DefaultTypeface_AppliedInConstructor()
    {
        BitmapFont? saved = TextRuntime.DefaultTypeface;
        try
        {
            BitmapFont font = new BitmapFont((Texture2D)null!, fontPattern);
            TextRuntime.DefaultTypeface = font;

            TextRuntime sut = new();

            sut.Typeface.ShouldBe(font);
        }
        finally
        {
            TextRuntime.DefaultTypeface = saved;
        }
    }

    [Fact]
    public void DefaultCustomFont_ObsoleteAlias_ShouldForwardToDefaultTypeface()
    {
        BitmapFont? saved = TextRuntime.DefaultTypeface;
        try
        {
            BitmapFont font = new BitmapFont((Texture2D)null!, fontPattern);
#pragma warning disable CS0618 // intentionally exercising the obsolete forwarder
            TextRuntime.DefaultCustomFont = font;

            TextRuntime.DefaultTypeface.ShouldBe(font);
            TextRuntime.DefaultCustomFont.ShouldBe(font);
#pragma warning restore CS0618
        }
        finally
        {
            TextRuntime.DefaultTypeface = saved;
        }
    }

    #endregion

    [Fact]
    public void MaxNumberOfLetters_ShouldNotChangeDimensions()
    {
        TextRuntime textRuntime = new TextRuntime();
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        textRuntime.Width = 0;
        textRuntime.Text = "This is some sample text";

        textRuntime.GetAbsoluteWidth().ShouldBeGreaterThan(0);
        var absoluteWidth = textRuntime.GetAbsoluteWidth();

        textRuntime.MaxLettersToShow = 0;

        textRuntime.GetAbsoluteWidth().ShouldBe(absoluteWidth);


    }

    [Fact]
    public void Anchor_CenterHorizontally_ShouldSetCorrectValues()
    {
        TextRuntime textRuntime = new TextRuntime();
        textRuntime.HorizontalAlignment = HorizontalAlignment.Right;
        textRuntime.VerticalAlignment = VerticalAlignment.Bottom;
        textRuntime.Text = "This is some sample text";
        textRuntime.Anchor(Anchor.CenterHorizontally);

        // Should set center
        textRuntime.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);

        // Should not change vertical
        textRuntime.VerticalAlignment.ShouldBe(VerticalAlignment.Bottom);
    }

    [Fact]
    public void Anchor_CenterVertically_ShouldSetCorrectValues()
    {
        TextRuntime textRuntime = new TextRuntime();
        textRuntime.HorizontalAlignment = HorizontalAlignment.Right;
        textRuntime.VerticalAlignment = VerticalAlignment.Bottom;
        textRuntime.Text = "This is some sample text";
        textRuntime.Anchor(Anchor.CenterVertically);

        // Should not change Horizontal
        textRuntime.HorizontalAlignment.ShouldBe(HorizontalAlignment.Right);

        // Should set Center
        textRuntime.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
    }

    #region Defaults

    [Fact]
    public void DefaultFont_ShouldBeArial()
    {
        TextRuntime.DefaultFont.ShouldBe("Arial");
    }

    [Fact]
    public void DefaultFontSize_ShouldBe18()
    {
        TextRuntime.DefaultFontSize.ShouldBe(18);
    }

    #endregion

    #region FontFamily

    [Fact]
    public void Font_ShouldDelegateToFontFamily()
    {
        TextRuntime sut = new();
        sut.FontFamily = "Comic Sans MS";
        sut.Font.ShouldBe("Comic Sans MS");
    }

    [Fact]
    public void FontFamily_ShouldSetAndGetFont()
    {
        TextRuntime sut = new();
        sut.FontFamily = "Impact";
        sut.FontFamily.ShouldBe("Impact");
    }

    #endregion

    #region FontSize

    [Fact]
    public void FontSize_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.FontSize = 24;
        sut.FontSize.ShouldBe(24);
    }

    #endregion

    #region SetTextNoTranslate

    [Fact]
    public void SetTextNoTranslate_ShouldUpdateTextProperty()
    {
        TextRuntime sut = new();
        sut.SetTextNoTranslate("Translated Text");
        sut.Text.ShouldBe("Translated Text");
    }

    [Fact]
    public void SetTextNoTranslate_WhenNull_ShouldSetTextToNull()
    {
        TextRuntime sut = new();
        sut.Text = "Some text";
        sut.SetTextNoTranslate(null);
        sut.Text.ShouldBeNull();
    }

    #endregion

    #region TextOverflowHorizontalMode

    [Fact]
    public void TextOverflowHorizontalMode_Default_ShouldBeTruncateWord()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    [Fact]
    public void TextOverflowHorizontalMode_WhenSetToEllipsis_ShouldReadBackAsEllipsis()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.EllipsisLetter);
    }

    [Fact]
    public void TextOverflowHorizontalMode_WhenSetBackToTruncate_ShouldReadBackAsTruncate()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.TruncateWord;
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    #endregion

    #region UseFontSmoothing

    [Fact]
    public void UseFontSmoothing_ShouldDefaultToTrue()
    {
        TextRuntime sut = new();
        sut.UseFontSmoothing.ShouldBeTrue();
    }

    [Fact]
    public void UseFontSmoothing_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.UseFontSmoothing = false;
        sut.UseFontSmoothing.ShouldBeFalse();
    }

    #endregion

    #region TextOverflowVerticalMode + RelativeToChildren (issue #3372)

    // Issue #3372 background: a Text sized HeightUnits=RelativeToChildren derives its
    // height from its own WrappedTextHeight (height = WrappedTextHeight + Height-offset).
    // With TextOverflowVerticalMode.TruncateLine, UpdateLines then derives a max line
    // count back from that height:
    //     maxLinesFromHeight = (int)(Height / LineHeightInPixels)
    // That is a circular dependency: a negative Height offset (commonly used to tighten
    // line spacing) drops the height below the content's natural height, the integer
    // division floors to one-too-few lines, and because pushing the height re-triggers
    // wrapping, each layout pass shaves off another line. The two tests below pin the two
    // agreed invariants: (1) when height follows content, every line renders; (2) a
    // negative offset shrinks the bounds below the rendered text height without dropping
    // a line.

    // Invariant 1 (+ stability): RelativeToChildren height is derived from content, so the
    // height-based line cap must not fire — all lines render regardless of a negative
    // Height offset, and they keep rendering across repeated layouts (no progressive
    // "leave and return" collapse). An explicit MaxNumberOfLines is a separate constraint,
    // covered below.
    [Fact]
    public void TruncateLine_WithRelativeToChildrenHeight_ShouldRenderAllLinesStablyAcrossLayouts()
    {
        TextRuntime sut = new();
        sut.HeightUnits = DimensionUnitType.RelativeToChildren;
        sut.Height = -1;

        Text renderable = (Text)sut.RenderableComponent;
        renderable.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;

        sut.Text = "Line1\nLine2\nLine3";

        sut.UpdateLayout();
        sut.UpdateLayout();
        sut.UpdateLayout();

        sut.WrappedText.Count.ShouldBe(3,
            "Because RelativeToChildren height follows content, so no line may be truncated by the derived height, stably across layouts");
    }

    // Invariant 2: a negative Height offset shrinks the layout bounds below the rendered
    // text height (the "tighten spacing" intent) by exactly the offset — and, per
    // invariant 1, without dropping the line (so the rendered height stays positive).
    [Fact]
    public void RelativeToChildrenHeight_WithNegativeOffset_ShouldShrinkBoundsBelowRenderedTextHeight()
    {
        const float heightOffset = -5;

        TextRuntime sut = new();
        sut.HeightUnits = DimensionUnitType.RelativeToChildren;
        sut.Height = heightOffset;

        Text renderable = (Text)sut.RenderableComponent;
        renderable.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;

        sut.Text = "Hello";
        sut.UpdateLayout();

        float renderedHeight = renderable.WrappedTextHeight;
        renderedHeight.ShouldBeGreaterThan(0, "Because the single line must still be rendered");
        sut.GetAbsoluteHeight().ShouldBe(renderedHeight + heightOffset,
            "Because the bounds are the full rendered height plus the (negative) offset");
        sut.GetAbsoluteHeight().ShouldBeLessThan(renderedHeight);
        sut.GetAbsoluteHeight().ShouldBeGreaterThan(0);
    }

    // The exact issue #3372 repro: vertical truncation + RelativeToChildren height +
    // negative offset + an explicit one-line cap. An explicit MaxNumberOfLines is NOT the
    // height-derived cap and must still apply, so the text shows exactly one line — not
    // zero (the bug) and not all three.
    [Fact]
    public void TruncateLine_WithRelativeToChildrenHeightAndMaxNumberOfLines_ShouldHonorMaxNumberOfLines()
    {
        TextRuntime sut = new();
        sut.HeightUnits = DimensionUnitType.RelativeToChildren;
        sut.Height = -1;
        sut.MaxNumberOfLines = 1;

        Text renderable = (Text)sut.RenderableComponent;
        renderable.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;

        sut.Text = "Line1\nLine2\nLine3";
        sut.UpdateLayout();

        sut.WrappedText.Count.ShouldBe(1,
            "Because an explicit MaxNumberOfLines still caps wrapping, even though the height-derived cap is gone");
    }

    #endregion

}
