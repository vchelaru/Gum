using Gum.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Pure decision logic for warning the user that Text's <c>HasDropshadow</c> variable is inert
/// under bmfont.exe — only KernSmith can bake a shadow into the font atlas. Kept free of services
/// so it can be unit tested without a loaded project or selection state.
/// </summary>
/// <remarks>
/// Public (unlike the sibling <see cref="ShapeVariableVersionGate"/>) because its consumer,
/// <c>PropertyGridManager</c>, lives in the separate <c>Gum</c> assembly rather than here in
/// <c>Gum.Presentation</c>.
/// </remarks>
public class TextDropshadowFontGeneratorWarningLogic
{
    private const string WarningText =
        "bmfont cannot bake dropshadows into the font atlas. Switch to KernSmith in Project Properties.";

    /// <summary>
    /// Returns the warning text to show under Text's HasDropshadow variable when the project's
    /// FontGenerator can't honor it, or null when no warning applies.
    /// </summary>
    public string? GetWarningIfApplicable(string? rootStandardTypeName, FontGeneratorType fontGenerator)
    {
        if (rootStandardTypeName != "Text")
        {
            return null;
        }

        if (fontGenerator != FontGeneratorType.BmFont)
        {
            return null;
        }

        return WarningText;
    }
}
