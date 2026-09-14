using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays a highlight rectangle around
/// the currently selected polygon vertex.
/// </summary>
public class SelectedPointHighlightVisual : EditorVisualBase
{
    private readonly LineRectangle _highlightRectangle;

    private const float RadiusAtNoZoom = 5;
    private const float PaddingAtNoZoom = 6;
    private const float LinePixelWidth = 3;

    /// <summary>
    /// The index of the currently selected point, or null for no selection.
    /// </summary>
    public int? SelectedIndex { get; set; }

    private float NodeDisplayWidth => RadiusAtNoZoom * 2 / Zoom;

    public SelectedPointHighlightVisual(EditorContext context, Layer layer) : base(context)
    {
        _highlightRectangle = new LineRectangle();
        _highlightRectangle.Color = Color.Magenta;
        _highlightRectangle.IsDotted = false;
        _highlightRectangle.LinePixelWidth = LinePixelWidth;
        _highlightRectangle.Visible = false;

        ShapeManager.Self.Add(_highlightRectangle, layer);
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _highlightRectangle.Visible = isVisible;
    }

    public override void Update()
    {
        var selectedPolygon = Context.SelectedObjects
            .FirstOrDefault()?.RenderableComponent as LinePolygon;

        var hasSelection = SelectedIndex != null &&
                          selectedPolygon != null &&
                          SelectedIndex < selectedPolygon.PointCount;

        _highlightRectangle.Visible = hasSelection;

        if (hasSelection)
        {
            UpdatePosition(selectedPolygon!, SelectedIndex!.Value);
        }
    }

    private void UpdatePosition(LinePolygon polygon, int index)
    {
        var padding = PaddingAtNoZoom / Zoom;
        var highlightSize = NodeDisplayWidth + padding;

        _highlightRectangle.Width = highlightSize;
        _highlightRectangle.Height = highlightSize;

        var vertexPosition = polygon.AbsolutePointAt(index);

        _highlightRectangle.X = vertexPosition.X - highlightSize / 2;
        _highlightRectangle.Y = vertexPosition.Y - highlightSize / 2;
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        // Don't clear the point selection here - let the handler manage it via OnSelectionChanged
        // The visual will update its visibility in Update() based on whether SelectedIndex is valid
    }

    public override void Destroy()
    {
        ShapeManager.Self.Remove(_highlightRectangle);
    }
}
