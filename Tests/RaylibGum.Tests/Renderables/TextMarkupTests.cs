using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
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
}
