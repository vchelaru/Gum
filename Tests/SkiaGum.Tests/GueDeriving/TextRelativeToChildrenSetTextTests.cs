using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// Setting Text to a string that measures the same keeps a RelativeToChildren text's Width offset (#7550).
/// </summary>
public class TextRelativeToChildrenSetTextTests : BaseTestClass
{
    public TextRelativeToChildrenSetTextTests()
    {
        new RenderingLibrary.SystemManagers().Initialize();
    }

    [Theory]
    [InlineData(0f, HorizontalAlignment.Left)]
    [InlineData(4f, HorizontalAlignment.Left)]
    [InlineData(4f, HorizontalAlignment.Center)]
    [InlineData(4f, HorizontalAlignment.Right)]
    [InlineData(-3f, HorizontalAlignment.Center)]
    public void Text_ShouldKeepLayout_WhenSetToSameString(float offset, HorizontalAlignment xOrigin)
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = offset;
        text.XOrigin = xOrigin;
        text.X = 50;
        text.Text = "Mason";
        float width = text.AbsoluteWidth;
        float left = text.AbsoluteLeft;

        text.Text = "Mason";

        text.AbsoluteWidth.ShouldBe(width);
        text.AbsoluteLeft.ShouldBe(left);
    }

    [Fact]
    public void SetTextNoTranslate_ShouldKeepWidthOffset_WhenSetToSameString()
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 4;
        text.SetTextNoTranslate("Mason");
        float width = text.AbsoluteWidth;

        text.SetTextNoTranslate("Mason");

        text.AbsoluteWidth.ShouldBe(width);
    }

    [Fact]
    public void Text_ShouldKeepParentSize_WhenSetToSameStringInsideRelativeToChildrenParent()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 4;
        parent.AddChild(text);
        text.Text = "Mason";
        float width = parent.AbsoluteWidth;

        text.Text = "Mason";

        parent.AbsoluteWidth.ShouldBe(width);
    }

    [Theory]
    [InlineData("Mason", "Mason Mason Mason")]
    [InlineData("Mason Mason Mason", "Mason")]
    [InlineData("Mason", "[Color=Red]Mason Mason[/Color]")]
    public void Text_ShouldResizeParentAndRaiseSizeChanged_WhenMeasuredSizeChanges(string first, string second)
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 4;
        parent.AddChild(text);
        text.Text = first;
        float parentBefore = parent.AbsoluteWidth;
        float textBefore = text.AbsoluteWidth;
        int parentSizeChanges = 0;
        parent.SizeChanged += (_, _) => parentSizeChanges++;

        text.Text = second;

        text.AbsoluteWidth.ShouldNotBe(textBefore);
        (parent.AbsoluteWidth - parentBefore).ShouldBe(text.AbsoluteWidth - textBefore, 0.01f);
        parentSizeChanges.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Text_ShouldNotRaiseParentSizeChanged_WhenSetToSameString()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 4;
        parent.AddChild(text);
        text.Text = "Mason";
        int parentSizeChanges = 0;
        parent.SizeChanged += (_, _) => parentSizeChanges++;

        text.Text = "Mason";

        parentSizeChanges.ShouldBe(0);
    }
}
