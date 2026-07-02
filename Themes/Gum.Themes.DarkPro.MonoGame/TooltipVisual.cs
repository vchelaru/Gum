using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseTooltipVisual = Gum.Forms.DefaultVisuals.V3.TooltipVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled Tooltip visual. Replaces the V3 NineSlice background with
/// the standard Dark Pro shell (Surface1 fill + 1 px Border stroke + corner
/// radius 2) and applies the theme's primary text color to the label. Passive
/// overlay — no state callbacks.
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
        // background, insert the Dark Pro fill + border behind the text, then
        // re-attach the text last so it renders on top.
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        AddChild(TextInstance);
        TextInstance.Color = DarkProStyling.ActiveStyle.Colors.Text;
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "DarkProTooltipFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = CornerRadius;
        fill.IsFilled = true;
        fill.FillColor = DarkProStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "DarkProTooltipBorder";
        border.X = 0;
        border.Y = 0;
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.CornerRadius = CornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = DarkProStyling.ActiveStyle.Colors.Border;
        return border;
    }
}
