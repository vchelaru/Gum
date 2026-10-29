using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.VariableGrid;

public class TextDropshadowFontGeneratorWarningLogicTests
{
    private readonly TextDropshadowFontGeneratorWarningLogic _sut = new();

    [Fact]
    public void GetWarningIfApplicable_ReturnsWarning_ForTextWithBmFont()
    {
        _sut.GetWarningIfApplicable("Text", FontGeneratorType.BmFont).ShouldNotBeNull();
    }

    [Fact]
    public void GetWarningIfApplicable_ReturnsNull_ForTextWithKernSmith()
    {
        _sut.GetWarningIfApplicable("Text", FontGeneratorType.KernSmith).ShouldBeNull();
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    [InlineData(null)]
    public void GetWarningIfApplicable_ReturnsNull_ForNonTextElements(string? rootStandardTypeName)
    {
        _sut.GetWarningIfApplicable(rootStandardTypeName, FontGeneratorType.BmFont).ShouldBeNull();
    }
}

public class FontGeneratorWarningTextTests
{
    [Fact]
    public void Warnings_PointAtSettingsFontsPage_NotTheRemovedFontSettingsMenu()
    {
        string[] warnings =
        [
            FontGeneratorWarningText.CannotGenerateFromFontFiles,
            FontGeneratorWarningText.CannotBakeDropshadows,
            new TextDropshadowFontGeneratorWarningLogic().GetWarningIfApplicable("Text", FontGeneratorType.BmFont)!,
        ];

        foreach (string warning in warnings)
        {
            warning.ShouldContain("Edit > Settings... > Fonts");
            warning.ShouldNotContain("Font Settings");
        }
    }
}
