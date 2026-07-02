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

namespace Gum.Themes.Meadow;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3
/// TextBoxVisual and PasswordBoxVisual) with the Meadow input shape stack: a
/// peach-light fill with a 13 px corner radius and a 2.5 px border that is
/// transparent at rest (so the box doesn't shift on hover), peach on hover, and
/// sky-blue with a translucent blue glow ring on focus. Body text uses the
/// Quicksand face. Shared by <see cref="TextBoxVisual"/> and
/// <see cref="PasswordBoxVisual"/>.
/// </summary>
internal sealed class MeadowTextInputDecoration
{
    private const float CornerRadius = 13f;
    private const float BorderThickness = 2.5f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public MeadowTextInputDecoration(TextBoxBaseVisual host)
    {
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        _focusRing = CreateFocusRing();
        host.AddChild(_focusRing);

        _fill = CreateFill();
        host.AddChild(_fill);

        // Re-attach ClipContainer between fill and border so text / placeholder /
        // caret / selection render above the fill, but the rounded border draws
        // ON TOP — Gum's clip container is rectangular, so content extending to
        // the edge would poke past the rounded outline at the corners.
        host.AddChild(host.ClipContainer);

        _border = CreateBorder();
        host.AddChild(_border);

        // Body face for the typed text + placeholder.
        host.TextInstance.Font = MeadowStyling.ActiveStyle.Text.BodyFontFamily;
        host.PlaceholderTextInstance.Font = MeadowStyling.ActiveStyle.Text.BodyFontFamily;

        WireStates(host);
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowTextInputFill";
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
        fill.FillColor = MeadowStyling.ActiveStyle.Colors.PeachLight;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "MeadowTextInputBorder";
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
        border.StrokeColor = new Color(0, 0, 0, 0);
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "MeadowTextInputFocusRing";
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
        ring.StrokeColor = MeadowStyling.ActiveStyle.Colors.BlueFocusRing;
        ring.Visible = false;
        return ring;
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        host.States.Enabled.Apply = () => Apply(host,
            fill: MeadowStyling.ActiveStyle.Colors.PeachLight, border: new Color(0, 0, 0, 0),
            text: MeadowStyling.ActiveStyle.Colors.TealDark, placeholder: MeadowStyling.ActiveStyle.Colors.Muted,
            caret: MeadowStyling.ActiveStyle.Colors.Teal, selection: MeadowStyling.ActiveStyle.Colors.Sage, ring: false);

        host.States.Highlighted.Apply = () => Apply(host,
            fill: MeadowStyling.ActiveStyle.Colors.PeachLight, border: MeadowStyling.ActiveStyle.Colors.PeachDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, placeholder: MeadowStyling.ActiveStyle.Colors.Muted,
            caret: MeadowStyling.ActiveStyle.Colors.Teal, selection: MeadowStyling.ActiveStyle.Colors.Sage, ring: false);

        host.States.Focused.Apply = () => Apply(host,
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.Blue,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, placeholder: MeadowStyling.ActiveStyle.Colors.Muted,
            caret: MeadowStyling.ActiveStyle.Colors.Teal, selection: MeadowStyling.ActiveStyle.Colors.Sage, ring: true);

        host.States.Disabled.Apply = () => Apply(host,
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, placeholder: MeadowStyling.ActiveStyle.Colors.DisabledInk,
            caret: MeadowStyling.ActiveStyle.Colors.DisabledInk, selection: MeadowStyling.ActiveStyle.Colors.Sage, ring: false);
    }

    private void Apply(TextBoxBaseVisual host, Color fill, Color border, Color text,
        Color placeholder, Color caret, Color selection, bool ring)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        host.TextInstance.Color = text;
        host.PlaceholderTextInstance.Color = placeholder;
        host.CaretInstance.Color = caret;
        host.SelectionInstance.Color = selection;
        _focusRing.Visible = ring;
    }
}
