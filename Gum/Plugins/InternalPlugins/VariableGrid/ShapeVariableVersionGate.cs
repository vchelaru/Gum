using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Pure decision logic for hiding the fill / dropshadow / gradient variables that were added to
/// the plain <c>Circle</c> and <c>Rectangle</c> standard elements in the
/// <see cref="GumProjectSave.GumxVersions.ShapeVariableExpansion"/> (version 3) surface
/// expansion. When an older project is loaded these variables are hidden in the variable grid so
/// the tool doesn't surface variables the project's runtime won't honor. Kept free of services
/// (the caller supplies the resolved project version and root standard type name) so it can be
/// unit tested without a loaded project or selection state.
/// </summary>
internal class ShapeVariableVersionGate
{
    // Only the plain Circle / Rectangle standard elements are gated. The legacy Skia shapes
    // (ColoredCircle / RoundedRectangle / Arc) carried gradient / dropshadow / fill long before
    // v3, so they must stay visible on older projects.
    private static readonly HashSet<string> GatedStandardTypeNames = new()
    {
        "Circle",
        "Rectangle",
    };

    // Fill / dropshadow / gradient variable names added to plain Circle / Rectangle in v3
    // (StandardElementsManager.AddFillAndStrokeVariables fill section, AddDropshadowVariables,
    // AddGradientVariables). Stroke is intentionally excluded — it is the always-present surface
    // on these shapes (gated implicitly by StrokeWidth = 0, not by version). Phase 0 decision: a
    // hardcoded name list here rather than a per-variable MinProjectVersion attribute. Keep in
    // sync with GumxVersions.ShapeVariableExpansion and the StandardElementsManager helpers.
    private static readonly HashSet<string> V3OnlyVariableNames = new()
    {
        // Fill
        "IsFilled",
        "FillRed",
        "FillGreen",
        "FillBlue",
        "FillAlpha",
        // Dropshadow
        "HasDropshadow",
        "DropshadowOffsetX",
        "DropshadowOffsetY",
        "DropshadowBlur",
        "DropshadowAlpha",
        "DropshadowRed",
        "DropshadowGreen",
        "DropshadowBlue",
        // Gradient
        "UseGradient",
        "GradientType",
        "GradientX1",
        "GradientX1Units",
        "GradientY1",
        "GradientY1Units",
        "GradientX2",
        "GradientX2Units",
        "GradientY2",
        "GradientY2Units",
        "GradientInnerRadius",
        "GradientInnerRadiusUnits",
        "GradientOuterRadius",
        "GradientOuterRadiusUnits",
        // Rounded corners (Rectangle only — absorbed from the retired RoundedRectangle standard)
        "CornerRadius",
        // Issue #3617 — per-corner CornerRadius overrides, added alongside CornerRadius itself.
        "CustomRadiusTopLeft",
        "CustomRadiusTopRight",
        "CustomRadiusBottomLeft",
        "CustomRadiusBottomRight",
        // Issue #3009 — Circle/Rectangle no longer expose the standalone gradient start
        // (Red1/Green1/Blue1/Alpha1); the start is the active body color, so there is no such
        // variable to gate. Color2 (Red2/Green2/Blue2/Alpha2) remains the standalone second stop.
        "Red2",
        "Green2",
        "Blue2",
        "Alpha2",
    };

    /// <summary>
    /// Returns true when the given variable (identified by its root name) should be hidden in
    /// the variable grid because it is a v3-only fill / dropshadow / gradient variable on a plain
    /// Circle / Rectangle and the loaded project predates v3.
    /// </summary>
    public bool GetIfHiddenForProjectVersion(string rootName, string? rootStandardTypeName, int projectVersion)
    {
        if (projectVersion >= (int)GumProjectSave.GumxVersions.ShapeVariableExpansion)
        {
            return false;
        }

        if (rootStandardTypeName == null || !GatedStandardTypeNames.Contains(rootStandardTypeName))
        {
            return false;
        }

        return V3OnlyVariableNames.Contains(rootName);
    }
}
