using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.VariableGrid;

public class ShapeVariableExclusionLogicTests
{
    private readonly ShapeVariableExclusionLogic _logic = new();

    [Theory]
    [InlineData("StrokeDashLength")]
    [InlineData("StrokeGapLength")]
    public void StrokeWidthZero_HidesDashAndGap_OnPlainShape(string variableName)
    {
        var finder = MakeFinder(("StrokeWidth", 0f));

        _logic.GetIfShapeVariableIsExcluded(variableName, finder, "Circle", "", out bool shouldExclude)
            .ShouldBeTrue();
        shouldExclude.ShouldBeTrue();
    }

    [Theory]
    [InlineData("StrokeDashLength")]
    [InlineData("StrokeGapLength")]
    public void StrokeWidthZero_HidesDashAndGap_OnLegacyStrokeShape(string variableName)
    {
        // Legacy ColoredCircle in stroke mode (IsFilled = false): the IsFilled gate doesn't
        // hide stroke vars, but StrokeWidth = 0 should.
        var finder = MakeFinder(("StrokeWidth", 0f), ("IsFilled", false));

        _logic.GetIfShapeVariableIsExcluded(variableName, finder, "ColoredCircle", "", out bool shouldExclude)
            .ShouldBeTrue();
        shouldExclude.ShouldBeTrue();
    }

    [Theory]
    [InlineData("StrokeRed")]
    [InlineData("StrokeGreen")]
    [InlineData("StrokeBlue")]
    [InlineData("StrokeAlpha")]
    public void StrokeWidthZero_HidesStrokeColorChannels(string variableName)
    {
        var finder = MakeFinder(("StrokeWidth", 0f));

        _logic.GetIfShapeVariableIsExcluded(variableName, finder, "Circle", "", out bool shouldExclude)
            .ShouldBeTrue();
        shouldExclude.ShouldBeTrue();
    }

    [Fact]
    public void StrokeWidthZero_KeepsStrokeWidthItselfVisible()
    {
        var finder = MakeFinder(("StrokeWidth", 0f));

        _logic.GetIfShapeVariableIsExcluded("StrokeWidth", finder, "Circle", "", out bool shouldExclude);
        shouldExclude.ShouldBeFalse();
    }

    [Theory]
    [InlineData("StrokeDashLength")]
    [InlineData("StrokeGapLength")]
    [InlineData("StrokeRed")]
    [InlineData("StrokeGreen")]
    [InlineData("StrokeBlue")]
    [InlineData("StrokeAlpha")]
    public void StrokeWidthPositive_ShowsStrokeChannels(string variableName)
    {
        var finder = MakeFinder(("StrokeWidth", 2f));

        _logic.GetIfShapeVariableIsExcluded(variableName, finder, "Circle", "", out bool shouldExclude);
        shouldExclude.ShouldBeFalse();
    }

    // Issue #3009 — Arc's gradient start is its primary Color; the Red1/Green1/Blue1/Alpha1 surface
    // is kept only as obsolete back-compat shims, so it is always hidden from Arc's grid (even with
    // a gradient on), leaving Arc a single primary Color.
    [Theory]
    [InlineData("Red1")]
    [InlineData("Green1")]
    [InlineData("Blue1")]
    [InlineData("Alpha1")]
    public void Arc_AlwaysHidesGradientStartChannels(string variableName)
    {
        var finder = MakeFinder(("UseGradient", true));

        _logic.GetIfShapeVariableIsExcluded(variableName, finder, "Arc", "", out bool shouldExclude)
            .ShouldBeTrue();
        shouldExclude.ShouldBeTrue();
    }

    // The legacy ColoredCircle / RoundedRectangle keep their standalone Color1 (unchanged by
    // #3009): the gradient start channels stay visible when a gradient is on.
    [Theory]
    [InlineData("ColoredCircle")]
    [InlineData("RoundedRectangle")]
    public void LegacyShapes_ShowGradientStartChannels_WhenGradientOn(string standardType)
    {
        var finder = MakeFinder(("UseGradient", true));

        _logic.GetIfShapeVariableIsExcluded("Red1", finder, standardType, "", out bool shouldExclude);
        shouldExclude.ShouldBeFalse();
    }

    private static RecursiveVariableFinder MakeFinder(params (string name, object value)[] variables)
    {
        var element = new ComponentSave { Name = "Test" };
        var state = new StateSave { Name = "Default", ParentContainer = element };
        foreach (var (name, value) in variables)
        {
            state.Variables.Add(new VariableSave { Name = name, Value = value, SetsValue = true });
        }
        return new RecursiveVariableFinder(state);
    }
}
