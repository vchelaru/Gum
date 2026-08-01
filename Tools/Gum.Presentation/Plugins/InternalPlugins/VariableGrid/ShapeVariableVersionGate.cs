using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Pure decision logic for hiding standard-element variables that were added to a project version
/// later than the one currently loaded (e.g. the fill / dropshadow / gradient variables added to
/// the plain <c>Circle</c> and <c>Rectangle</c> standard elements in the
/// <see cref="GumProjectSave.GumxVersions.ShapeVariableExpansion"/> (version 3) surface
/// expansion, or Text's <c>LocalizeText</c> variable added in version 4). When an older project is
/// loaded these variables are hidden in the variable grid so the tool doesn't surface variables the
/// project's runtime won't honor. Kept free of services (the caller supplies the resolved project
/// version and root standard type name) so it can be unit tested without a loaded project or
/// selection state.
/// </summary>
internal class ShapeVariableVersionGate
{
    // Only the plain Circle / Rectangle standard elements are gated. The legacy Skia shapes
    // (ColoredCircle / RoundedRectangle / Arc) carried gradient / dropshadow / fill long before
    // v3, so they must stay visible on older projects. Text's dropshadow surface (issue #4005) and
    // LocalizeText (issue #4222) are gated here too — the fill/gradient names in the map below
    // don't apply to Text, but the per-variable lookup still only matches Text's own gated names.
    private static readonly HashSet<string> GatedStandardTypeNames = new()
    {
        "Circle",
        "Rectangle",
        "Text",
    };

    // Gated variable names mapped to the minimum project version that unlocks them. Most are the
    // fill / dropshadow / gradient variables added to plain Circle / Rectangle in v3
    // (StandardElementsManager.AddFillAndStrokeVariables fill section, AddDropshadowVariables,
    // AddGradientVariables); LocalizeText (issue #4222) is the first entry gated at a different
    // version, which is why this is a per-variable map rather than a single global cutoff. Stroke
    // is intentionally excluded — it is the always-present surface on these shapes (gated
    // implicitly by StrokeWidth = 0, not by version). Phase 0 decision: a hardcoded name list here
    // rather than reading VariableSave.MinimumGumxVersion directly. Keep in sync with the
    // StandardElementsManager helpers and their MinimumGumxVersion tags.
    private static readonly Dictionary<string, int> GatedVariableMinimumVersions = BuildGatedVariableMinimumVersions();

    private static Dictionary<string, int> BuildGatedVariableMinimumVersions()
    {
        int v3 = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;

        return new Dictionary<string, int>
        {
            // Fill
            ["IsFilled"] = v3,
            ["FillRed"] = v3,
            ["FillGreen"] = v3,
            ["FillBlue"] = v3,
            ["FillAlpha"] = v3,
            // Dropshadow
            ["HasDropshadow"] = v3,
            ["DropshadowOffsetX"] = v3,
            ["DropshadowOffsetY"] = v3,
            ["DropshadowBlur"] = v3,
            ["DropshadowAlpha"] = v3,
            ["DropshadowRed"] = v3,
            ["DropshadowGreen"] = v3,
            ["DropshadowBlue"] = v3,
            // Gradient
            ["UseGradient"] = v3,
            ["GradientType"] = v3,
            ["GradientX1"] = v3,
            ["GradientX1Units"] = v3,
            ["GradientY1"] = v3,
            ["GradientY1Units"] = v3,
            ["GradientX2"] = v3,
            ["GradientX2Units"] = v3,
            ["GradientY2"] = v3,
            ["GradientY2Units"] = v3,
            ["GradientInnerRadius"] = v3,
            ["GradientInnerRadiusUnits"] = v3,
            ["GradientOuterRadius"] = v3,
            ["GradientOuterRadiusUnits"] = v3,
            // Rounded corners (Rectangle only — absorbed from the retired RoundedRectangle standard)
            ["CornerRadius"] = v3,
            // Issue #3617 — per-corner CornerRadius overrides, added alongside CornerRadius itself.
            ["CustomRadiusTopLeft"] = v3,
            ["CustomRadiusTopRight"] = v3,
            ["CustomRadiusBottomLeft"] = v3,
            ["CustomRadiusBottomRight"] = v3,
            // Issue #3009 — Circle/Rectangle no longer expose the standalone gradient start
            // (Red1/Green1/Blue1/Alpha1); the start is the active body color, so there is no such
            // variable to gate. Color2 (Red2/Green2/Blue2/Alpha2) remains the standalone second stop.
            ["Red2"] = v3,
            ["Green2"] = v3,
            ["Blue2"] = v3,
            ["Alpha2"] = v3,
            // Issue #4222 — Text's LocalizeText variable, gated at v4 (a version past the rest of
            // this list, which is why a per-variable map is needed instead of one cutoff).
            ["LocalizeText"] = (int)GumProjectSave.GumxVersions.LocalizeTextExpansion,
        };
    }

    /// <summary>
    /// Returns true when the given variable (identified by its root name) should be hidden in the
    /// variable grid because the loaded project predates the version that unlocked it.
    /// </summary>
    public bool GetIfHiddenForProjectVersion(string rootName, string? rootStandardTypeName, int projectVersion)
    {
        if (rootStandardTypeName == null || !GatedStandardTypeNames.Contains(rootStandardTypeName))
        {
            return false;
        }

        if (!GatedVariableMinimumVersions.TryGetValue(rootName, out int minimumVersion))
        {
            return false;
        }

        return projectVersion < minimumVersion;
    }
}
