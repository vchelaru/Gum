using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
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

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
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
        fill.FillColor = ForestGladeStyling.ActiveStyle.Colors.InputFill;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
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
        border.StrokeColor = RestBorder;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        const float halo = FocusRingInset;
        RectangleRuntime ring = new RectangleRuntime();
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
        ring.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.AccentHalo;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        Color focusBorder = ForestGladeStyling.ActiveStyle.Colors.LeafBright;

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
        _fill.FillColor = fillDisabled ? ForestGladeStyling.ActiveStyle.Colors.InputFillDisabled : ForestGladeStyling.ActiveStyle.Colors.InputFill;
        _border.StrokeColor = border;
        _focusRing.Visible = showFocusRing;
    }
}
