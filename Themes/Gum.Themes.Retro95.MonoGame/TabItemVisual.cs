using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using BaseTabItemVisual = Gum.Forms.DefaultVisuals.V3.TabItemVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95 styled TabItem visual: a raised bevel in the Surface gray that turns the navy Selection color
/// (with white text) when selected.
/// </summary>
public class TabItemVisual : BaseTabItemVisual
{
    private readonly Retro95Bevel _bevel;

    public TabItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        TextInstance.Parent = null;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Raised);
        AddChild(TextInstance);

        States.Selected.Apply = () => ApplyPalette(Retro95Styling.ActiveStyle.Colors.Selection, Retro95Styling.ActiveStyle.Colors.SelectionText);
        States.Unselected.Apply = () => ApplyPalette(Retro95Styling.ActiveStyle.Colors.Surface, Retro95Styling.ActiveStyle.Colors.Text);
        ApplyPalette(Retro95Styling.ActiveStyle.Colors.Surface, Retro95Styling.ActiveStyle.Colors.Text);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _bevel.SetFill(fill);
        TextInstance.Color = text;
    }
}
