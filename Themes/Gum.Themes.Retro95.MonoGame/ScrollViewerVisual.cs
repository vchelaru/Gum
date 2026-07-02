using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using BaseScrollViewerVisual = Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ScrollViewer visual. Same shell as <see cref="ListBoxVisual"/> — inset
/// bevel + white fill. ScrollViewer in the V3 hierarchy is the cascading base for
/// ItemsControl / Menu / MenuItem chrome, so themeing it once covers those too
/// (per the gum-theming skill's note on visual-side inheritance).
/// </summary>
public class ScrollViewerVisual : BaseScrollViewerVisual
{
    private readonly Retro95Bevel _bevel;

    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ScrollAndClipContainer.Parent = null;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Inset, Retro95Styling.ActiveStyle.Colors.WhiteFill);

        AddChild(ScrollAndClipContainer);

        // Inset on all four sides so the scroll bar fits inside the bevel chrome
        // (see ListBoxVisual for the same pattern + rationale).
        VerticalScrollBarInstance.X = -2f;
        VerticalScrollBarInstance.Y = 2f;
        VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromSmall;
        VerticalScrollBarInstance.YOrigin = VerticalAlignment.Top;
        VerticalScrollBarInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        VerticalScrollBarInstance.Height = -4f;

        WireStates();
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.WhiteFill);
        States.Focused.Apply = () => _bevel.SetFill(Retro95Styling.ActiveStyle.Colors.WhiteFill);
    }
}
