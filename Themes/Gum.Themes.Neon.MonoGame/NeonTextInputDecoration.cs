using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.Neon;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3
/// TextBoxVisual and PasswordBoxVisual) with the Neon shape stack: Surface1
/// fill + 1 px border at near-square corners (CSS <c>--r: 1px</c>). The chrome
/// is intentionally state-stable — hover and focus do not modulate border /
/// fill / glow. Focus is signalled by the caret blinking inside the field,
/// which the V3 base already handles. Shared by <see cref="TextBoxVisual"/>
/// and <see cref="PasswordBoxVisual"/>.
/// </summary>
internal sealed class NeonTextInputDecoration
{
    private const float CornerRadius = 1f;
    private const float BorderThickness = 1f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public NeonTextInputDecoration(TextBoxBaseVisual host)
    {
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        _fill = CreateFill();
        host.AddChild(_fill);

        // Re-attach ClipContainer between fill and border so text / placeholder /
        // caret / selection render above the fill, but the border draws ON TOP.
        host.AddChild(host.ClipContainer);

        _border = CreateBorder();
        host.AddChild(_border);

        WireStates(host);
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
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
        fill.FillColor = NeonStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
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
        border.StrokeColor = NeonStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        // Enabled / Highlighted / Focused all render identical chrome — the
        // blinking caret (provided by the V3 base) is the only focus cue.
        // Hover/focus border colour changes and the offset focus ring were
        // visually noisy on a control that's rarely the centre of attention.
        host.States.Enabled.Apply = () => Apply(host,
            fill: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Border,
            text: NeonStyling.ActiveStyle.Colors.Text, placeholder: NeonStyling.ActiveStyle.Colors.Placeholder,
            caret: NeonStyling.ActiveStyle.Colors.Accent, selection: NeonStyling.ActiveStyle.Colors.AccentDim);

        host.States.Highlighted.Apply = () => Apply(host,
            fill: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Border,
            text: NeonStyling.ActiveStyle.Colors.Text, placeholder: NeonStyling.ActiveStyle.Colors.Placeholder,
            caret: NeonStyling.ActiveStyle.Colors.Accent, selection: NeonStyling.ActiveStyle.Colors.AccentDim);

        host.States.Focused.Apply = () => Apply(host,
            fill: NeonStyling.ActiveStyle.Colors.Surface1, border: NeonStyling.ActiveStyle.Colors.Border,
            text: NeonStyling.ActiveStyle.Colors.Text, placeholder: NeonStyling.ActiveStyle.Colors.Placeholder,
            caret: NeonStyling.ActiveStyle.Colors.Accent, selection: NeonStyling.ActiveStyle.Colors.AccentDim);

        host.States.Disabled.Apply = () => Apply(host,
            fill: NeonStyling.ActiveStyle.Colors.Background, border: NeonStyling.ActiveStyle.Colors.DisabledBorder,
            text: NeonStyling.ActiveStyle.Colors.Muted, placeholder: NeonStyling.ActiveStyle.Colors.Placeholder,
            caret: NeonStyling.ActiveStyle.Colors.Muted, selection: NeonStyling.ActiveStyle.Colors.AccentDim);
    }

    private void Apply(TextBoxBaseVisual host, Color fill, Color border, Color text,
        Color placeholder, Color caret, Color selection)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        host.TextInstance.Color = text;
        host.PlaceholderTextInstance.Color = placeholder;
        host.CaretInstance.Color = caret;
        host.SelectionInstance.Color = selection;
    }
}
