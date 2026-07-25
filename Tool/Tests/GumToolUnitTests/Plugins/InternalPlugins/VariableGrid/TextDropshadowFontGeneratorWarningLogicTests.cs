using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.VariableGrid;

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
