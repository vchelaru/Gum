using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Pure decision logic for hiding shape variables that did not exist before the
/// <see cref="GumProjectSave.GumxVersions.ShapeVariableExpansion"/> (version 3) variable
/// surface expansion. When an older project is loaded these variables are hidden in the
/// variable grid so the tool doesn't surface variables the project's runtime won't honor.
/// Kept free of services (the caller supplies the resolved project version) so it can be
/// unit tested without a loaded project or selection state.
/// </summary>
internal class ShapeVariableVersionGate
{
    // Hardcoded list of v3-only variable names. These are the channel-decomposed stroke and
    // fill color variables introduced by #2931/#2943 (AddFillAndStrokeVariables). They are the
    // only shape variable names that are strictly new in version 3 — gradient / dropshadow /
    // StrokeWidth / StrokeDashLength / IsFilled all predate v3 on the legacy Skia shapes
    // (ColoredCircle / RoundedRectangle / Arc), so gating those by name would wrongly hide them
    // on older projects. Phase 0 decision: a hardcoded name list here rather than a per-variable
    // MinProjectVersion attribute. Keep in sync with GumxVersions.ShapeVariableExpansion.
    private static readonly HashSet<string> V3OnlyVariableNames = new()
    {
        "StrokeRed",
        "StrokeGreen",
        "StrokeBlue",
        "StrokeAlpha",
        "FillRed",
        "FillGreen",
        "FillBlue",
        "FillAlpha",
    };

    /// <summary>
    /// Returns true when the given variable (identified by its root name) should be hidden in
    /// the variable grid because it is a v3-only variable and the loaded project predates v3.
    /// </summary>
    public bool GetIfHiddenForProjectVersion(string rootName, int projectVersion)
    {
        return projectVersion < (int)GumProjectSave.GumxVersions.ShapeVariableExpansion
            && V3OnlyVariableNames.Contains(rootName);
    }
}
