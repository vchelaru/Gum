using Gum.GueDeriving;
using Gum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;
using static RaylibGum.Tests.Rendering.TextMeasureHelpers;

namespace RaylibGum.Tests.Rendering;

/// <summary>Gaps not pinned by TextControlCharacterTests (#6835): wrapping, CRLF, hit testing, styled runs.</summary>
public class TextControlCharacterGapTests : BaseTestClass
{
    // The shared wrapper (IWrappedTextExtensions) only breaks at ' ', so a tab glues its neighbors into one
    // word that is then cut mid-word; MonoGame runs the same wrapper over a tab that advances four spaces.
    [Fact]
    public void Wrap_Tab_IsNotABreakOpportunity()
    {
        (TextRuntime runtime, Text text) = Make("aaa\tbbb ccc ddd", width: 60);
        List<string> lines = text.WrappedText.ToList();
        Remove(runtime);

        lines.ShouldBe(new List<string> { "aaa\tb", "bb ccc ", "ddd" });
    }

    [Fact]
    public void Wrap_Crlf_HasSameLinesAsLf()
    {
        (TextRuntime r1, Text t1) = Make("ab\r\ncd");
        List<string> crlf = t1.WrappedText.ToList();
        float crlfWidth = r1.AbsoluteWidth;
        (TextRuntime r2, Text t2) = Make("ab\ncd");
        List<string> lf = t2.WrappedText.ToList();
        float lfWidth = r2.AbsoluteWidth;
        Remove(r1); Remove(r2);

        crlf.Count.ShouldBe(lf.Count);
        crlfWidth.ShouldBe(lfWidth, 0.01f);
    }

    [Fact]
    public void HitTest_AfterTab_ReturnsIndexInOriginalString()
    {
        (TextRuntime r, Text t) = Make("a\tb");
        float left = 0f;
        // Just left of the end of the line: caret after 'b' is index 3 in the ORIGINAL string, not 6.
        int atEnd = t.GetCharacterIndexAtPosition(left + r.AbsoluteWidth + 5, 0f + 2);
        // Inside the tab's blank span: caret must be index 1 or 2, never past the tab's 4 columns.
        float afterA = t.MeasureString("a");
        int inTab = t.GetCharacterIndexAtPosition(left + afterA + 4, 0f + 2);
        Remove(r);

        atEnd.ShouldBe(3);
        inTab.ShouldBeInRange(1, 2);
    }

    [Fact]
    public void Measure_StyledRunWithTab_MatchesFourSpaces()
    {
        (TextRuntime r1, Text t1) = Make("[Color=Red]a\tb[/Color]");
        float tab = r1.AbsoluteWidth;
        (TextRuntime r2, Text t2) = Make("[Color=Red]a    b[/Color]");
        float spaces = r2.AbsoluteWidth;
        Remove(r1); Remove(r2);

        tab.ShouldBe(spaces, 0.01f);
    }
}
