using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Hazard;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3 TextBoxVisual
/// and PasswordBoxVisual) with the theme's shape stack - focus ring + filled body +
/// stroked border - and wires the corresponding state callbacks. This lets
/// <see cref="TextBoxVisual"/> and <see cref="PasswordBoxVisual"/> share their
/// decoration without a common base class (each already extends its own V3 type).
///
/// This is the recommended pattern any time two visuals need identical chrome but
/// inherit from different bases: extract the shape stack into a helper, hold a
/// reference to it from each visual so the shapes stay alive for the state callbacks.
/// </summary>
internal sealed class HazardTextInputDecoration
{
    private const float CornerRadius = 0f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public HazardTextInputDecoration(TextBoxBaseVisual host)
    {
        // Detach the base NineSlice background and underline focus indicator, and
        // temporarily detach the ClipContainer so we can rebuild the child order with
        // our shapes BEHIND the text-rendering layer.
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        _focusRing = HazardShapes.FocusRing(HazardStyling.ActiveStyle.Colors.Accent, CornerRadius, FocusRingInset, BorderThickness, "TextInputFocusRing");
        host.AddChild(_focusRing);

        _fill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface1, CornerRadius, "TextInputFill");
        host.AddChild(_fill);

        _border = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness, "TextInputBorder");
        host.AddChild(_border);

        // Re-attach the ClipContainer last so text / placeholder / caret / selection
        // render on top of the shape stack.
        host.AddChild(host.ClipContainer);

        WireStates(host);
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        // TextBox/PasswordBox have no Pushed state - you click to focus, not press.
        // The border transitions Border (rest) -> BorderHover (hover hint) -> Accent +
        // focus ring (focused).
        host.States.Enabled.Apply = () => Apply(host,
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Border, text: HazardStyling.ActiveStyle.Colors.Text, ring: false);

        host.States.Highlighted.Apply = () => Apply(host,
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.BorderHover, text: HazardStyling.ActiveStyle.Colors.Text, ring: false);

        host.States.Focused.Apply = () => Apply(host,
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Accent, text: HazardStyling.ActiveStyle.Colors.Text, ring: true);

        host.States.Disabled.Apply = () => Apply(host,
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder, text: HazardStyling.ActiveStyle.Colors.DisabledText, ring: false);
    }

    private void Apply(TextBoxBaseVisual host, Color fill, Color border, Color text, bool ring)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        host.TextInstance.Color = text;
        host.PlaceholderTextInstance.Color = HazardStyling.ActiveStyle.Colors.Placeholder;
        host.CaretInstance.Color = text;
        host.SelectionInstance.Color = HazardStyling.ActiveStyle.Colors.AccentPressed;
        _focusRing.Visible = ring;
    }
}
