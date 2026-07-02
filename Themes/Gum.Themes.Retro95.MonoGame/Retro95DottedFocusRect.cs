using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

namespace Gum.Themes.Retro95;

/// <summary>
/// The canonical Win95 1-pixel dotted focus rectangle. Implemented as a single
/// stroked-and-dashed <see cref="RectangleRuntime"/> with <c>CornerRadius=0</c>:
/// Apos.Shapes natively rasterizes the dash pattern via <c>StrokeDashLength</c> /
/// <c>StrokeGapLength</c>, so the chrome scales cleanly with the host visual without
/// any per-frame dot bookkeeping or sensitivity to layout timing.
/// <para>
/// Call <see cref="Show"/> from the host's focused-state callback; call <see cref="Hide"/>
/// from non-focused states. The rectangle's bounds follow its container, which fills the
/// host by default — repoint <see cref="Container"/>'s X/Y/Width/Height before
/// <see cref="Show"/> to scope the rect to a sub-region (the label area on
/// CheckBox / RadioButton, the field area on ComboBox, etc.).
/// </para>
/// </summary>
public sealed class Retro95DottedFocusRect
{
    // With IsAntialiased=false the shape's edge rasterizes crisply, so the
    // canonical Win95 1 px dash / 1 px gap pattern reads as it should — no
    // AA bloom widening dashes or eroding gaps.
    private const float DashLength = 1f;
    private const float GapLength = 1f;
    private const float StrokeWidth = 1f;

    private readonly ContainerRuntime _container;
    private readonly RectangleRuntime _rect;

    /// <summary>The wrapper container holding the dashed-stroke rectangle. Adjust its
    /// X/Y/Width/Height before <see cref="Show"/> to scope the focus rect to a sub-region.</summary>
    public ContainerRuntime Container => _container;

    /// <summary>Creates the dotted focus rect attached to <paramref name="host"/>. The
    /// container fills the host by default.</summary>
    /// <param name="host">Visual that owns the focus rect.</param>
    /// <param name="inset">Pixels in from the container edge to the stroke. The stroke is
    /// painted on the container's edge, so the inset is realized by shrinking the
    /// container itself via negative <c>RelativeToParent</c> dimensions.</param>
    public Retro95DottedFocusRect(GraphicalUiElement host, float inset = 0f)
    {
        _container = new ContainerRuntime();
        _container.Name = "Retro95FocusRect";
        _container.HasEvents = false;
        _container.X = 0; _container.Y = 0;
        _container.XUnits = GeneralUnitType.PixelsFromMiddle;
        _container.YUnits = GeneralUnitType.PixelsFromMiddle;
        _container.XOrigin = HorizontalAlignment.Center;
        _container.YOrigin = VerticalAlignment.Center;
        _container.Width = -(inset * 2f); _container.Height = -(inset * 2f);
        _container.WidthUnits = DimensionUnitType.RelativeToParent;
        _container.HeightUnits = DimensionUnitType.RelativeToParent;
        _container.Visible = false;
        host.AddChild(_container);

        _rect = new RectangleRuntime();
        _rect.Name = "Retro95FocusRectStroke";
        _rect.X = 0; _rect.Y = 0;
        _rect.XUnits = GeneralUnitType.PixelsFromMiddle;
        _rect.YUnits = GeneralUnitType.PixelsFromMiddle;
        _rect.XOrigin = HorizontalAlignment.Center;
        _rect.YOrigin = VerticalAlignment.Center;
        _rect.Width = 0; _rect.Height = 0;
        _rect.WidthUnits = DimensionUnitType.RelativeToParent;
        _rect.HeightUnits = DimensionUnitType.RelativeToParent;
        _rect.CornerRadius = 0f;
        _rect.IsFilled = false;
        _rect.StrokeWidth = StrokeWidth;
        _rect.StrokeWidthUnits = DimensionUnitType.Absolute;
        _rect.StrokeDashLength = DashLength;
        _rect.StrokeGapLength = GapLength;
        _rect.IsAntialiased = false;
        _rect.StrokeColor = Retro95Styling.ActiveStyle.Colors.Text;
        _container.AddChild(_rect);
    }

    public void Show() => _container.Visible = true;
    public void Hide() => _container.Visible = false;
}
