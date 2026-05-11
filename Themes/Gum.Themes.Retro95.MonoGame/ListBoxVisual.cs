using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ListBox visual. Inset bevel + white fill (matches <c>.rc-lb</c>).
/// Border-on-top isn't needed here — the bevel chrome already fills its own edge strips
/// in front of the clip container.
/// </summary>
public class ListBoxVisual : BaseListBoxVisual
{
    private readonly Retro95Bevel _bevel;

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ClipAndScrollContainer.Parent = null;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Inset, Retro95Colors.WhiteFill);

        AddChild(ClipAndScrollContainer);

        if (VerticalScrollBarInstance != null)
        {
            VerticalScrollBarInstance.X = -2f;
        }

        WireStates();
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => _bevel.SetFill(Retro95Colors.WhiteFill);
        States.Highlighted.Apply = () => _bevel.SetFill(Retro95Colors.WhiteFill);
        States.Focused.Apply = () => _bevel.SetFill(Retro95Colors.WhiteFill);
        States.HighlightedFocused.Apply = () => _bevel.SetFill(Retro95Colors.WhiteFill);
        States.Pushed.Apply = () => _bevel.SetFill(Retro95Colors.WhiteFill);
        States.Disabled.Apply = () => _bevel.SetFill(Retro95Colors.Surface);
        States.DisabledFocused.Apply = () => _bevel.SetFill(Retro95Colors.Surface);
    }
}
