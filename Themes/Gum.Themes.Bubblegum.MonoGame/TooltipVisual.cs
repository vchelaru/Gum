using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseTooltipVisual = Gum.Forms.DefaultVisuals.V3.TooltipVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled Tooltip visual. Surface1 fill + 2 px pink border at
/// CornerRadius=8. Passive overlay — no state callbacks.
/// </summary>
public class TooltipVisual : BaseTooltipVisual
{
    private const float CornerRadius = 8f;
    private const float BorderThickness = 2f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public TooltipVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = BubblegumShapes.Fill(
            color: BubblegumStyling.ActiveStyle.Colors.Surface1,
            cornerRadius: CornerRadius,
            name: "BubblegumTooltipFill");
        AddChild(_fill);

        _border = BubblegumShapes.Border(
            color: BubblegumStyling.ActiveStyle.Colors.Border,
            cornerRadius: CornerRadius,
            thickness: BorderThickness,
            name: "BubblegumTooltipBorder");
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.Color = BubblegumStyling.ActiveStyle.Colors.Text;
    }
}
