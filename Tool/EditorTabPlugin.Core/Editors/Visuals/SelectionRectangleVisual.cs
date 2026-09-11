using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Tool-side (XNALIKE-rendering) implementation of <see cref="ISelectionRectangleVisual"/> —
/// the marquee/rubber-band rectangle <see cref="RectangleSelector"/> (now headless, in
/// Gum.Presentation) draws while dragging. Owns all styling (color, dotted line, thickness);
/// <see cref="RectangleSelector"/> only ever sets geometry/visibility through the interface.
/// </summary>
public class SelectionRectangleVisual : ISelectionRectangleVisual
{
    private readonly LineRectangle _selectionRectangle;

    public bool Visible
    {
        get => _selectionRectangle.Visible;
        set => _selectionRectangle.Visible = value;
    }

    public float X
    {
        get => _selectionRectangle.X;
        set => _selectionRectangle.X = value;
    }

    public float Y
    {
        get => _selectionRectangle.Y;
        set => _selectionRectangle.Y = value;
    }

    public float Width
    {
        get => _selectionRectangle.Width;
        set => _selectionRectangle.Width = value;
    }

    public float Height
    {
        get => _selectionRectangle.Height;
        set => _selectionRectangle.Height = value;
    }

    public SelectionRectangleVisual(Layer overlayLayer)
    {
        _selectionRectangle = new LineRectangle();
        _selectionRectangle.Color = Color.DodgerBlue;
        _selectionRectangle.IsDotted = true;
        _selectionRectangle.LinePixelWidth = 1;
        _selectionRectangle.Visible = false;

        ShapeManager.Self.Add(_selectionRectangle, overlayLayer);
    }

    public void Destroy()
    {
        ShapeManager.Self.Remove(_selectionRectangle);
    }
}
