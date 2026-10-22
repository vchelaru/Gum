using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// #6426: pins the vertical offset of each wrapped line of a styled (BBCode) Text, which the draw
/// pass and <see cref="Text.GetLineTopOffsets"/> share. A line advances by the base line height times the
/// tallest [FontScale] run on it (never less than the base), times the line height multiplier. Explicit
/// newlines keep the line split independent of font metrics, and every expectation is derived from the
/// per-line advance of a plain control text, so no font metric is hard-coded.
/// </summary>
public class TextLineTopOffsetTests : BaseTestClass
{
    private const string ThreeLines = "a\n[FontScale=3]b[/FontScale]\nc";

    private static float PlainLineAdvance()
    {
        TextRuntime plain = new();
        plain.Text = "a\nb";
        float advance = ((Text)plain.RenderableComponent!).GetLineTopOffsets()[1];
        advance.ShouldBeGreaterThan(0);
        return advance;
    }

    private static Text Styled(string markup, out TextRuntime runtime)
    {
        runtime = new TextRuntime();
        runtime.Text = markup;
        return (Text)runtime.RenderableComponent!;
    }

    [Fact]
    public void GetLineTopOffsets_RunOnMiddleLine_ShiftsOnlyTheLinesBelowIt()
    {
        float h = PlainLineAdvance();

        IReadOnlyList<float> offsets = Styled("a\n[FontScale=2]b[/FontScale]\nc\nd", out _).GetLineTopOffsets();

        offsets.Count.ShouldBe(4);
        offsets[0].ShouldBe(0);
        offsets[1].ShouldBe(h, 0.01);
        offsets[2].ShouldBe(h + 2 * h, 0.01);
        offsets[3].ShouldBe(h + 2 * h + h, 0.01);
    }

    [Fact]
    public void GetLineTopOffsets_RunSmallerThanBase_DoesNotShrinkTheLine()
    {
        float h = PlainLineAdvance();

        IReadOnlyList<float> offsets = Styled("a\n[FontScale=0.5]b[/FontScale]\nc", out _).GetLineTopOffsets();

        offsets[1].ShouldBe(h, 0.01);
        offsets[2].ShouldBe(2 * h, 0.01, "because a run smaller than the base scale does not shrink its line");
    }

    [Fact]
    public void GetLineTopOffsets_ColorOnlyRuns_AdvanceLikePlainText()
    {
        float h = PlainLineAdvance();

        IReadOnlyList<float> offsets = Styled("[Color=Red]a[/Color]\nb\n[Color=Blue]c[/Color]", out _).GetLineTopOffsets();

        offsets[1].ShouldBe(h, 0.01);
        offsets[2].ShouldBe(2 * h, 0.01);
    }

    [Fact]
    public void GetLineTopOffsets_LargestRunOnALineWins()
    {
        float h = PlainLineAdvance();

        IReadOnlyList<float> offsets = Styled("[FontScale=2]a[/FontScale] [FontScale=3]b[/FontScale]\nc", out _).GetLineTopOffsets();

        offsets[1].ShouldBe(3 * h, 0.01);
    }

    [Fact]
    public void GetLineTopOffsets_AfterTextChange_FollowsTheNewLines()
    {
        float h = PlainLineAdvance();
        Text text = Styled("a\n[FontScale=2]b[/FontScale]\nc", out TextRuntime runtime);
        text.GetLineTopOffsets()[2].ShouldBe(3 * h, 0.01);

        runtime.Text = "[FontScale=3]a[/FontScale]\nb\nc\nd";

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();
        offsets.Count.ShouldBe(4);
        offsets[1].ShouldBe(3 * h, 0.01);
        offsets[2].ShouldBe(4 * h, 0.01);
        offsets[3].ShouldBe(5 * h, 0.01);
    }

    [Fact]
    public void GetLineTopOffsets_AfterMovingTheRunToAnotherLine_FollowsTheRun()
    {
        float h = PlainLineAdvance();
        Text text = Styled("[FontScale=2]a[/FontScale]\nb\nc", out TextRuntime runtime);
        text.GetLineTopOffsets()[1].ShouldBe(2 * h, 0.01);

        runtime.Text = "a\nb\n[FontScale=2]c[/FontScale]";

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();
        offsets[1].ShouldBe(h, 0.01);
        offsets[2].ShouldBe(2 * h, 0.01);
    }

    [Fact]
    public void GetLineTopOffsets_AfterLineHeightMultiplierChange_ScalesTheAdvance()
    {
        float h = PlainLineAdvance();
        Text text = Styled("a\n[FontScale=2]b[/FontScale]\nc", out TextRuntime runtime);
        text.GetLineTopOffsets()[2].ShouldBe(3 * h, 0.01);

        runtime.LineHeightMultiplier = 1.5f;

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();
        offsets[1].ShouldBe(1.5f * h, 0.01);
        offsets[2].ShouldBe(1.5f * 3 * h, 0.01);
    }

    [Fact]
    public void GetLineTopOffsets_AfterFontScaleChange_RunHeightFollowsTheNewBase()
    {
        float h = PlainLineAdvance();
        Text text = Styled("a\n[FontScale=3]b[/FontScale]\nc", out TextRuntime runtime);
        text.GetLineTopOffsets()[2].ShouldBe(4 * h, 0.01);

        runtime.FontScale = 2;

        // Base scale 2: the plain line advances 2h; the [FontScale=3] run is an absolute scale, so its line advances 3h.
        IReadOnlyList<float> offsets = text.GetLineTopOffsets();
        offsets[1].ShouldBe(2 * h, 0.01);
        offsets[2].ShouldBe(5 * h, 0.01);
    }

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1f)]
    [InlineData(2f, 1f)]
    [InlineData(0.5f, 1.5f)]
    [InlineData(1f, 1.5f)]
    [InlineData(2f, 1.5f)]
    public void GetLineTopOffsets_PlainText_AdvanceScalesWithFontScaleAndMultiplier(float fontScale, float multiplier)
    {
        float h = PlainLineAdvance();
        TextRuntime runtime = new();
        runtime.Text = "a" + (char)10 + "b";
        runtime.FontScale = fontScale;
        runtime.LineHeightMultiplier = multiplier;

        IReadOnlyList<float> offsets = ((Text)runtime.RenderableComponent!).GetLineTopOffsets();

        offsets[0].ShouldBe(0);
        offsets[1].ShouldBe(h * fontScale * multiplier, 0.01);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void GetLineTopOffsets_StyledText_AtFontScaleOne_AdvanceIsUnchanged(float multiplier)
    {
        float h = PlainLineAdvance();
        Text text = Styled(ThreeLines, out TextRuntime runtime);
        runtime.LineHeightMultiplier = multiplier;

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();

        offsets[1].ShouldBe(h * multiplier, 0.01);
        offsets[2].ShouldBe(4 * h * multiplier, 0.01);
    }

    [Fact]
    public void GetLineTopOffsets_StyledText_BaseFontScaleTwo_RunScaleIsAbsolute()
    {
        float h = PlainLineAdvance();
        Text text = Styled(ThreeLines, out TextRuntime runtime);

        runtime.FontScale = 2;

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();
        offsets[0].ShouldBe(0);
        offsets[1].ShouldBe(2 * h, 0.01);
        offsets[2].ShouldBe(5 * h, 0.01);
    }

    [Fact]
    public void GetLineTopOffsets_WrappedStyledText_FollowsTheWrapWidth()
    {
        string markup = "aaa bbb [FontScale=2]ccc[/FontScale] ddd eee fff";
        TextRuntime runtime = new();
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.Width = 100000;
        runtime.Text = markup;
        Text text = (Text)runtime.RenderableComponent!;
        text.WrappedText.Count.ShouldBe(1);
        text.GetLineTopOffsets().Count.ShouldBe(1);

        runtime.Width = 40;

        text.WrappedText.Count.ShouldBeGreaterThan(2);
        IReadOnlyList<float> offsets = text.GetLineTopOffsets();
        offsets.Count.ShouldBe(text.WrappedText.Count);
        offsets[0].ShouldBe(0);
        for (int i = 1; i < offsets.Count; i++)
        {
            offsets[i].ShouldBeGreaterThan(offsets[i - 1]);
        }
    }
}
