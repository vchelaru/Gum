using Gum.DataTypes;
using Gum.GueDeriving;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

// Issue #7787 (Skia counterpart): a null Typeface re-measures like a fresh text.
public class TextRuntimeTypefaceTests : BaseTestClass
{
    const string Plain = "Hi there and a long tail of words to wrap around nicely.";
    const string Other = "Something else entirely, also long enough to wrap around a bit.";

    public TextRuntimeTypefaceTests()
    {
        new RenderingLibrary.SystemManagers().Initialize();
    }

    static TextRuntime Make()
    {
        TextRuntime r = new();
        r.WidthUnits = DimensionUnitType.Absolute;
        r.Width = 200;
        r.HeightUnits = DimensionUnitType.RelativeToChildren;
        r.Height = 0;
        return r;
    }

    static TextRuntime Fresh(string text)
    {
        TextRuntime fresh = Make();
        fresh.Typeface = null;
        fresh.Text = text;
        fresh.UpdateLayout();
        return fresh;
    }

    [Theory]
    [InlineData(Plain, Plain)]
    [InlineData(Plain, Other)]
    [InlineData("", "")]
    public void Typeface_SetNonNullThenNullAfterText_MatchesFresh(string first, string after)
    {
        TextRuntime r = Make();
        r.Text = first;
        r.UpdateLayout();
        r.Typeface = SKTypeface.Default;
        r.UpdateLayout();
        r.Typeface = null;
        r.Text = after;
        r.UpdateLayout();

        TextRuntime fresh = Fresh(after);
        r.AbsoluteHeight.ShouldBe(fresh.AbsoluteHeight);
        r.AbsoluteWidth.ShouldBe(fresh.AbsoluteWidth);
    }

    [Theory]
    [InlineData(Plain, Plain)]
    [InlineData(Plain, Other)]
    public void Typeface_SetNullAfterText_MatchesFresh(string first, string after)
    {
        TextRuntime r = Make();
        r.Text = first;
        r.UpdateLayout();
        r.Typeface = null;
        r.Text = after;
        r.UpdateLayout();

        TextRuntime fresh = Fresh(after);
        r.AbsoluteHeight.ShouldBe(fresh.AbsoluteHeight);
        r.AbsoluteWidth.ShouldBe(fresh.AbsoluteWidth);
    }
}
