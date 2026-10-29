using Gum.DataTypes;
using Gum.GueDeriving;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #6827: SystemManagers.GlobalFontScale must scale text the same way it does on MonoGame.
/// </summary>
public class TextGlobalFontScaleTests : BaseTestClass
{
    public TextGlobalFontScaleTests()
    {
        new RenderingLibrary.SystemManagers().Initialize();
    }

    static TextRuntime CreateText(string text)
    {
        TextRuntime sut = new();
        sut.WidthUnits = DimensionUnitType.RelativeToChildren;
        sut.HeightUnits = DimensionUnitType.RelativeToChildren;
        sut.Width = 0;
        sut.Height = 0;
        sut.Text = text;
        return sut;
    }

    [Fact]
    public void SystemManagersGlobalFontScale_Two_ShouldDoubleTextSize()
    {
        float saved = RenderingLibrary.SystemManagers.GlobalFontScale;
        try
        {
            RenderingLibrary.SystemManagers.GlobalFontScale = 1;
            TextRuntime atOne = CreateText("Hello");

            RenderingLibrary.SystemManagers.GlobalFontScale = 2;
            TextRuntime atTwo = CreateText("Hello");

            atTwo.AbsoluteWidth.ShouldBe(2 * atOne.AbsoluteWidth, 2);
            atTwo.AbsoluteHeight.ShouldBe(2 * atOne.AbsoluteHeight, 2);
        }
        finally
        {
            RenderingLibrary.SystemManagers.GlobalFontScale = saved;
        }
    }
}
