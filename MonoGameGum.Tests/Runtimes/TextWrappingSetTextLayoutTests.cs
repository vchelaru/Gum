using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Text that wraps (a width cap or a fixed width) keeps its wrapped layout when its string is set again, and
/// follows a Forms control's own text assignment.
/// </summary>
public class TextWrappingSetTextLayoutTests : BaseTestClass
{
    const string LongText = "Mason went to the market to buy some bread and cheese";

    public static IEnumerable<object[]> WrapCases()
    {
        float?[] maxWidths = { 40, 80, 120 };
        float[] offsets = { 0, 4 };
        VerticalAlignment[] yOrigins = { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom };
        foreach (float? max in maxWidths)
        {
            foreach (float offset in offsets)
            {
                foreach (VerticalAlignment y in yOrigins)
                {
                    yield return new object[] { max, offset, y };
                }
            }
        }
    }

    static TextRuntime BuildCapped(float? maxWidth, float offset, VerticalAlignment yOrigin)
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = offset;
        text.MaxWidth = maxWidth;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.YOrigin = yOrigin;
        text.Y = 50;
        return text;
    }

    [Theory]
    [MemberData(nameof(WrapCases))]
    public void Text_ShouldKeepWrappedLayout_WhenSetToSameString(float? maxWidth, float offset, VerticalAlignment yOrigin)
    {
        TextRuntime text = BuildCapped(maxWidth, offset, yOrigin);
        text.Text = LongText;
        float width = text.AbsoluteWidth;
        float height = text.AbsoluteHeight;
        float top = text.AbsoluteTop;

        text.Text = LongText;

        text.AbsoluteWidth.ShouldBe(width);
        text.AbsoluteHeight.ShouldBe(height);
        text.AbsoluteTop.ShouldBe(top);
    }

    [Theory]
    [MemberData(nameof(WrapCases))]
    public void Text_ShouldMatchFreshLayout_WhenSetShortThenLong(float? maxWidth, float offset, VerticalAlignment yOrigin)
    {
        TextRuntime text = BuildCapped(maxWidth, offset, yOrigin);
        text.Text = "Hi";
        text.Text = LongText;
        text.Text = LongText;

        TextRuntime fresh = BuildCapped(maxWidth, offset, yOrigin);
        fresh.Text = LongText;

        text.AbsoluteWidth.ShouldBe(fresh.AbsoluteWidth);
        text.AbsoluteHeight.ShouldBe(fresh.AbsoluteHeight);
        text.AbsoluteTop.ShouldBe(fresh.AbsoluteTop);
    }

    [Theory]
    [InlineData(40f)]
    [InlineData(100f)]
    public void Text_ShouldKeepWrappedHeight_WhenWidthIsFixedAndSetToSameString(float width)
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.Absolute;
        text.Width = width;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.Text = LongText;
        float height = text.AbsoluteHeight;

        text.Text = LongText;

        text.AbsoluteWidth.ShouldBe(width);
        text.AbsoluteHeight.ShouldBe(height);
    }

    [Fact]
    public void Label_ShouldKeepSize_WhenTextIsSetToSameString()
    {
        Label label = new();
        label.Text = "Mason";
        float width = label.Visual.GetAbsoluteWidth();
        float height = label.Visual.GetAbsoluteHeight();

        label.Text = "Mason";

        label.Visual.GetAbsoluteWidth().ShouldBe(width);
        label.Visual.GetAbsoluteHeight().ShouldBe(height);
    }

    [Fact]
    public void Button_ShouldKeepTextSize_WhenTextIsSetToSameString()
    {
        Button button = new();
        button.Text = "Mason";
        TextRuntime textVisual = (TextRuntime)button.Visual.GetChildByName("TextInstance")!;
        float width = textVisual.AbsoluteWidth;

        button.Text = "Mason";

        textVisual.AbsoluteWidth.ShouldBe(width);
    }

    [Fact]
    public void TextBox_ShouldKeepVisualSize_WhenTextIsSetToSameString()
    {
        TextBox textBox = new();
        textBox.Text = "Mason";
        float width = textBox.Visual.GetAbsoluteWidth();

        textBox.Text = "Mason";

        textBox.Visual.GetAbsoluteWidth().ShouldBe(width);
    }
}
