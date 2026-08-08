using System;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Sizes and positions the grid overlay to cover a camera's visible world rect, aligned to the
/// canvas-space grid so it doesn't visually shift as the camera pans or zooms.
/// </summary>
public static class GridOverlayCalculator
{
    /// <summary>
    /// Computes the grid-aligned origin and cell counts needed to cover the given visible world
    /// rect. <paramref name="originX"/>/<paramref name="originY"/> are the world position of the
    /// first (top-left) grid line at or before the visible rect; <paramref name="columnCount"/>/
    /// <paramref name="rowCount"/> include one extra line so the rect's right/bottom edge is
    /// always covered.
    /// </summary>
    public static void Calculate(
        float visibleLeft, float visibleTop, float visibleRight, float visibleBottom, float gridSize,
        out float originX, out float originY, out int columnCount, out int rowCount)
    {
        if (gridSize <= 0)
        {
            originX = 0;
            originY = 0;
            columnCount = 0;
            rowCount = 0;
            return;
        }

        originX = GridSnapper.Snap(visibleLeft, gridSize);
        originY = GridSnapper.Snap(visibleTop, gridSize);

        float visibleWidth = visibleRight - originX;
        float visibleHeight = visibleBottom - originY;

        columnCount = (int)Math.Ceiling(visibleWidth / gridSize) + 1;
        rowCount = (int)Math.Ceiling(visibleHeight / gridSize) + 1;
    }
}
