using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.VariableGrid;

public class ShapeVariableVersionGateTests
{
    private const int OlderThanV3 = (int)GumProjectSave.GumxVersions.AttributeVersion;
    private const int V3 = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;

    private readonly ShapeVariableVersionGate _gate = new();

    [Theory]
    // Fill
    [InlineData("IsFilled")]
    [InlineData("FillRed")]
    [InlineData("FillGreen")]
    [InlineData("FillBlue")]
    [InlineData("FillAlpha")]
    // Dropshadow
    [InlineData("HasDropshadow")]
    [InlineData("DropshadowOffsetX")]
    [InlineData("DropshadowOffsetY")]
    [InlineData("DropshadowBlur")]
    [InlineData("DropshadowAlpha")]
    [InlineData("DropshadowRed")]
    [InlineData("DropshadowGreen")]
    [InlineData("DropshadowBlue")]
    // Gradient
    [InlineData("UseGradient")]
    [InlineData("GradientType")]
    [InlineData("GradientX1")]
    [InlineData("GradientY1")]
    [InlineData("GradientX2Units")]
    [InlineData("GradientInnerRadius")]
    [InlineData("GradientOuterRadiusUnits")]
    // Issue #3009 — Circle/Rectangle no longer expose the standalone gradient start
    // (Red1/Green1/Blue1/Alpha1); the start is the active body color, so there is no such variable
    // to gate. Color2 (Red2/Green2/Blue2/Alpha2) remains the standalone second stop and is gated.
    [InlineData("Alpha2")]
    // CornerRadius (Rectangle-only v3 surface absorbed from the retired RoundedRectangle)
    [InlineData("CornerRadius")]
    public void GetIfHidden_HidesFillDropshadowGradient_OnOlderCircleAndRectangle(string variableName)
    {
        _gate.GetIfHiddenForProjectVersion(variableName, "Circle", OlderThanV3).ShouldBeTrue();
        _gate.GetIfHiddenForProjectVersion(variableName, "Rectangle", OlderThanV3).ShouldBeTrue();
    }

    [Theory]
    [InlineData("FillRed")]
    [InlineData("HasDropshadow")]
    [InlineData("UseGradient")]
    public void GetIfHidden_KeepsGatedVariables_OnV3Project(string variableName)
    {
        _gate.GetIfHiddenForProjectVersion(variableName, "Circle", V3).ShouldBeFalse();
    }

    [Theory]
    // The gate is scoped to plain Circle / Rectangle only. On the legacy Skia shapes these
    // variables predate v3, so they must stay visible even on an older project.
    [InlineData("ColoredCircle")]
    [InlineData("RoundedRectangle")]
    [InlineData("Arc")]
    public void GetIfHidden_NeverGatesLegacySkiaShapes(string rootStandardTypeName)
    {
        _gate.GetIfHiddenForProjectVersion("UseGradient", rootStandardTypeName, OlderThanV3).ShouldBeFalse();
        _gate.GetIfHiddenForProjectVersion("HasDropshadow", rootStandardTypeName, OlderThanV3).ShouldBeFalse();
        _gate.GetIfHiddenForProjectVersion("FillRed", rootStandardTypeName, OlderThanV3).ShouldBeFalse();
    }

    [Theory]
    // Stroke is the always-present surface on plain Circle / Rectangle (gated implicitly by
    // StrokeWidth = 0, not by version), so stroke variables are never version-gated.
    [InlineData("StrokeWidth")]
    [InlineData("StrokeDashLength")]
    [InlineData("StrokeGapLength")]
    [InlineData("StrokeRed")]
    [InlineData("StrokeGreen")]
    [InlineData("StrokeBlue")]
    [InlineData("StrokeAlpha")]
    public void GetIfHidden_NeverGatesStrokeVariables_OnCircle(string variableName)
    {
        _gate.GetIfHiddenForProjectVersion(variableName, "Circle", OlderThanV3).ShouldBeFalse();
    }

    [Theory]
    // Non-shape and pre-v3 variables are never gated regardless of element type.
    [InlineData("Width")]
    [InlineData("X")]
    [InlineData("Visible")]
    public void GetIfHidden_NeverGatesUnrelatedVariables(string variableName)
    {
        _gate.GetIfHiddenForProjectVersion(variableName, "Circle", OlderThanV3).ShouldBeFalse();
    }

    [Fact]
    public void GetIfHidden_HandlesNullRootStandardTypeName()
    {
        _gate.GetIfHiddenForProjectVersion("FillRed", null, OlderThanV3).ShouldBeFalse();
    }
}
