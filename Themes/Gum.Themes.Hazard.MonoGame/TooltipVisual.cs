using Gum.GueDeriving;
using BaseTooltipVisual = Gum.Forms.DefaultVisuals.V3.TooltipVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled Tooltip visual. Replaces the V3 NineSlice background with
/// the standard Hazard shell (Surface1 fill + 1 px Border stroke + corner
/// radius 2, built via <see cref="HazardShapes"/>) and applies the theme's
/// primary text color to the label. Passive overlay — no state callbacks.
/// </summary>
public class TooltipVisual : BaseTooltipVisual
{
    private const float CornerRadius = 0f;
    private const float BorderThickness = 1f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public TooltipVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // V3 order is [Background, TextInstance]. Detach the NineSlice
        // background, insert the Hazard fill + border behind the text, then
        // re-attach the text last so it renders on top.
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface1, CornerRadius);
        AddChild(_fill);

        _border = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness);
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.Color = HazardStyling.ActiveStyle.Colors.Text;
    }
}
