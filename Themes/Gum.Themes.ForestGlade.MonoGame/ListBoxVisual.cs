using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled ListBox visual. Deep canopy fill with leaf-large
/// per-corner radii, sun-pale tinted border, accent focus ring outside the
/// body.
/// </summary>
public class ListBoxVisual : BaseListBoxVisual
{
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 3f;
    private const float FocusRingThickness = 3f;

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ClipAndScrollContainer.Parent = null;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        // ClipAndScrollContainer goes between fill and border. The
        // rectangular clip would let row hover/selection fills poke past
        // the rounded leaf outline at the corners; painting the border on
        // top masks them.
        AddChild(ClipAndScrollContainer);

        _border = CreateBorder();
        AddChild(_border);

        if (VerticalScrollBarInstance != null)
        {
            VerticalScrollBarInstance.X = -2f;
        }

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "ForestGladeListBoxFill";
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
        border.Name = "ForestGladeListBoxBorder";
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
        ring.Name = "ForestGladeListBoxFocusRing";
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
        Color hoverBorder = new Color(232, 255, 117, 115);
        Color focusBorder = ForestGladeColors.LeafBright;
        Color disabledBorder = new Color(232, 255, 117, 26);

        States.Enabled.Apply = () => ApplyPalette(border: restBorder, showFocusRing: false);
        States.Highlighted.Apply = () => ApplyPalette(border: hoverBorder, showFocusRing: false);
        States.Focused.Apply = () => ApplyPalette(border: focusBorder, showFocusRing: true);
        States.HighlightedFocused.Apply = () => ApplyPalette(border: focusBorder, showFocusRing: true);
        States.Pushed.Apply = () => ApplyPalette(border: focusBorder, showFocusRing: false);
        States.Disabled.Apply = () => ApplyPalette(border: disabledBorder, showFocusRing: false, fillDisabled: true);
        States.DisabledFocused.Apply = () => ApplyPalette(border: disabledBorder, showFocusRing: true, fillDisabled: true);
    }

    private void ApplyPalette(Color border, bool showFocusRing, bool fillDisabled = false)
    {
        _fill.Color = fillDisabled ? ForestGladePalette.InputFillDisabled : ForestGladePalette.InputFill;
        _border.Color = border;
        _focusRing.Visible = showFocusRing;
    }
}
