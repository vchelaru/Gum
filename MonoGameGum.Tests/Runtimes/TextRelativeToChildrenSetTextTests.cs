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

    [Fact]
    public void Text_ShouldRestoreRenderableWidth_WhenAssignmentThrows()
    {
        Moq.Mock<Gum.Localization.ILocalizationService> localization = new();
        localization.Setup(x => x.Translate("Boom")).Throws<System.InvalidOperationException>();
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 4;
        text.Text = "Mason";
        float? renderableWidth = ((Text)text.RenderableComponent).Width;
        Gum.Wireframe.CustomSetPropertyOnRenderable.LocalizationService = localization.Object;

        Should.Throw<System.InvalidOperationException>(() => text.Text = "Boom");

        ((Text)text.RenderableComponent).Width.ShouldBe(renderableWidth);
    }
}
