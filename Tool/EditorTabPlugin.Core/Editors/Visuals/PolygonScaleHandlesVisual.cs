using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace Gum.Wireframe.Editors.Visuals;

/// <summary>
/// The 8 handles around a selected polygon's bounding box, used to scale its points (#5934).
/// The box is the extent of the points, not the polygon's Width/Height, which measure from the
/// origin.
/// </summary>
public class PolygonScaleHandlesVisual : EditorVisualBase, IResizeHandlesVisual
{
    private const float GapAtNoZoom = 4;

    private readonly ResizeHandles _handles;

    float IResizeHandlesVisual.HandlesWidth => _handles.Width;
    float IResizeHandlesVisual.HandlesHeight => _handles.Height;
    ResizeSide IResizeHandlesVisual.GetSideOver(float worldX, float worldY) => _handles.GetSideOver(worldX, worldY);

    public PolygonScaleHandlesVisual(EditorContext context, Color lineColor) : base(context)
    {
        _handles = new ResizeHandles(OverlayLayer, lineColor, context.DisplayScale);
        _handles.ShowOrigin = false;
        Visible = false;
    }

    protected override void OnVisibilityChanged(bool isVisible)
    {
        _handles.Visible = isVisible;
    }

    public override void Update()
    {
        if (!Visible)
        {
            return;
        }

        UpdateHandles();
    }

    public override void UpdateToSelection(ICollection<GraphicalUiElement> selectedObjects)
    {
        LinePolygon? polygon = selectedObjects.Count == 1
            ? selectedObjects.First().RenderableComponent as LinePolygon
            : null;

        if (polygon == null || polygon.PointCount < 2 || Context.IsSelectionLocked())
        {
            Visible = false;
            return;
        }

        Visible = true;
        UpdateHandles();
    }

    private void UpdateHandles()
    {
        if (Context.SelectedObjects.FirstOrDefault()?.RenderableComponent is not LinePolygon polygon ||
            polygon.PointCount == 0)
        {
            return;
        }

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        for (int i = 0; i < polygon.PointCount; i++)
        {
            Vector2 point = polygon.PointAt(i);
            minX = MathF.Min(minX, point.X);
            minY = MathF.Min(minY, point.Y);
            maxX = MathF.Max(maxX, point.X);
            maxY = MathF.Max(maxY, point.Y);
        }

        // Held off the points' extent so a click on the polygon's own edge still reaches the
        // add-point marker, and a point sitting on the extent stays grabbable.
        float gap = ToWorldOverlaySize(GapAtNoZoom);
        minX -= gap;
        minY -= gap;
        maxX += gap;
        maxY += gap;

        // Same local-to-world transform as LinePolygon.AbsolutePointAt.
        Matrix4x4 rotation = polygon.GetAbsoluteRotationMatrix();
        Vector2 topLeft = minX * new Vector2(rotation.M11, rotation.M12) +
            minY * new Vector2(rotation.M21, rotation.M22) +
            new Vector2(polygon.GetAbsoluteX(), polygon.GetAbsoluteY());

        _handles.SetBounds(topLeft.X, topLeft.Y, maxX - minX, maxY - minY, polygon.GetAbsoluteRotation());
        _handles.UpdateHandleSizes();
    }

    public override void Destroy()
    {
        _handles.Destroy();
    }
}
