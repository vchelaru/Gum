using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A RelativeToChildren container follows its text's size exactly: it grows and shrinks when the text does,
/// and keeps its size when the text measures the same.
/// </summary>
public class TextChangeParentRemeasureTests : BaseTestClass
{
    public static IEnumerable<object[]> Cases()
    {
        ChildrenLayout[] layouts = { ChildrenLayout.Regular, ChildrenLayout.TopToBottomStack, ChildrenLayout.LeftToRightStack };
        DimensionUnitType[] textWidthUnits = { DimensionUnitType.RelativeToChildren, DimensionUnitType.Absolute };
        float[] offsets = { 0, 4, 10 };
        foreach (ChildrenLayout layout in layouts)
        {
            foreach (DimensionUnitType units in textWidthUnits)
            {
                foreach (float offset in offsets)
                {
                    yield return new object[] { layout, units, offset };
                }
            }
        }
    }

    static (TextRuntime text, ContainerRuntime parent) Build(ChildrenLayout layout, DimensionUnitType units, float width)
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        parent.ChildrenLayout = layout;

        TextRuntime text = new();
        text.WidthUnits = units;
        text.Width = width;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        parent.AddChild(text);
        return (text, parent);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Text_ShouldGrowParent_ByTextWidthDifference(ChildrenLayout layout, DimensionUnitType units, float width)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(layout, units, width);
        text.Text = "Mason";
        float parentBefore = parent.AbsoluteWidth;
        float textBefore = text.AbsoluteWidth;

        text.Text = "Mason Mason Mason";

        float textGrowth = text.AbsoluteWidth - textBefore;
        if (units == DimensionUnitType.RelativeToChildren)
        {
            textGrowth.ShouldBeGreaterThan(0);
        }
        (parent.AbsoluteWidth - parentBefore).ShouldBe(textGrowth, 0.01f);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Text_ShouldShrinkParent_WhenTextGetsShorter(ChildrenLayout layout, DimensionUnitType units, float width)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(layout, units, width);
        text.Text = "Mason Mason Mason";
        float parentBefore = parent.AbsoluteWidth;
        float textBefore = text.AbsoluteWidth;

        text.Text = "Mason";

        (parent.AbsoluteWidth - parentBefore).ShouldBe(text.AbsoluteWidth - textBefore, 0.01f);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Text_ShouldKeepParentSize_WhenSetToSameString(ChildrenLayout layout, DimensionUnitType units, float width)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(layout, units, width);
        text.Text = "Mason";
        float parentWidth = parent.AbsoluteWidth;
        float parentHeight = parent.AbsoluteHeight;

        text.Text = "Mason";

        parent.AbsoluteWidth.ShouldBe(parentWidth);
        parent.AbsoluteHeight.ShouldBe(parentHeight);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Text_ShouldGrowParentHeight_WhenTextGainsLine(ChildrenLayout layout, DimensionUnitType units, float width)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(layout, units, width);
        text.Text = "Mason";
        float parentBefore = parent.AbsoluteHeight;

        text.Text = "Mason\nMason";

        parent.AbsoluteHeight.ShouldBeGreaterThan(parentBefore);
        parent.AbsoluteHeight.ShouldBe(text.AbsoluteHeight, 0.01f);
    }

    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void Text_ShouldMoveFollowingSibling_WhenTextSizeChanges(ChildrenLayout layout)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(layout, DimensionUnitType.RelativeToChildren, 4);
        TextRuntime next = new();
        next.Text = "Next";
        parent.AddChild(next);
        text.Text = "Mason";
        float nextBefore = layout == ChildrenLayout.TopToBottomStack ? next.AbsoluteTop : next.AbsoluteLeft;

        text.Text = layout == ChildrenLayout.TopToBottomStack ? "Mason\nMason" : "Mason Mason";

        float nextAfter = layout == ChildrenLayout.TopToBottomStack ? next.AbsoluteTop : next.AbsoluteLeft;
        nextAfter.ShouldBeGreaterThan(nextBefore);
    }

    [Theory]
    [InlineData(ChildrenLayout.TopToBottomStack)]
    [InlineData(ChildrenLayout.LeftToRightStack)]
    public void Text_ShouldNotMoveFollowingSibling_WhenSetToSameString(ChildrenLayout layout)
    {
        (TextRuntime text, ContainerRuntime parent) = Build(layout, DimensionUnitType.RelativeToChildren, 4);
        TextRuntime next = new();
        next.Text = "Next";
        parent.AddChild(next);
        text.Text = "Mason";
        float nextLeft = next.AbsoluteLeft;
        float nextTop = next.AbsoluteTop;

        text.Text = "Mason";

        next.AbsoluteLeft.ShouldBe(nextLeft);
        next.AbsoluteTop.ShouldBe(nextTop);
    }
}
