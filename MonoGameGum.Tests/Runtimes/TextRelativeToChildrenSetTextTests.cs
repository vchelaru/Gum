using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class TextRelativeToChildrenSetTextTests : BaseTestClass
{
    [Fact]
    public void Text_ShouldKeepWidthOffset_WhenSetToSameString()
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 4;
        text.Text = "Mason";
        float before = text.AbsoluteWidth;

        text.Text = "Mason";

        text.AbsoluteWidth.ShouldBe(before);
    }

    [Fact]
    public void Text_ShouldNotMoveCenteredText_WhenSetToSameString()
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 4;
        text.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        text.X = 100;
        text.Text = "Mason";
        float left = text.AbsoluteLeft;

        text.Text = "Mason";

        text.AbsoluteLeft.ShouldBe(left);
    }
}
