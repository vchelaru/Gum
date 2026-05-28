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
    [InlineData("StrokeRed")]
    [InlineData("StrokeGreen")]
    [InlineData("StrokeBlue")]
    [InlineData("StrokeAlpha")]
    [InlineData("FillRed")]
    [InlineData("FillGreen")]
    [InlineData("FillBlue")]
    [InlineData("FillAlpha")]
    public void GetIfHidden_HidesV3OnlyVariables_OnOlderProject(string variableName)
    {
        _gate.GetIfHiddenForProjectVersion(variableName, OlderThanV3).ShouldBeTrue();
    }

    [Theory]
    [InlineData("StrokeRed")]
    [InlineData("FillAlpha")]
    public void GetIfHidden_KeepsV3OnlyVariables_OnV3Project(string variableName)
    {
        _gate.GetIfHiddenForProjectVersion(variableName, V3).ShouldBeFalse();
    }

    [Theory]
    // gradient / dropshadow / StrokeWidth / IsFilled predate v3 on the legacy Skia shapes, so
    // they must never be gated by version regardless of how old the project is.
    [InlineData("UseGradient")]
    [InlineData("HasDropshadow")]
    [InlineData("StrokeWidth")]
    [InlineData("StrokeDashLength")]
    [InlineData("IsFilled")]
    [InlineData("Red")]
    [InlineData("Width")]
    public void GetIfHidden_NeverHidesPreV3Variables_OnOlderProject(string variableName)
    {
        _gate.GetIfHiddenForProjectVersion(variableName, OlderThanV3).ShouldBeFalse();
    }
}
