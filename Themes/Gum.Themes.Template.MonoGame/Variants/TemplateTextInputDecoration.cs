using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Template.Variants;

/// <summary>
/// "Rich" variant of <see cref="Gum.Themes.Template.TemplateTextInputDecoration"/>.
/// Compared to the flat decoration this changes ONLY the shapes: the input
/// CornerRadius is bumped to 8 (vs 2) and the focus ring is a soft
/// translucent-accent glow (a wider stroke in a translucent accent color) shown on
/// the Focused state, instead of the flat variant's crisp 1px opaque ring. The
/// palette tokens, the 4 states, and the BODY-font opt-in are identical to the
/// flat source.
/// </summary>
internal sealed class TemplateTextInputDecoration
{
    // Rounder input corners (flat source used 2).
    private const float CornerRadius = 8f;
    private const float BorderThickness = 1f;
    // Soft glow: a wider, translucent stroke offset further outside the body.
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public TemplateTextInputDecoration(TextBoxBaseVisual host)
    {
        // Detach the base NineSlice background and underline focus indicator, and
        // temporarily detach the ClipContainer so we can rebuild the child order with
        // our shapes BEHIND the text-rendering layer.
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        _focusRing = TemplateShapes.FocusRing(TemplateStyling.ActiveStyle.Colors.AccentGlow, CornerRadius, FocusRingInset, FocusRingThickness, "TextInputFocusRing");
        host.AddChild(_focusRing);

        _fill = TemplateShapes.Fill(TemplateStyling.ActiveStyle.Colors.Surface1, CornerRadius, "TextInputFill");
        host.AddChild(_fill);

        _border = TemplateShapes.Border(TemplateStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness, "TextInputBorder");
        host.AddChild(_border);

        // Re-attach the ClipContainer last so text / placeholder / caret / selection
        // render on top of the shape stack.
        host.AddChild(host.ClipContainer);

        // Typed text uses the quieter BODY family rather than the display default -
        // the demonstration of multi-font support. Drop these two lines (and the
        // BodyFontFamily registration) if your theme uses a single family.
        host.TextInstance.Font = TemplateStyling.ActiveStyle.Text.BodyFontFamily;
        host.PlaceholderTextInstance.Font = TemplateStyling.ActiveStyle.Text.BodyFontFamily;

        WireStates(host);
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        // TextBox/PasswordBox have no Pushed state - you click to focus, not press.
        // The border transitions Border (rest) -> BorderHover (hover hint) -> Accent +
        // focus ring (focused).
        host.States.Enabled.Apply = () => Apply(host,
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.Border, text: TemplateStyling.ActiveStyle.Colors.Text, ring: false);

        host.States.Highlighted.Apply = () => Apply(host,
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.BorderHover, text: TemplateStyling.ActiveStyle.Colors.Text, ring: false);

        host.States.Focused.Apply = () => Apply(host,
            fill: TemplateStyling.ActiveStyle.Colors.Surface1, border: TemplateStyling.ActiveStyle.Colors.Accent, text: TemplateStyling.ActiveStyle.Colors.Text, ring: true);

        host.States.Disabled.Apply = () => Apply(host,
            fill: TemplateStyling.ActiveStyle.Colors.DisabledFill, border: TemplateStyling.ActiveStyle.Colors.DisabledBorder, text: TemplateStyling.ActiveStyle.Colors.DisabledText, ring: false);
    }

    private void Apply(TextBoxBaseVisual host, Color fill, Color border, Color text, bool ring)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        host.TextInstance.Color = text;
        host.PlaceholderTextInstance.Color = TemplateStyling.ActiveStyle.Colors.Placeholder;
        host.CaretInstance.Color = text;
        host.SelectionInstance.Color = TemplateStyling.ActiveStyle.Colors.AccentPressed;
        _focusRing.Visible = ring;
    }
}
