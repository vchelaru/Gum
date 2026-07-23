using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System.Linq;
using WpfDataUi.Controls;
using Xunit;

namespace GumToolUnitTests.Managers;

public class StandardElementsManagerTests : BaseTestClass
{
    private static StandardElementsManagerGumTool CreateSut(ISelectedState? selectedState = null) =>
        new(Mock.Of<IPluginManager>(), selectedState ?? Mock.Of<ISelectedState>());

    // FillAlpha / StrokeAlpha / DropshadowAlpha are 0-255 byte channels just like the legacy
    // Alpha, so they should get the same SliderDisplay with a [0, 255] range rather than a
    // plain int textbox.
    [Theory]
    [InlineData("FillAlpha")]
    [InlineData("StrokeAlpha")]
    [InlineData("DropshadowAlpha")]
    public void SetPreferredDisplayers_AssignsByteRangeSlider_ToAlphaVariables(string variableName)
    {
        var state = new StateSave();
        state.Variables.Add(new VariableSave { Type = "int", Name = variableName });

        CreateSut().SetPreferredDisplayers(state);

        var variable = state.Variables.First();
        variable.PreferredDisplayer.ShouldBe(typeof(SliderDisplay));
        variable.PropertiesToSetOnDisplayer["MinValue"].ShouldBe(0.0);
        variable.PropertiesToSetOnDisplayer["MaxValue"].ShouldBe(255.0);
    }

    // StrokeWidth has a meaningful floor of 0 but no natural maximum, so it stays a plain numeric
    // textbox (no SliderDisplay, which would require an arbitrary max) and only gets a MinValue.
    [Fact]
    public void SetPreferredDisplayers_ClampsStrokeWidthToMinZero_AsPlainTextBox()
    {
        var state = new StateSave();
        state.Variables.Add(new VariableSave { Type = "float", Name = "StrokeWidth" });

        CreateSut().SetPreferredDisplayers(state);

        var variable = state.Variables.First();
        variable.PreferredDisplayer.ShouldBeNull();
        variable.PropertiesToSetOnDisplayer["MinValue"].ShouldBe(0.0);
        variable.PropertiesToSetOnDisplayer.ContainsKey("MaxValue").ShouldBeFalse();
    }

    // Pins the static->instance drain: the Parent variable's type converter is built from the
    // injected ISelectedState. Before the drain SetPreferredDisplayers pulled ISelectedState from
    // the static Locator (which throws in a unit test); now it must come from the constructor.
    [Fact]
    public void SetPreferredDisplayers_AssignsAvailableParentsConverter_FromInjectedSelectedState()
    {
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Type = "string", Name = "Parent" });

        CreateSut().SetPreferredDisplayers(state);

        VariableSave variable = state.Variables.First();
        variable.CustomTypeConverter.ShouldBeOfType<AvailableParentsTypeConverter>();
        variable.PropertiesToSetOnDisplayer["IsEditable"].ShouldBe(true);
    }

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
        // Issue #3009 — Circle/Rectangle no longer expose the standalone gradient start
        // (Red1/Green1/Blue1/Alpha1); the gradient start is the active body color (FillColor when
        // filled, StrokeColor otherwise). Color2 (the standalone second stop) remains.
        state.Variables.Any(v => v.Name == "Red1").ShouldBeFalse();
        state.Variables.Any(v => v.Name == "Green1").ShouldBeFalse();
        state.Variables.Any(v => v.Name == "Blue1").ShouldBeFalse();
        state.Variables.Any(v => v.Name == "Alpha1").ShouldBeFalse();
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
