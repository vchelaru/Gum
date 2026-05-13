using Gum.Wireframe;
using RenderingLibrary.Graphics;
using BaseScrollViewerVisual = Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Chrome used inside a menu's drop-down popup. Same V3 base as <see cref="ScrollViewerVisual"/>
/// but uses the raised-bevel + gray Surface look of a Win95 menu popup, rather than the inset
/// white look of a content scroll view. Built on demand by <see cref="MenuItemVisual"/> via
/// the <c>MenuItem.ScrollViewerVisualTemplate</c> hook so the popup is themed separately from
/// the user-facing <see cref="ScrollViewerVisual"/>.
/// </summary>
public class Retro95MenuPopupVisual : BaseScrollViewerVisual
{
    private readonly Retro95Bevel _bevel;

    public Retro95MenuPopupVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ScrollAndClipContainer.Parent = null;

        _bevel = Retro95Bevel.AddTo(this, BevelMode.Raised);

        AddChild(ScrollAndClipContainer);

        VerticalScrollBarInstance.X = -2f;
        VerticalScrollBarInstance.Y = 2f;
        VerticalScrollBarInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        VerticalScrollBarInstance.YOrigin = VerticalAlignment.Top;
        VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        VerticalScrollBarInstance.Height = -4f;
    }
}
