using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace Gum.Themes.Neon;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3
/// TextBoxVisual and PasswordBoxVisual) with the Neon shape stack: Surface1
/// fill with a state-driven cyan glow + 1 px border at near-square corners
/// (CSS <c>--r: 1px</c>). Shared by <see cref="TextBoxVisual"/> and
/// <see cref="PasswordBoxVisual"/>.
/// </summary>
internal sealed class NeonTextInputDecoration
{
    private const float CornerRadius = 1f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 4f;
    private const float FocusRingThickness = 1f;
    private const float FocusGlowBlur = 18f;

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

    public NeonTextInputDecoration(TextBoxBaseVisual host)
    {
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        // Offset white focus ring goes in first (behind everything). Visible
        // only on focus — gives focus a distinct shape from hover.
        _focusRing = CreateFocusRing();
        host.AddChild(_focusRing);

        _fill = CreateFill();
        host.AddChild(_fill);

        // Re-attach ClipContainer between fill and border so text / placeholder /
        // caret / selection render above the fill, but the border draws ON TOP.
        // Gum's clip container is rectangular — at the 1 px corner radius this
        // matters less than at Bubblegum's 8 px, but the same ordering keeps
        // the border crisp over content that hits the edge.
        host.AddChild(host.ClipContainer);

        _border = CreateBorder();
        host.AddChild(_border);

        WireStates(host);
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "NeonTextInputFocusRing";
        ring.X = 0;
        ring.Y = 0;
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = CornerRadius + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = NeonPalette.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "NeonTextInputFill";
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
        fill.Color = NeonColors.Surface1;
        fill.HasDropshadow = false;
        fill.DropshadowColor = NeonPalette.GlowMedium;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = 0f;
        fill.DropshadowBlurX = FocusGlowBlur;
        fill.DropshadowBlurY = FocusGlowBlur;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "NeonTextInputBorder";
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
        border.Color = NeonColors.Border;
        return border;
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        host.States.Enabled.Apply = () => Apply(host,
            fill: NeonColors.Surface1, border: NeonColors.Border,
            text: NeonColors.Text, placeholder: NeonColors.Placeholder,
            caret: NeonColors.Accent, selection: NeonColors.AccentDim, glow: false);

        host.States.Highlighted.Apply = () => Apply(host,
            fill: NeonColors.Surface1, border: NeonColors.BorderHover,
            text: NeonColors.Text, placeholder: NeonColors.Placeholder,
            caret: NeonColors.Accent, selection: NeonColors.AccentDim, glow: false);

        host.States.Focused.Apply = () => Apply(host,
            fill: NeonColors.Surface1, border: NeonColors.Accent,
            text: NeonColors.Text, placeholder: NeonColors.Placeholder,
            caret: NeonColors.Accent, selection: NeonColors.AccentDim, glow: true);

        // Disabled body text → Muted so the value stays legible. The near-black
        // Disabled token only colors the chrome / placeholder where there's
        // nothing the user needs to read.
        host.States.Disabled.Apply = () => Apply(host,
            fill: NeonColors.Background, border: NeonColors.DisabledBorder,
            text: NeonColors.Muted, placeholder: NeonColors.Placeholder,
            caret: NeonColors.Muted, selection: NeonColors.AccentDim, glow: false);
    }

    private void Apply(TextBoxBaseVisual host, Color fill, Color border, Color text,
        Color placeholder, Color caret, Color selection, bool glow)
    {
        _fill.Color = fill;
        _border.Color = border;
        host.TextInstance.Color = text;
        host.PlaceholderTextInstance.Color = placeholder;
        host.CaretInstance.Color = caret;
        host.SelectionInstance.Color = selection;
        _fill.HasDropshadow = glow;
        _focusRing.Visible = glow;
    }
}
