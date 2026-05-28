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
        state.Variables.Any(v => v.Name == "DropshadowBlur").ShouldBeTrue();
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
    }

    // Plain Circle / Rectangle don't support dashed strokes, so the dash/gap variables are not
    // exposed on them (they remain on the legacy ColoredCircle / RoundedRectangle / Arc).
    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_DoesNotExposeStrokeDashAndGap(string standardElementName)
    {
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.Any(v => v.Name == "StrokeDashLength").ShouldBeFalse();
        state.Variables.Any(v => v.Name == "StrokeGapLength").ShouldBeFalse();
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_IsFilledDefaultsFalse_SoOutlineOnlyAppearanceMatchesCheckbox(string standardElementName)
    {
        // The runtime keeps its #2938 ctor defaults (IsFilled = true + transparent fill) so
        // historical code-only constructions still render outline-only. The tool diverges
        // intentionally: defaulting IsFilled = false here keeps the same visual but makes the
        // checkbox honestly reflect what's drawn. Pairs with the visible fill defaults below so
        // toggling IsFilled in the variable grid produces an immediate visual change.
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.First(v => v.Name == "IsFilled").Value.ShouldBe(false);
    }

    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_FillColorDefaultsOpaqueWhite_SoTogglingIsFilledShowsImmediateChange(string standardElementName)
    {
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.First(v => v.Name == "FillAlpha").Value.ShouldBe(255);
        state.Variables.First(v => v.Name == "FillRed").Value.ShouldBe(255);
        state.Variables.First(v => v.Name == "FillGreen").Value.ShouldBe(255);
        state.Variables.First(v => v.Name == "FillBlue").Value.ShouldBe(255);
    }

    // The legacy Color / Red / Green / Blue / Alpha route to the stroke slot under #2938's
    // two-slot model, so surfacing them on plain Circle / Rectangle alongside the explicit
    // StrokeRed/Green/Blue/Alpha channels would be redundant and confusing. The runtime
    // keeps the [Obsolete] aliases so older saved projects still load.
    [Theory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void DefaultState_DoesNotExposeLegacyColorChannels(string standardElementName)
    {
        var state = StandardElementsManager.Self.DefaultStates[standardElementName];

        state.Variables.Any(v => v.Name == "Red").ShouldBeFalse();
        state.Variables.Any(v => v.Name == "Green").ShouldBeFalse();
        state.Variables.Any(v => v.Name == "Blue").ShouldBeFalse();
        state.Variables.Any(v => v.Name == "Alpha").ShouldBeFalse();
        state.Variables.Any(v => v.Name == "Color").ShouldBeFalse();
    }
}
