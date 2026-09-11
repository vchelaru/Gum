using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// Visual component that displays draggable nodes for polygon vertices.
/// Each vertex of the polygon gets a small rectangle that can be grabbed.
/// </summary>
public class PolygonPointNodesVisual : EditorVisualBase
{
    private readonly List<SolidRectangle> _pointNodes = new();
    private readonly Layer _layer;

    private const float RadiusAtNoZoom = 5;

    /// <summary>
    /// The index of the currently highlighted node (cursor over), or null.
    /// </summary>
    public int? HighlightedIndex { get; set; }

    /// <summary>
    /// The index of the currently grabbed node (being dragged), or null.
    /// </summary>
    public int? GrabbedIndex { get; set; }

    /// <summary>
    /// The index of the currently selected node, or null.
    /// </summary>
    public int? SelectedIndex { get; set; }

    private float NodeDisplayWidth => RadiusAtNoZoom * 2 / Zoom;

    public PolygonPointNodesVisual(EditorContext context, Layer layer) : base(context)
    {
        _layer = layer;
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        foreach (var node in _pointNodes)
        {
            node.Visible = isVisible;
        }
    }

    public override void Update()
    {
        if (!Visible) return;

        UpdateNodePositions();
        UpdateNodeColors();
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        var selectedPolygon = selectedObjects
            .FirstOrDefault()?.RenderableComponent as LinePolygon;

        UpdateNodeCount(selectedPolygon?.PointCount ?? 0);

        if (selectedPolygon != null)
        {
            Visible = true;
            UpdateNodePositions();
        }
        else
        {
            Visible = false;
        }
    }

    private void UpdateNodeCount(int neededCount)
    {
        // Create needed nodes
        while (_pointNodes.Count < neededCount)
        {
            var rectangle = new SolidRectangle();
            rectangle.Width = NodeDisplayWidth;
            rectangle.Height = NodeDisplayWidth;
            rectangle.Visible = Visible; // Inherit visibility from parent visual
            ShapeManager.Self.Add(rectangle, _layer);
            _pointNodes.Add(rectangle);
        }

        // Destroy excess nodes
        while (_pointNodes.Count > neededCount)
        {
            var node = _pointNodes.Last();
            ShapeManager.Self.Remove(node);
            _pointNodes.Remove(node);
        }
    }

    private void UpdateNodePositions()
    {
        var selectedPolygon = Context.SelectedObjects
            .FirstOrDefault()?.RenderableComponent as LinePolygon;

        if (selectedPolygon == null) return;

        var nodeDimension = NodeDisplayWidth;

        for (int i = 0; i < selectedPolygon.PointCount && i < _pointNodes.Count; i++)
        {
            var point = selectedPolygon.AbsolutePointAt(i);

            _pointNodes[i].X = point.X - nodeDimension / 2;
            _pointNodes[i].Y = point.Y - nodeDimension / 2;
            _pointNodes[i].Width = nodeDimension;
            _pointNodes[i].Height = nodeDimension;
        }
    }

    private void UpdateNodeColors()
    {
        for (int i = 0; i < _pointNodes.Count; i++)
        {
            bool isHighlighted = i == HighlightedIndex || i == GrabbedIndex;

            // Also highlight last point if first is highlighted (for closed polygons)
            if (HighlightedIndex == 0 && i == _pointNodes.Count - 1)
            {
                isHighlighted = true;
            }

            _pointNodes[i].Color = isHighlighted ? Color.Yellow : Color.Gray;
        }
    }

    public override void Destroy()
    {
        foreach (var node in _pointNodes)
        {
            ShapeManager.Self.Remove(node);
        }
        _pointNodes.Clear();
    }

    #region Query Methods (for PolygonPointInputHandler)

    /// <summary>
    /// Gets the index of the point at the specified coordinates, or null.
    /// </summary>
    public int? GetIndexOver(float worldX, float worldY)
    {
        var effectiveRadius = RadiusAtNoZoom / Zoom;
        for (int i = 0; i < _pointNodes.Count; i++)
        {
            var node = _pointNodes[i];
            var left = node.X;
            var top = node.Y;
            var right = left + effectiveRadius * 2;
            var bottom = top + effectiveRadius * 2;

            if (worldX > left && worldX < right &&
                worldY > top && worldY < bottom)
            {
                return i;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the number of point nodes.
    /// </summary>
    public int PointCount => _pointNodes.Count;

    #endregion
}
