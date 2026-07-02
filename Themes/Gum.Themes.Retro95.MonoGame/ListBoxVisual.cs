using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
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

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Inset, Retro95Styling.ActiveStyle.Colors.WhiteFill);

        AddChild(ClipAndScrollContainer);

        if (VerticalScrollBarInstance != null)
        {
            // Inset 2 px from every side so the scroll bar sits inside the
            // ListBox's 2 px beveled chrome instead of overlapping it. Right
            // inset comes from X = -2; top/bottom come from Y = 2 + a -4 height
            // adjustment (Win95 also stopped the bar short of the bevel).
            VerticalScrollBarInstance.X = -2f;
            VerticalScrollBarInstance.Y = 2f;
            VerticalScrollBarInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            VerticalScrollBarInstance.YOrigin = VerticalAlignment.Top;
            VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            VerticalScrollBarInstance.Height = -4f;
        }

        WireStates();
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.WhiteFill);
        States.Highlighted.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.WhiteFill);
        States.Focused.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.WhiteFill);
        States.HighlightedFocused.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.WhiteFill);
        States.Pushed.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.WhiteFill);
        States.Disabled.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.Surface);
        States.DisabledFocused.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.Surface);
    }
}
