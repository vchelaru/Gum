using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.Retro95;

/// <summary>
/// The 2-pixel bevel mode applied to a Retro95 chrome rectangle.
/// </summary>
public enum BevelMode
{
    /// <summary>Raised — top-left light, bottom-right dark. Default button / scroll-bar arrow.</summary>
    Raised,
    /// <summary>Sunken — top-left dark, bottom-right light. Pressed button.</summary>
    Sunken,
    /// <summary>Inset — top-left dark, bottom-right light, used for inputs (TextBox, ListBox, ComboBox field).</summary>
    Inset,
    /// <summary>Status panel: a 1-pixel inverse bevel (top-left dark, bottom-right light, no inner highlight band) — used for the dim sunken cells of a status bar.</summary>
    StatusPanel,
}

/// <summary>
/// A Retro95 beveled chrome rectangle. Composes 8 thin <see cref="RectangleRuntime"/>
/// strips along the four edges to reproduce the Win95 CSS 4-layer inset box-shadow:
/// <c>inset 1px 1px 0 #fff, inset 2px 2px 0 #DFDFDF, inset -1px -1px 0 #808080, inset -2px -2px 0 #404040</c>.
/// <para>
/// Use <see cref="AddTo"/> to attach the chrome to a parent; the returned instance exposes a
/// <see cref="SetMode"/> to flip raised ↔ sunken for press states, and a <see cref="SetFill"/>
/// to tweak the central body color. Strips fill the parent at <c>RelativeToParent</c>, so
/// the bevel resizes with its host.
/// </para>
/// <para>
/// The Win95 chrome has square corners by design — no <c>CornerRadius</c> property exists
/// here. If a control needs a focus indicator, the convention is a 1-pixel dotted inset border;
/// since the runtime has no dotted-stroke primitive, this theme approximates it with a single
/// dark RectangleRuntime inset 4 px from the body edge (see consumers like ButtonVisual).
/// </para>
/// </summary>
public sealed class Retro95Bevel
{
    private const float OuterStripThickness = 1f;
    private const float InnerStripThickness = 1f;

    private readonly RectangleRuntime _fill;

    // Outer ring (1 px on each edge).
    private readonly RectangleRuntime _outerTop;
    private readonly RectangleRuntime _outerLeft;
    private readonly RectangleRuntime _outerRight;
    private readonly RectangleRuntime _outerBottom;

    // Inner ring (1 px just inside the outer ring).
    private readonly RectangleRuntime _innerTop;
    private readonly RectangleRuntime _innerLeft;
    private readonly RectangleRuntime _innerRight;
    private readonly RectangleRuntime _innerBottom;

    private Retro95Bevel(
        RectangleRuntime fill,
        RectangleRuntime outerTop, RectangleRuntime outerLeft,
        RectangleRuntime outerRight, RectangleRuntime outerBottom,
        RectangleRuntime innerTop, RectangleRuntime innerLeft,
        RectangleRuntime innerRight, RectangleRuntime innerBottom)
    {
        _fill = fill;
        _outerTop = outerTop;
        _outerLeft = outerLeft;
        _outerRight = outerRight;
        _outerBottom = outerBottom;
        _innerTop = innerTop;
        _innerLeft = innerLeft;
        _innerRight = innerRight;
        _innerBottom = innerBottom;
    }

    /// <summary>The central fill rectangle. Color defaults to <see cref="Retro95Styling.ActiveStyle.Colors.Surface"/>.</summary>
    public RectangleRuntime Fill => _fill;

    /// <summary>
    /// Adds a beveled chrome block to <paramref name="parent"/>. Returns the bevel handle for
    /// later state-driven mode and fill changes. The chrome fills the parent (RelativeToParent
    /// on both axes) by default.
    /// </summary>
    public static Retro95Bevel AddTo(GraphicalUiElement parent, BevelMode mode, Color? fillColor = null)
    {
        RectangleRuntime fill = NewStretchedRect("Retro95BevelFill");
        fill.FillColor = fillColor ?? Retro95Styling.ActiveStyle.Colors.Surface;
        parent.AddChild(fill);

        RectangleRuntime outerTop = NewEdgeStrip("Retro95BevelOuterTop", Edge.Top, OuterStripThickness, inset: 0f);
        RectangleRuntime outerLeft = NewEdgeStrip("Retro95BevelOuterLeft", Edge.Left, OuterStripThickness, inset: 0f);
        RectangleRuntime outerRight = NewEdgeStrip("Retro95BevelOuterRight", Edge.Right, OuterStripThickness, inset: 0f);
        RectangleRuntime outerBottom = NewEdgeStrip("Retro95BevelOuterBottom", Edge.Bottom, OuterStripThickness, inset: 0f);

        RectangleRuntime innerTop = NewEdgeStrip("Retro95BevelInnerTop", Edge.Top, InnerStripThickness, inset: OuterStripThickness);
        RectangleRuntime innerLeft = NewEdgeStrip("Retro95BevelInnerLeft", Edge.Left, InnerStripThickness, inset: OuterStripThickness);
        RectangleRuntime innerRight = NewEdgeStrip("Retro95BevelInnerRight", Edge.Right, InnerStripThickness, inset: OuterStripThickness);
        RectangleRuntime innerBottom = NewEdgeStrip("Retro95BevelInnerBottom", Edge.Bottom, InnerStripThickness, inset: OuterStripThickness);

        parent.AddChild(outerTop);
        parent.AddChild(outerLeft);
        parent.AddChild(outerRight);
        parent.AddChild(outerBottom);
        parent.AddChild(innerTop);
        parent.AddChild(innerLeft);
        parent.AddChild(innerRight);
        parent.AddChild(innerBottom);

        Retro95Bevel bevel = new Retro95Bevel(fill,
            outerTop, outerLeft, outerRight, outerBottom,
            innerTop, innerLeft, innerRight, innerBottom);
        bevel.SetMode(mode);
        return bevel;
    }

    /// <summary>Flip the bevel's edge colors. Cheap (just sets <see cref="RectangleRuntime.FillColor"/>
    /// on 8 thin strips); call from a state callback whenever press / inset state changes.</summary>
    public void SetMode(BevelMode mode)
    {
        switch (mode)
        {
            case BevelMode.Raised:
                // top-left light, bottom-right dark
                _outerTop.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightOuter;
                _outerLeft.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightOuter;
                _innerTop.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightInner;
                _innerLeft.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightInner;
                _outerBottom.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowOuter;
                _outerRight.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowOuter;
                _innerBottom.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowInner;
                _innerRight.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowInner;
                break;

            case BevelMode.Sunken:
                // top-left dark, bottom-right light
                _outerTop.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowOuter;
                _outerLeft.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowOuter;
                _innerTop.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowInner;
                _innerLeft.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowInner;
                _outerBottom.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightOuter;
                _outerRight.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightOuter;
                _innerBottom.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightInner;
                _innerRight.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightInner;
                break;

            case BevelMode.Inset:
                // Text-input style: outer dark + inner highlight on top-left,
                // outer light + inner shadow on bottom-right. Same as Sunken
                // but with the highlight/shadow swapped on the inner ring so
                // the white inner band reads as the input fill catching light.
                _outerTop.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowInner;
                _outerLeft.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowInner;
                _innerTop.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowOuter;
                _innerLeft.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowOuter;
                _outerBottom.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightInner;
                _outerRight.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightInner;
                _innerBottom.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightOuter;
                _innerRight.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightOuter;
                break;

            case BevelMode.StatusPanel:
                // 1-pixel inverse bevel — only the outer ring is colored, inner ring matches the fill.
                _outerTop.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowInner;
                _outerLeft.FillColor = Retro95Styling.ActiveStyle.Colors.ShadowInner;
                _outerBottom.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightOuter;
                _outerRight.FillColor = Retro95Styling.ActiveStyle.Colors.HighlightOuter;
                _innerTop.FillColor = _fill.FillColor;
                _innerLeft.FillColor = _fill.FillColor;
                _innerBottom.FillColor = _fill.FillColor;
                _innerRight.FillColor = _fill.FillColor;
                break;
        }
    }

    /// <summary>Set the central fill color (e.g. white for text-input fills, gray for button bodies).</summary>
    public void SetFill(Color color) => _fill.FillColor = color;

    private enum Edge { Top, Left, Right, Bottom }

    private static RectangleRuntime NewStretchedRect(string name)
    {
        RectangleRuntime r = new RectangleRuntime();
        r.Name = name;
        r.X = 0; r.Y = 0;
        r.XUnits = GeneralUnitType.PixelsFromMiddle;
        r.YUnits = GeneralUnitType.PixelsFromMiddle;
        r.XOrigin = HorizontalAlignment.Center;
        r.YOrigin = VerticalAlignment.Center;
        r.Width = 0; r.Height = 0;
        r.WidthUnits = DimensionUnitType.RelativeToParent;
        r.HeightUnits = DimensionUnitType.RelativeToParent;
        r.IsFilled = true;
        r.StrokeWidth = 0;
        return r;
    }

    private static RectangleRuntime NewEdgeStrip(string name, Edge edge, float thickness, float inset)
    {
        RectangleRuntime r = new RectangleRuntime();
        r.Name = name;
        r.IsFilled = true;
        r.StrokeWidth = 0;
        switch (edge)
        {
            case Edge.Top:
                r.X = 0;
                r.Y = inset;
                r.XUnits = GeneralUnitType.PixelsFromMiddle;
                r.YUnits = GeneralUnitType.PixelsFromSmall;
                r.XOrigin = HorizontalAlignment.Center;
                r.YOrigin = VerticalAlignment.Top;
                r.Width = -(2f * inset);
                r.Height = thickness;
                r.WidthUnits = DimensionUnitType.RelativeToParent;
                r.HeightUnits = DimensionUnitType.Absolute;
                break;
            case Edge.Bottom:
                r.X = 0;
                r.Y = -inset;
                r.XUnits = GeneralUnitType.PixelsFromMiddle;
                r.YUnits = GeneralUnitType.PixelsFromLarge;
                r.XOrigin = HorizontalAlignment.Center;
                r.YOrigin = VerticalAlignment.Bottom;
                r.Width = -(2f * inset);
                r.Height = thickness;
                r.WidthUnits = DimensionUnitType.RelativeToParent;
                r.HeightUnits = DimensionUnitType.Absolute;
                break;
            case Edge.Left:
                r.X = inset;
                r.Y = 0;
                r.XUnits = GeneralUnitType.PixelsFromSmall;
                r.YUnits = GeneralUnitType.PixelsFromMiddle;
                r.XOrigin = HorizontalAlignment.Left;
                r.YOrigin = VerticalAlignment.Center;
                r.Width = thickness;
                r.Height = -(2f * inset);
                r.WidthUnits = DimensionUnitType.Absolute;
                r.HeightUnits = DimensionUnitType.RelativeToParent;
                break;
            case Edge.Right:
                r.X = -inset;
                r.Y = 0;
                r.XUnits = GeneralUnitType.PixelsFromLarge;
                r.YUnits = GeneralUnitType.PixelsFromMiddle;
                r.XOrigin = HorizontalAlignment.Right;
                r.YOrigin = VerticalAlignment.Center;
                r.Width = thickness;
                r.Height = -(2f * inset);
                r.WidthUnits = DimensionUnitType.Absolute;
                r.HeightUnits = DimensionUnitType.RelativeToParent;
                break;
        }
        return r;
    }
}
