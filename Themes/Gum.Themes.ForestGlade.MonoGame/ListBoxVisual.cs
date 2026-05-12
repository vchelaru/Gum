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
    // Leaf curve here is tighter than the rest of the theme (8 px instead of
    // leaf-large's 18 px). The border-on-top trick can only mask overflow up
    // to its own width — the rectangular clip container leaks content past the
    // rounded leaf curve at the bottom-left and top-right corners, and at
    // leaf-large radius (18 px) a 2 px border can't fully cover the gap. An
    // 8 px curve with a 2 px solid border keeps the asymmetric leaf identity
    // and stays inside what the border can mask. Skia themes don't suffer
    // this since they clip to actual rounded paths.
    private const float RoundedRadius = 8f;
    private const float SharpRadius = 2f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 3f;
    private const float FocusRingThickness = 3f;
    private static readonly Color RestBorder = new Color(232, 255, 117, 220);
    private static readonly Color HoverBorder = new Color(232, 255, 117, 240);
    private static readonly Color DisabledBorderColor = new Color(232, 255, 117, 50);

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
            // X inset on top of the thumb's existing 2 px in-bar inset gives a
            // visible gap between the thumb and the listbox border at the right
            // (~5 px). Height inset gives the same breathing room at top and
            // bottom — without it the thumb at min/max value hugs the listbox
            // border at top/bottom respectively, even though the in-bar inset
            // is there: the 2 px of in-bar inset is hidden behind the listbox's
            // 2 px border.
            float scrollBarInset = BorderThickness + 1f;
            VerticalScrollBarInstance.X = -scrollBarInset;
            VerticalScrollBarInstance.Height = -scrollBarInset * 2f;
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
        ApplyLeafShape(fill);
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
        ring.Name = "ForestGladeListBoxFocusRing";
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
        Color focusBorder = ForestGladeColors.LeafBright;

        States.Enabled.Apply = () => ApplyPalette(border: RestBorder, showFocusRing: false);
        States.Highlighted.Apply = () => ApplyPalette(border: HoverBorder, showFocusRing: false);
        States.Focused.Apply = () => ApplyPalette(border: focusBorder, showFocusRing: true);
        States.HighlightedFocused.Apply = () => ApplyPalette(border: focusBorder, showFocusRing: true);
        States.Pushed.Apply = () => ApplyPalette(border: focusBorder, showFocusRing: false);
        States.Disabled.Apply = () => ApplyPalette(border: DisabledBorderColor, showFocusRing: false, fillDisabled: true);
        States.DisabledFocused.Apply = () => ApplyPalette(border: DisabledBorderColor, showFocusRing: true, fillDisabled: true);
    }

    private void ApplyPalette(Color border, bool showFocusRing, bool fillDisabled = false)
    {
        _fill.Color = fillDisabled ? ForestGladePalette.InputFillDisabled : ForestGladePalette.InputFill;
        _border.Color = border;
        _focusRing.Visible = showFocusRing;
    }
}
