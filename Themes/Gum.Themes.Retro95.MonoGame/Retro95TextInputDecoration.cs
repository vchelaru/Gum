using Gum.Forms.DefaultVisuals.V3;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Retro95;

/// <summary>
/// Decorates a <see cref="TextBoxBaseVisual"/> (the shared base of V3 TextBoxVisual
/// and PasswordBoxVisual) with the Retro95 chrome: an inset 2-pixel bevel + white
/// fill. Shared by <see cref="TextBoxVisual"/> and <see cref="PasswordBoxVisual"/>.
/// </summary>
internal sealed class Retro95TextInputDecoration
{
    private readonly Retro95Bevel _bevel;
    private readonly TextBoxBaseVisual _host;

    public Retro95TextInputDecoration(TextBoxBaseVisual host)
    {
        _host = host;
        host.Background.Parent = null;
        host.FocusedIndicator.Parent = null;
        host.ClipContainer.Parent = null;

        _bevel = Retro95Bevel.AddTo(host, BevelMode.Inset, Retro95Styling.ActiveStyle.Colors.WhiteFill);

        // Re-attach ClipContainer above the bevel — text / placeholder / caret /
        // selection then render on top of the white inset body.
        host.AddChild(host.ClipContainer);

        WireStates();
    }

    private void WireStates()
    {
        _host.States.Enabled.Apply = () => Apply(
            fill: Retro95Styling.ActiveStyle.Colors.WhiteFill, text: Retro95Styling.ActiveStyle.Colors.Text,
            placeholder: Retro95Styling.ActiveStyle.Colors.DisabledText,
            caret: Retro95Styling.ActiveStyle.Colors.Text, selection: Retro95Styling.ActiveStyle.Colors.TextBoxSelection);

        _host.States.Highlighted.Apply = () => Apply(
            fill: Retro95Styling.ActiveStyle.Colors.WhiteHover, text: Retro95Styling.ActiveStyle.Colors.Text,
            placeholder: Retro95Styling.ActiveStyle.Colors.DisabledText,
            caret: Retro95Styling.ActiveStyle.Colors.Text, selection: Retro95Styling.ActiveStyle.Colors.TextBoxSelection);

        _host.States.Focused.Apply = () => Apply(
            fill: Retro95Styling.ActiveStyle.Colors.WhiteFill, text: Retro95Styling.ActiveStyle.Colors.Text,
            placeholder: Retro95Styling.ActiveStyle.Colors.DisabledText,
            caret: Retro95Styling.ActiveStyle.Colors.Text, selection: Retro95Styling.ActiveStyle.Colors.TextBoxSelection);

        _host.States.Disabled.Apply = () => Apply(
            fill: Retro95Styling.ActiveStyle.Colors.Surface, text: Retro95Styling.ActiveStyle.Colors.DisabledText,
            placeholder: Retro95Styling.ActiveStyle.Colors.DisabledText,
            caret: Retro95Styling.ActiveStyle.Colors.DisabledText, selection: Retro95Styling.ActiveStyle.Colors.TextBoxSelection);
    }

    private void Apply(Color fill, Color text, Color placeholder, Color caret, Color selection)
    {
        _bevel.SetFill(fill);
        _host.TextInstance.Color = text;
        _host.PlaceholderTextInstance.Color = placeholder;
        _host.CaretInstance.Color = caret;
        _host.SelectionInstance.Color = selection;
    }
}
