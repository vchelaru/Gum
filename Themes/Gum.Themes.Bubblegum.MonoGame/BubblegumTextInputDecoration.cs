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

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3
/// TextBoxVisual and PasswordBoxVisual) with the Bubblegum shape stack:
/// translucent focus ring + Surface1 fill + 2 px pink border at 8 px corner
/// radius. Shared by <see cref="TextBoxVisual"/> and <see cref="PasswordBoxVisual"/>.
/// </summary>
internal sealed class BubblegumTextInputDecoration
{
    private const float CornerRadius = 8f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public BubblegumTextInputDecoration(TextBoxBaseVisual host)
    {
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        _focusRing = BubblegumShapes.FocusRing(
            color: BubblegumStyling.ActiveStyle.Colors.FocusRing,
            cornerRadius: CornerRadius,
            inset: FocusRingInset,
            thickness: FocusRingThickness,
            name: "BubblegumTextInputFocusRing");
        host.AddChild(_focusRing);

        _fill = BubblegumShapes.Fill(
            color: BubblegumStyling.ActiveStyle.Colors.Surface1,
            cornerRadius: CornerRadius,
            name: "BubblegumTextInputFill");
        host.AddChild(_fill);

        // Re-attach ClipContainer between fill and border so text / placeholder /
        // caret / selection render above the fill, but the rounded border draws
        // ON TOP. Gum's clip container is rectangular — without rounded
        // clipping, content rendered to the edge would visibly poke past the
        // rounded outline at the corners. Painting the border last masks those
        // corner regions with the theme's pink stroke.
        host.AddChild(host.ClipContainer);

        _border = BubblegumShapes.Border(
            color: BubblegumStyling.ActiveStyle.Colors.Border,
            cornerRadius: CornerRadius,
            thickness: BorderThickness,
            name: "BubblegumTextInputBorder");
        host.AddChild(_border);

        WireStates(host);
    }

    private void WireStates(TextBoxBaseVisual host)
    {
        host.States.Enabled.Apply = () => Apply(host,
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Border,
            text: BubblegumStyling.ActiveStyle.Colors.Text, placeholder: BubblegumStyling.ActiveStyle.Colors.Placeholder,
            caret: BubblegumStyling.ActiveStyle.Colors.Accent, selection: BubblegumStyling.ActiveStyle.Colors.AccentLight, ring: false);

        host.States.Highlighted.Apply = () => Apply(host,
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, placeholder: BubblegumStyling.ActiveStyle.Colors.Placeholder,
            caret: BubblegumStyling.ActiveStyle.Colors.Accent, selection: BubblegumStyling.ActiveStyle.Colors.AccentLight, ring: false);

        host.States.Focused.Apply = () => Apply(host,
            fill: BubblegumStyling.ActiveStyle.Colors.Surface1, border: BubblegumStyling.ActiveStyle.Colors.Accent,
            text: BubblegumStyling.ActiveStyle.Colors.Text, placeholder: BubblegumStyling.ActiveStyle.Colors.Placeholder,
            caret: BubblegumStyling.ActiveStyle.Colors.Accent, selection: BubblegumStyling.ActiveStyle.Colors.AccentLight, ring: true);

        host.States.Disabled.Apply = () => Apply(host,
            fill: BubblegumStyling.ActiveStyle.Colors.DisabledFill, border: BubblegumStyling.ActiveStyle.Colors.Disabled,
            text: BubblegumStyling.ActiveStyle.Colors.Disabled, placeholder: BubblegumStyling.ActiveStyle.Colors.Disabled,
            caret: BubblegumStyling.ActiveStyle.Colors.Disabled, selection: BubblegumStyling.ActiveStyle.Colors.AccentLight, ring: false);
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
