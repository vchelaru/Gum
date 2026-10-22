using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A NaN or infinite MinWidth, MinHeight, MaxWidth or MaxHeight is no limit (#6473), everywhere layout reads it: the
/// clamp of the size, the line breaks of a wrapping stack and the wrap width of a text. Each case lays out exactly as the
/// same tree with no bound.
/// </summary>
public class NonFiniteSizeBoundLayoutTests : BaseTestClass
{
    public static TheoryData<string, float> BoundCases()
    {
        TheoryData<string, float> data = new();
        foreach (string bound in new[] { "MinWidth", "MinHeight", "MaxWidth", "MaxHeight" })
        {
            foreach (float value in new[] { float.PositiveInfinity, float.NegativeInfinity, float.NaN })
            {
                data.Add(bound, value);
            }
        }
        return data;
    }

    public static TheoryData<string, float> MaxCases()
    {
        TheoryData<string, float> data = new();
        foreach (string bound in new[] { "MaxWidth", "MaxHeight" })
        {
            foreach (float value in new[] { float.PositiveInfinity, float.NegativeInfinity, float.NaN })
            {
                data.Add(bound, value);
            }
        }
        return data;
    }

    // A stack that grows to its children and wraps, five 80 x 20 children, with the bound on the axis it stacks along
    // (width for a row, height for a column, whose children are 20 x 80). Everything a wrap changes is in the snapshot.
    private static string WrappingStackLayout(string bound, float? value)
    {
        bool row = bound.EndsWith("Width");
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        parent.ChildrenLayout = row ? ChildrenLayout.LeftToRightStack : ChildrenLayout.TopToBottomStack;
        parent.WrapsChildren = true;
        for (int i = 0; i < 5; i++)
        {
            ContainerRuntime child = new();
            child.Width = row ? 80 : 20;
            child.Height = row ? 20 : 80;
            parent.AddChild(child);
        }

        parent.SetProperty(bound, value);

        StringBuilder snapshot = new();
        snapshot.Append(parent.AbsoluteWidth).Append('x').Append(parent.AbsoluteHeight);
        foreach (GraphicalUiElement child in parent.Children!)
        {
            snapshot.Append(" (").Append(child.AbsoluteLeft - parent.AbsoluteLeft).Append(',')
                .Append(child.AbsoluteTop - parent.AbsoluteTop).Append(')');
        }
        return snapshot.ToString();
    }

    [Fact]
    public void WrappingStack_FiniteMaximum_StillWraps()
    {
        WrappingStackLayout("MaxWidth", 200f).ShouldNotBe(WrappingStackLayout("MaxWidth", null));
        WrappingStackLayout("MaxHeight", 200f).ShouldNotBe(WrappingStackLayout("MaxHeight", null));
    }

    [Theory]
    [MemberData(nameof(BoundCases))]
    public void WrappingStack_NonFiniteBound_LaysOutAsWithNoBound(string bound, float value)
    {
        WrappingStackLayout(bound, value).ShouldBe(WrappingStackLayout(bound, null));
    }

    // The bound on the other axis than the one the stack runs along is the stack's size limit only.
    [Theory]
    [MemberData(nameof(MaxCases))]
    public void WrappingStack_NonFiniteBoundOnTheCrossAxis_LaysOutAsWithNoBound(string bound, float value)
    {
        string crossBound = bound == "MaxWidth" ? "MaxHeight" : "MaxWidth";
        string Layout(float? v)
        {
            ContainerRuntime parent = new();
            parent.WidthUnits = DimensionUnitType.RelativeToChildren;
            parent.HeightUnits = DimensionUnitType.RelativeToChildren;
            parent.Width = 0;
            parent.Height = 0;
            parent.ChildrenLayout = bound == "MaxWidth" ? ChildrenLayout.LeftToRightStack : ChildrenLayout.TopToBottomStack;
            parent.WrapsChildren = true;
            for (int i = 0; i < 5; i++)
            {
                ContainerRuntime child = new();
                child.Width = 80;
                child.Height = 20;
                parent.AddChild(child);
            }
            parent.SetProperty(crossBound, v);
            return parent.AbsoluteWidth + "x" + parent.AbsoluteHeight;
        }

        Layout(value).ShouldBe(Layout(null));
    }

    // A plain container, fixed at 120 x 70 or growing to one 80 x 20 child (plus 10 on each axis), with one bound set.
    private static string ContainerLayout(string bound, float? value, bool growsToContent)
    {
        ContainerRuntime parent = new();
        ContainerRuntime child = new();
        child.Width = 80;
        child.Height = 20;
        parent.AddChild(child);
        if (growsToContent)
        {
            parent.WidthUnits = DimensionUnitType.RelativeToChildren;
            parent.HeightUnits = DimensionUnitType.RelativeToChildren;
            parent.Width = 10;
            parent.Height = 10;
        }
        else
        {
            parent.Width = 120;
            parent.Height = 70;
        }

        parent.SetProperty(bound, value);

        return parent.AbsoluteWidth + "x" + parent.AbsoluteHeight + " child " + child.AbsoluteWidth + "x" + child.AbsoluteHeight
            + " at " + (child.AbsoluteLeft - parent.AbsoluteLeft) + "," + (child.AbsoluteTop - parent.AbsoluteTop);
    }

    [Theory]
    [MemberData(nameof(BoundCases))]
    public void FixedSizeContainer_NonFiniteBound_LaysOutAsWithNoBound(string bound, float value)
    {
        ContainerLayout(bound, value, growsToContent: false).ShouldBe(ContainerLayout(bound, null, growsToContent: false));
    }

    [Theory]
    [MemberData(nameof(BoundCases))]
    public void RelativeToChildrenContainer_NonFiniteBound_LaysOutAsWithNoBound(string bound, float value)
    {
        ContainerLayout(bound, value, growsToContent: true).ShouldBe(ContainerLayout(bound, null, growsToContent: true));
    }

    [Fact]
    public void Container_FiniteBound_StillLimitsTheSize()
    {
        ContainerLayout("MaxWidth", 100f, growsToContent: false).ShouldNotBe(ContainerLayout("MaxWidth", null, growsToContent: false));
        ContainerLayout("MinHeight", 100f, growsToContent: true).ShouldNotBe(ContainerLayout("MinHeight", null, growsToContent: true));
    }

    private const string LongText = "a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a";

    // (width, height, wrapped line count) of a text that grows to its content, with the bound set before and after the text.
    private static (float Width, float Height, int Lines) TextLayout(float? maxWidth, bool boundFirst, bool growsToContent)
    {
        TextRuntime text = new();
        text.Width = growsToContent ? 0 : 50;
        text.WidthUnits = growsToContent ? DimensionUnitType.RelativeToChildren : DimensionUnitType.Absolute;
        if (boundFirst)
        {
            text.MaxWidth = maxWidth;
        }
        text.Text = LongText;
        if (!boundFirst)
        {
            text.MaxWidth = maxWidth;
        }
        text.UpdateLayout();
        Text inner = (Text)text.RenderableComponent!;
        return (text.AbsoluteWidth, text.AbsoluteHeight, inner.WrappedText.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Text_FiniteMaxWidth_StillWraps(bool boundFirst)
    {
        TextLayout(60, boundFirst, growsToContent: true).Lines.ShouldBeGreaterThan(TextLayout(null, boundFirst, growsToContent: true).Lines);
    }

    [Theory]
    [InlineData(float.PositiveInfinity, true, true)]
    [InlineData(float.PositiveInfinity, false, true)]
    [InlineData(float.NegativeInfinity, true, true)]
    [InlineData(float.NegativeInfinity, false, true)]
    [InlineData(float.NaN, true, true)]
    [InlineData(float.NaN, false, true)]
    [InlineData(float.PositiveInfinity, true, false)]
    [InlineData(float.NegativeInfinity, true, false)]
    [InlineData(float.NaN, true, false)]
    [InlineData(float.NegativeInfinity, false, false)]
    [InlineData(float.NaN, false, false)]
    public void Text_NonFiniteMaxWidth_WrapsAsWithNoMaxWidth(float maxWidth, bool boundFirst, bool growsToContent)
    {
        TextLayout(maxWidth, boundFirst, growsToContent).ShouldBe(TextLayout(null, boundFirst, growsToContent));
    }
}
