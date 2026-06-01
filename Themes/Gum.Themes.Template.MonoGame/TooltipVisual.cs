using Gum.GueDeriving;
using BaseTooltipVisual = Gum.Forms.DefaultVisuals.V3.TooltipVisual;

namespace Gum.Themes.Template;

/// <summary>
/// Template-styled Tooltip visual. Replaces the V3 NineSlice background with
/// the standard Template shell (Surface1 fill + 1 px Border stroke + corner
/// radius 2, built via <see cref="TemplateShapes"/>) and applies the theme's
/// primary text color to the label. Passive overlay — no state callbacks.
/// </summary>
public class TooltipVisual : BaseTooltipVisual
{
    private const float CornerRadius = 2f;
    private const float BorderThickness = 1f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public TooltipVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // V3 order is [Background, TextInstance]. Detach the NineSlice
        // background, insert the Template fill + border behind the text, then
        // re-attach the text last so it renders on top.
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = TemplateShapes.Fill(TemplatePalette.Surface1, CornerRadius);
        AddChild(_fill);

        _border = TemplateShapes.Border(TemplatePalette.Border, CornerRadius, BorderThickness);
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.Font = TemplateTheme.BodyFontFamily; // tooltip body uses the body face
        TextInstance.Color = TemplatePalette.Text;
    }
}
