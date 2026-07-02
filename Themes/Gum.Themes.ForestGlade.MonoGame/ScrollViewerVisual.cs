using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
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

    private static void ApplyLeafShape(RectangleRuntime r)
    {
        r.CornerRadius = SharpRadius;
        r.CustomRadiusTopLeft = SharpRadius;
        r.CustomRadiusTopRight = RoundedRadius;
        r.CustomRadiusBottomRight = SharpRadius;
        r.CustomRadiusBottomLeft = RoundedRadius;
    }

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

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

        // Inset ScrollAndClipContainer rather than the scroll bar itself.
        // V3 stomps VerticalScrollBarInstance's Y/Height every state apply
        // and there is no per-frame hook on the wrapper (GraphicalUiElement.
        // PreRender is only invoked on the contained renderable, which is
        // an InvisibleRenderable for the ScrollViewer root). Shrinking the
        // container is one ctor-time edit that V3 doesn't fight: every
        // child (scroll bar + clip container) inherits the inset.
        const float Inset = BorderThickness + 1f;
        ScrollAndClipContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
        ScrollAndClipContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        ScrollAndClipContainer.XOrigin = HorizontalAlignment.Center;
        ScrollAndClipContainer.YOrigin = VerticalAlignment.Center;
        ScrollAndClipContainer.X = 0f;
        ScrollAndClipContainer.Y = 0f;
        ScrollAndClipContainer.Width = -Inset * 2f;
        ScrollAndClipContainer.Height = -Inset * 2f;
        ScrollAndClipContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        ScrollAndClipContainer.HeightUnits = DimensionUnitType.RelativeToParent;

        AddChild(ScrollAndClipContainer);

        _border = CreateBorder();
        AddChild(_border);

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
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
        fill.FillColor = ForestGladeStyling.ActiveStyle.Colors.InputFill;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
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
        border.StrokeColor = RestBorder;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        const float halo = FocusRingInset;
        RectangleRuntime ring = new RectangleRuntime();
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
        ring.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.AccentHalo;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () =>
        {
            _border.StrokeColor = RestBorder;
            _focusRing.Visible = false;
        };

        States.Focused.Apply = () =>
        {
            _border.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.LeafBright;
            _focusRing.Visible = true;
        };
    }
}
