using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3
/// TextBoxVisual and PasswordBoxVisual) with the Dark Pro shape stack
/// (focus ring + filled rect + 1px stroked border at CornerRadius=2) and
/// the corresponding state callbacks. Lets <see cref="TextBoxVisual"/>
/// and <see cref="PasswordBoxVisual"/> share their decoration logic
/// without forcing them into a common base class - each must already
/// extend its own V3.* type.
/// </summary>
internal sealed class DarkProTextInputDecoration
{
    private const float CornerRadius = 2f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public DarkProTextInputDecoration(TextBoxBaseVisual host)
    {
        // Detach the base NineSlice background and underline focus indicator.
        // Temporarily detach the ClipContainer so we can rebuild the child
        // order with our shapes behind the text-rendering layer.
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        _focusRing = CreateFocusRing();
        host.AddChild(_focusRing);

        _fill = CreateFill();
        host.AddChild(_fill);

        _border = CreateBorder();
        host.AddChild(_border);

        // Re-attach the ClipContainer last so text / placeholder / caret /
        // selection render on top of the shape stack.
        host.AddChild(host.ClipContainer);

        WireStates(host);
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "DarkProTextInputFill";
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
        border.Name = "DarkProTextInputBorder";
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

    private static RectangleRuntime CreateFocusRing()
    {
        // 1px stroke sitting one pixel outside the border, matching the CSS
        // `box-shadow: 0 0 0 1px var(--acc)` from the mockup.
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "DarkProTextInputFocusRing";
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
        ring.StrokeWidth = BorderThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = DarkProStyling.ActiveStyle.Colors.Accent;
        ring.Visible = false;
        return ring;
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        // TextBox/PasswordBox don't have a Pushed state - you click to focus,
        // not press. Hover → click → focused is the normal flow, so the
        // border transitions Border (rest) → Accent (hover hint) → Accent +
        // focus ring (focused).
        host.States.Enabled.Apply = () => Apply(host,
            fill: DarkProStyling.ActiveStyle.Colors.Surface1, border: DarkProStyling.ActiveStyle.Colors.Border,
            text: DarkProStyling.ActiveStyle.Colors.Text, placeholder: DarkProStyling.ActiveStyle.Colors.Placeholder,
            caret: DarkProStyling.ActiveStyle.Colors.Text, selection: DarkProStyling.ActiveStyle.Colors.AccentDark, ring: false);

        // Hover uses BorderHover (gray) per the CSS spec. Unlike Button - which
        // jumps to Accent on hover so a hover->press doesn't flicker the color
        // family - text inputs have no Pushed state, so the natural progression
        // is transient gray hover -> sustained blue focus. Keeping that contrast
        // helps the user see they've moved from "could click" to "now editing".
        host.States.Highlighted.Apply = () => Apply(host,
            fill: DarkProStyling.ActiveStyle.Colors.Surface1, border: DarkProStyling.ActiveStyle.Colors.BorderHover,
            text: DarkProStyling.ActiveStyle.Colors.Text, placeholder: DarkProStyling.ActiveStyle.Colors.Placeholder,
            caret: DarkProStyling.ActiveStyle.Colors.Text, selection: DarkProStyling.ActiveStyle.Colors.AccentDark, ring: false);

        host.States.Focused.Apply = () => Apply(host,
            fill: DarkProStyling.ActiveStyle.Colors.Surface1, border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text, placeholder: DarkProStyling.ActiveStyle.Colors.Placeholder,
            caret: DarkProStyling.ActiveStyle.Colors.Text, selection: DarkProStyling.ActiveStyle.Colors.AccentDark, ring: true);

        host.States.Disabled.Apply = () => Apply(host,
            fill: DarkProStyling.ActiveStyle.Colors.DisabledFill, border: DarkProStyling.ActiveStyle.Colors.DisabledBorder,
            text: DarkProStyling.ActiveStyle.Colors.DisabledText, placeholder: DarkProStyling.ActiveStyle.Colors.DisabledText,
            caret: DarkProStyling.ActiveStyle.Colors.DisabledText, selection: DarkProStyling.ActiveStyle.Colors.AccentDark, ring: false);
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
