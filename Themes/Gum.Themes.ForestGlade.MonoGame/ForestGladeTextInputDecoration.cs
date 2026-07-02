using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3
/// TextBoxVisual and PasswordBoxVisual) with the Forest Glade shape stack:
/// glassy dark fill with leaf-medium per-corner radii, sun-pale tinted
/// border that brightens on hover, leaf-bright border + accent-halo focus
/// ring + Gaussian glow on focus. Shared by
/// <see cref="TextBoxVisual"/> and <see cref="PasswordBoxVisual"/>.
/// </summary>
internal sealed class ForestGladeTextInputDecoration
{
    private const float BorderThickness = 1f;
    private const float FocusHaloThickness = 3f;
    private const float FocusGlowBlur = 14f;

    private readonly RectangleRuntime _focusHalo;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ForestGladeTextInputDecoration(TextBoxBaseVisual host)
    {
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        // Focus halo sits OUTSIDE the body — added first so the body paints
        // on top of it where they overlap (only the corners and ring stroke
        // extend past the body).
        _focusHalo = CreateFocusHalo();
        host.AddChild(_focusHalo);

        _fill = CreateFill();
        host.AddChild(_fill);

        // Re-attach ClipContainer between fill and border so text /
        // placeholder / caret / selection render above the fill, but the
        // border draws ON TOP.
        host.AddChild(host.ClipContainer);

        _border = CreateBorder();
        host.AddChild(_border);

        WireStates(host);
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "ForestGladeInputFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyMedium(fill);
        fill.IsFilled = true;
        fill.StrokeWidth = 0;
        // CSS .fg-input: linear-gradient(180deg, rgba(0,0,0,.30), rgba(0,0,0,.18))
        // over the canopy background — translates to a vertical gradient on
        // the fill with a slightly darker top blending into a lighter base.
        fill.UseGradient = true;
        fill.GradientType = GradientType.Linear;
        fill.GradientX1Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientY1Units = GeneralUnitType.PixelsFromSmall;
        fill.GradientX1 = 0f;
        fill.GradientY1 = 0f;
        fill.GradientX2Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientY2Units = GeneralUnitType.PixelsFromLarge;
        fill.GradientX2 = 0f;
        fill.GradientY2 = 0f;
        fill.FillColor = new Color(2, 22, 25);
        fill.Color2 = new Color(4, 36, 40);
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "ForestGladeInputBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyMedium(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = new Color(232, 255, 117, 56); // CSS .22 alpha
        return border;
    }

    private static RectangleRuntime CreateFocusHalo()
    {
        // Leaf-medium with corners bumped by FocusHaloThickness so the halo
        // sits ~3 px outside the body's outer edge on every side.
        const float halo = FocusHaloThickness;
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "ForestGladeInputFocusHalo";
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = halo * 2f;
        ring.Height = halo * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = 2f + halo;
        ring.CustomRadiusTopLeft = 2f + halo;
        ring.CustomRadiusTopRight = 12f + halo;
        ring.CustomRadiusBottomRight = 2f + halo;
        ring.CustomRadiusBottomLeft = 12f + halo;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusHaloThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.AccentHalo;
        ring.Visible = false;
        return ring;
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        Color restBorder = new Color(232, 255, 117, 56);   // CSS .22
        Color hoverBorder = new Color(232, 255, 117, 115); // CSS .45
        Color focusBorder = ForestGladeStyling.ActiveStyle.Colors.LeafBright;
        Color disabledBorder = new Color(232, 255, 117, 20); // CSS .08

        host.States.Enabled.Apply = () => Apply(host,
            fill: ForestGladeStyling.ActiveStyle.Colors.InputFill, border: restBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, placeholder: ForestGladeStyling.ActiveStyle.Colors.Placeholder,
            caret: ForestGladeStyling.ActiveStyle.Colors.SunPale, selection: ForestGladeStyling.ActiveStyle.Colors.AccentDim,
            haloVisible: false, glow: false);

        host.States.Highlighted.Apply = () => Apply(host,
            fill: ForestGladeStyling.ActiveStyle.Colors.InputFill, border: hoverBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, placeholder: ForestGladeStyling.ActiveStyle.Colors.Placeholder,
            caret: ForestGladeStyling.ActiveStyle.Colors.SunPale, selection: ForestGladeStyling.ActiveStyle.Colors.AccentDim,
            haloVisible: false, glow: false);

        host.States.Focused.Apply = () => Apply(host,
            fill: ForestGladeStyling.ActiveStyle.Colors.InputFillFocused, border: focusBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Text, placeholder: ForestGladeStyling.ActiveStyle.Colors.Placeholder,
            caret: ForestGladeStyling.ActiveStyle.Colors.SunPale, selection: ForestGladeStyling.ActiveStyle.Colors.AccentDim,
            haloVisible: true, glow: true);

        host.States.Disabled.Apply = () => Apply(host,
            fill: ForestGladeStyling.ActiveStyle.Colors.InputFillDisabled, border: disabledBorder,
            text: ForestGladeStyling.ActiveStyle.Colors.Disabled, placeholder: ForestGladeStyling.ActiveStyle.Colors.Placeholder,
            caret: ForestGladeStyling.ActiveStyle.Colors.Disabled, selection: ForestGladeStyling.ActiveStyle.Colors.AccentDim,
            haloVisible: false, glow: false);
    }

    private void Apply(TextBoxBaseVisual host, Color fill, Color border, Color text,
        Color placeholder, Color caret, Color selection, bool haloVisible, bool glow)
    {
        // Recompute the vertical gradient stops from the state's base fill so
        // each state stays subtly darker at the top than the bottom. The
        // gradient start is the fill color itself.
        _fill.FillColor = Darken(fill, 0.65f);
        _fill.Color2 = fill;
        _fill.HasDropshadow = glow;
        if (glow)
        {
            _fill.DropshadowColor = ForestGladeStyling.ActiveStyle.Colors.GlowMedium;
            _fill.DropshadowOffsetX = 0f;
            _fill.DropshadowOffsetY = 0f;
            _fill.DropshadowBlur = FocusGlowBlur;
        }
        _border.StrokeColor = border;
        host.TextInstance.Color = text;
        host.PlaceholderTextInstance.Color = placeholder;
        host.CaretInstance.Color = caret;
        host.SelectionInstance.Color = selection;
        _focusHalo.Visible = haloVisible;
    }

    private static Color Darken(Color c, float factor)
    {
        return new Color((byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor), c.A);
    }
}
