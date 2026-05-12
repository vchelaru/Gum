using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseScrollViewerVisual = Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade ScrollViewer. Same shell pattern as <see cref="ListBoxVisual"/>:
/// deep canopy fill + leaf-large per-corner radii + sun-pale border, with an
/// accent halo focus ring outside.
/// </summary>
public class ScrollViewerVisual : BaseScrollViewerVisual
{
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 3f;
    private const float FocusRingThickness = 3f;

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ScrollAndClipContainer.Parent = null;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        AddChild(ScrollAndClipContainer);

        _border = CreateBorder();
        AddChild(_border);

        VerticalScrollBarInstance.X = -2f;

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "ForestGladeScrollViewerFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyLarge(fill);
        fill.IsFilled = true;
        fill.Color = ForestGladePalette.InputFill;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "ForestGladeScrollViewerBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyLarge(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = new Color(232, 255, 117, 56);
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        const float halo = FocusRingInset;
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "ForestGladeScrollViewerFocusRing";
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = halo * 2f;
        ring.Height = halo * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = 4f + halo;
        ring.CustomRadiusTopLeft = 4f + halo;
        ring.CustomRadiusTopRight = 18f + halo;
        ring.CustomRadiusBottomRight = 4f + halo;
        ring.CustomRadiusBottomLeft = 18f + halo;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = ForestGladeColors.AccentHalo;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        Color restBorder = new Color(232, 255, 117, 56);

        States.Enabled.Apply = () =>
        {
            _border.Color = restBorder;
            _focusRing.Visible = false;
        };

        States.Focused.Apply = () =>
        {
            _border.Color = ForestGladeColors.LeafBright;
            _focusRing.Visible = true;
        };
    }
}
