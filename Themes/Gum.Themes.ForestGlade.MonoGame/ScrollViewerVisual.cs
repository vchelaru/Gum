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
    // Same rationale as ListBoxVisual — tighter 8 px leaf curve so the 2 px
    // border can fully mask any content spilling past the rounded corners
    // (rectangular clip container can't follow the leaf path).
    private const float RoundedRadius = 8f;
    private const float SharpRadius = 2f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 3f;
    private const float FocusRingThickness = 3f;
    private static readonly Color RestBorder = new Color(232, 255, 117, 220);

    private static void ApplyLeafShape(RoundedRectangleRuntime r)
    {
        r.CornerRadius = SharpRadius;
        r.CustomRadiusTopLeft = SharpRadius;
        r.CustomRadiusTopRight = RoundedRadius;
        r.CustomRadiusBottomRight = SharpRadius;
        r.CustomRadiusBottomLeft = RoundedRadius;
    }

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

        // Re-anchor the vertical scroll bar to centre/PixelsFromMiddle so the
        // Height-inset distributes evenly on top and bottom (the V3 default
        // anchors top). X inset gives a gap to the body border on the right;
        // ApplyScrollBarInset reapplies the values in PreRender because V3's
        // RefreshMarginsFromScrollBarVisibility (which fires on scroll-bar-
        // visibility state changes and during init) stomps Height back to
        // -horizontalScrollBar.AbsoluteHeight.
        VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        VerticalScrollBarInstance.YOrigin = VerticalAlignment.Center;
        VerticalScrollBarInstance.Y = 0f;
        ApplyScrollBarInset();

        WireStates();
    }

    private const float ScrollBarInset = BorderThickness + 1f;

    private void ApplyScrollBarInset()
    {
        VerticalScrollBarInstance.X = -ScrollBarInset;
        VerticalScrollBarInstance.Height = -ScrollBarInset * 2f;
    }

    public override void PreRender()
    {
        base.PreRender();
        // base.PreRender() can run RefreshMarginsFromScrollBarVisibility,
        // which overwrites VerticalScrollBarInstance.Height. Reapply our
        // top/bottom inset every frame so the scroll bar keeps its margins.
        // Cheap — two property writes on values that usually don't change.
        ApplyScrollBarInset();
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
        ApplyLeafShape(fill);
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
        ApplyLeafShape(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = RestBorder;
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
        ring.CornerRadius = SharpRadius + halo;
        ring.CustomRadiusTopLeft = SharpRadius + halo;
        ring.CustomRadiusTopRight = RoundedRadius + halo;
        ring.CustomRadiusBottomRight = SharpRadius + halo;
        ring.CustomRadiusBottomLeft = RoundedRadius + halo;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = ForestGladeColors.AccentHalo;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () =>
        {
            _border.Color = RestBorder;
            _focusRing.Visible = false;
        };

        States.Focused.Apply = () =>
        {
            _border.Color = ForestGladeColors.LeafBright;
            _focusRing.Visible = true;
        };
    }
}
