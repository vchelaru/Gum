using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #7787: setting Typeface to null after Text was set leaves a stale measured size.
// The Skip reason names the issue; remove it with the fix.
public class TextRuntimeTypefaceTests : BaseTestClass
{
    const string Plain = "Hi there and a long tail of words to wrap around nicely.";
    const string Tagged = "Hi [Color=Red]red[/Color] [FontSize=40]big[/FontSize] and a long tail of words to wrap around nicely.";
    const string Other = "Something else entirely, also long enough to wrap around a bit.";
    const string KnownRed = "Issue #7787: Typeface = null leaves a stale measured size";

    static TextRuntime Make()
    {
        TextRuntime r = new();
        r.WidthUnits = DimensionUnitType.Absolute;
        r.Width = 200;
        r.HeightUnits = DimensionUnitType.RelativeToChildren;
        r.Height = 0;
        return r;
    }

    static TextRuntime FreshNullTypeface(string text)
    {
        TextRuntime fresh = Make();
        fresh.Typeface = null;
        fresh.Text = text;
        fresh.UpdateLayout();
        return fresh;
    }

    static TextRuntime TextThenNull(string text, string textAfter)
    {
        TextRuntime r = Make();
        r.Text = text;
        r.UpdateLayout();
        r.Typeface = null;
        r.Text = textAfter;
        r.UpdateLayout();
        return r;
    }

    [Theory(Skip = KnownRed)]
    [InlineData(Plain)]
    [InlineData(Tagged)]
    [InlineData("")]
    public void Typeface_SetNullAfterText_SameString_MatchesFresh(string text)
    {
        TextRuntime r = TextThenNull(text, text);
        TextRuntime fresh = FreshNullTypeface(text);
        r.AbsoluteHeight.ShouldBe(fresh.AbsoluteHeight);
        r.AbsoluteWidth.ShouldBe(fresh.AbsoluteWidth);
    }

    [Theory(Skip = KnownRed)]
    [InlineData(Plain)]
    [InlineData(Tagged)]
    [InlineData("")]
    public void Typeface_SetNonNullThenNullAfterText_SameString_MatchesFresh(string text)
    {
        TextRuntime donor = Make();
        donor.FontSize = 60;
        donor.Text = "x";
        donor.UpdateLayout();
        BitmapFont? other = donor.Typeface;

        TextRuntime r = Make();
        r.Text = text;
        r.UpdateLayout();
        r.Typeface = other;
        r.UpdateLayout();
        r.Typeface = null;
        r.Text = text;
        r.UpdateLayout();

        TextRuntime fresh = FreshNullTypeface(text);
        r.AbsoluteHeight.ShouldBe(fresh.AbsoluteHeight);
        r.AbsoluteWidth.ShouldBe(fresh.AbsoluteWidth);
    }

    [Theory(Skip = KnownRed)]
    [InlineData(Plain)]
    [InlineData(Tagged)]
    public void Typeface_SetNullAfterText_DifferentString_MatchesFresh(string text)
    {
        TextRuntime r = TextThenNull(text, Other);
        TextRuntime fresh = FreshNullTypeface(Other);
        r.AbsoluteHeight.ShouldBe(fresh.AbsoluteHeight);
        r.AbsoluteWidth.ShouldBe(fresh.AbsoluteWidth);
    }
}
