using Gum.Managers;
using Shouldly;
using System.Linq;
using Xunit;

namespace GumToolUnitTests.Managers;

public class StandardElementsManagerTests : BaseTestClass
{
    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_ExposesBlendVariable(string standardElementName)
    {
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.Any(v => v.Name == "Blend").ShouldBeTrue(
            $"{standardElementName} default state should expose the Blend variable so v3 shape rendering can route through SetProperty.");
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_ExposesDropshadowVariables(string standardElementName)
    {
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.Any(v => v.Name == "HasDropshadow").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "DropshadowOffsetX").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "DropshadowOffsetY").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "DropshadowBlurX").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "DropshadowBlurY").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "DropshadowAlpha").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "DropshadowRed").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "DropshadowGreen").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "DropshadowBlue").ShouldBeTrue();
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_ExposesFillAndStrokeColorChannels(string standardElementName)
    {
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.Any(v => v.Name == "FillRed").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "FillGreen").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "FillBlue").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "FillAlpha").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "StrokeRed").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "StrokeGreen").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "StrokeBlue").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "StrokeAlpha").ShouldBeTrue();
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_ExposesGradientVariables(string standardElementName)
    {
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.Any(v => v.Name == "UseGradient").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "GradientType").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "Red1").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "Green1").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "Blue1").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "Alpha1").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "Red2").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "Green2").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "Blue2").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "Alpha2").ShouldBeTrue();
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_ExposesStrokeAndFilledVariables(string standardElementName)
    {
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.Any(v => v.Name == "IsFilled").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "StrokeWidth").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "StrokeDashLength").ShouldBeTrue();
        state.Variables.Any(v => v.Name == "StrokeGapLength").ShouldBeTrue();
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_FillColorDefaultsTransparent_PreservingHistoricalStrokeOnlyAppearance(string standardElementName)
    {
        // The plain Circle/Rectangle runtimes default FillColor to transparent (alpha 0) and
        // IsFilled to true, so the historical "outline only" visual is preserved without users
        // having to toggle IsFilled = false. The default state must match.
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.First(v => v.Name == "FillAlpha").Value.ShouldBe(0);
    }
}
