using Gum.Renderables;
using Gum.GueDeriving;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// #6450: how LineHeightMultiplier combines with a BBCode [FontScale] run on a line. The reference is
/// MonoGame's Text.ComputeStyledLineHeight: a line holding a size run is
/// <c>max(baseLineHeight * baseScale * multiplier, baseLineHeight * runScale)</c> - the run is an absolute
/// scale and is never multiplied - while a line without a size run (plain, or colour-only runs) is
/// <c>baseLineHeight * baseScale * multiplier</c>. h below is one plain line at scale 1 and multiplier 1,
/// read from a control text, so no font metric is hard-coded.
/// </summary>
public class TextRunLineHeightMultiplierTests : BaseTestClass
{
    private static float PlainLineHeight()
    {
        TextRuntime plain = new();
        plain.Text = "a\nb";
        float height = ((Text)plain.RenderableComponent!).GetLineTopOffsets()[1];
        height.ShouldBeGreaterThan(0);
        return height;
    }

    private static Text Build(string markup, float baseScale, float multiplier)
    {
        TextRuntime runtime = new();
        runtime.FontScale = baseScale;
        runtime.LineHeightMultiplier = multiplier;
        runtime.Text = markup;
        return (Text)runtime.RenderableComponent!;
    }

    private static float RunLine(float h, float baseScale, float runScale, float multiplier) =>
        h * System.Math.Max(baseScale * multiplier, runScale);

    private static void ShouldReportTheDrawnHeight(Text text, float lastLineAdvance)
    {
        IReadOnlyList<float> offsets = text.GetLineTopOffsets();
        // The reported height is stored as whole base units, so allow one pixel of rounding at scale 2.
        text.WrappedTextHeight.ShouldBe(offsets[offsets.Count - 1] + lastLineAdvance, 1.01);
    }

    public static IEnumerable<object[]> BaseRunMultiplier()
    {
        foreach (float baseScale in new[] { 0.5f, 1f, 2f })
        {
            foreach (float runScale in new[] { 0.75f, 1.5f, 3f })
            {
                foreach (float multiplier in new[] { 1f, 1.5f, 0.8f })
                {
                    yield return new object[] { baseScale, runScale, multiplier };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(BaseRunMultiplier))]
    public void RunLine_BetweenPlainLines_AdvancesByMaxOfScaledBaseAndRun(float baseScale, float runScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build($"a\n[FontScale={runScale}]b[/FontScale]\nc", baseScale, multiplier);

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();

        float plain = h * baseScale * multiplier;
        float run = RunLine(h, baseScale, runScale, multiplier);
        offsets[0].ShouldBe(0);
        offsets[1].ShouldBe(plain, 0.01);
        offsets[2].ShouldBe(plain + run, 0.01);
        ShouldReportTheDrawnHeight(text, plain);
    }

    [Theory]
    [MemberData(nameof(BaseRunMultiplier))]
    public void RunLine_AsLastLine_ReportedHeightMatchesTheDrawnAdvance(float baseScale, float runScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build($"a\n[FontScale={runScale}]b[/FontScale]", baseScale, multiplier);

        ShouldReportTheDrawnHeight(text, RunLine(h, baseScale, runScale, multiplier));
    }

    [Fact]
    public void RunLine_LargerThanBase_IsNotMultiplied()
    {
        float h = PlainLineHeight();

        // Base 2, run 3, multiplier 1.5: the run line is 3h (72 px when h is 24), not 3 * 1.5h.
        IReadOnlyList<float> offsets = Build("a\n[FontScale=3]b[/FontScale]\nc", 2, 1.5f).GetLineTopOffsets();

        offsets[1].ShouldBe(2 * 1.5f * h, 0.01);
        (offsets[2] - offsets[1]).ShouldBe(3 * h, 0.01);
    }

    [Fact]
    public void RunLine_ScaledBaseExceedsRun_UsesTheMultipliedBase()
    {
        float h = PlainLineHeight();

        // Base 2 at multiplier 1.5 is 3h; the run is only 2.5h, so the multiplied base wins.
        IReadOnlyList<float> offsets = Build("[FontScale=2.5]a[/FontScale]\nb", 2, 1.5f).GetLineTopOffsets();

        offsets[1].ShouldBe(3 * h, 0.01);
    }

    [Fact]
    public void RunLine_RunEqualToBase_AtMultiplierBelowOne_IsNotShrunk()
    {
        float h = PlainLineHeight();

        // The run is a size run, so the line is max(0.8 * 2, 2) * h, unlike a colour-only run line.
        IReadOnlyList<float> offsets = Build("[FontScale=2]a[/FontScale]\nb", 2, 0.8f).GetLineTopOffsets();

        offsets[1].ShouldBe(2 * h, 0.01);
    }

    [Fact]
    public void ColourOnlyRunLine_AtMultiplierBelowOne_AdvancesLikePlainText()
    {
        float h = PlainLineHeight();

        IReadOnlyList<float> offsets = Build("[Color=Red]a[/Color]\n[Color=Blue]b[/Color]\nc", 2, 0.8f).GetLineTopOffsets();

        offsets[1].ShouldBe(2 * 0.8f * h, 0.01);
        offsets[2].ShouldBe(2 * 2 * 0.8f * h, 0.01);
    }

    [Fact]
    public void ConsecutiveRunLines_EachAdvanceByTheirOwnMax()
    {
        float h = PlainLineHeight();
        Text text = Build("[FontScale=3]a[/FontScale]\n[FontScale=1.5]b[/FontScale]\nc", 2, 1.5f);

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();

        // Run 3 beats 2 * 1.5; run 1.5 loses to 2 * 1.5.
        offsets[1].ShouldBe(3 * h, 0.01);
        offsets[2].ShouldBe(3 * h + 3 * h, 0.01);
        ShouldReportTheDrawnHeight(text, 3 * h);
    }

    [Fact]
    public void RunLine_LargestRunOnTheLineWins_NotMultiplied()
    {
        float h = PlainLineHeight();

        IReadOnlyList<float> offsets = Build("[FontScale=2]a[/FontScale] [FontScale=4]b[/FontScale]\nc", 1, 1.5f).GetLineTopOffsets();

        offsets[1].ShouldBe(4 * h, 0.01);
    }

    [Fact]
    public void FontSizeRun_LargerThanMultipliedBase_IsNotMultiplied()
    {
        float h = PlainLineHeight();
        TextRuntime runtime = new();
        runtime.LineHeightMultiplier = 1.5f;
        runtime.Text = "x";
        Text text = (Text)runtime.RenderableComponent!;
        // No font creator is wired in these tests, so [FontSize] scales the base atlas to the given pixel size.
        int doubleBaseSize = text.Font.BaseSize * 2;
        runtime.Text = $"a\n[FontSize={doubleBaseSize}]b[/FontSize]\nc";

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();

        offsets[1].ShouldBe(1.5f * h, 0.01);
        (offsets[2] - offsets[1]).ShouldBe(2 * h, 0.01, "because a size-2 run beats the multiplied base (1.5) and is not itself multiplied");
        ShouldReportTheDrawnHeight(text, 1.5f * h);
    }

    [Fact]
    public void RunLine_AfterMultiplierChange_UsesTheNewMultiplier()
    {
        float h = PlainLineHeight();
        TextRuntime runtime = new();
        runtime.FontScale = 2;
        runtime.Text = "a\n[FontScale=3]b[/FontScale]\nc";
        Text text = (Text)runtime.RenderableComponent!;
        text.GetLineTopOffsets()[2].ShouldBe(2 * h + 3 * h, 0.01);

        runtime.LineHeightMultiplier = 2;

        IReadOnlyList<float> offsets = text.GetLineTopOffsets();
        offsets[1].ShouldBe(4 * h, 0.01);
        offsets[2].ShouldBe(4 * h + 4 * h, 0.01, "because the multiplied base (2 * 2) now beats the run (3)");
        ShouldReportTheDrawnHeight(text, 4 * h);
    }
}
