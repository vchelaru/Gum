using Gum.Wireframe.Editors;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace Gum.Plugins.InternalPlugins.EditorTab.Views;

/// <summary>
/// Owns the LineGrid overlay drawn on the main editor canvas, keeping it aligned to the
/// canvas-space grid and covering the camera's visible area as it pans/zooms (issue #4137).
/// </summary>
public class GridOverlayManager
{
    private readonly LineGrid _lineGrid;

    public bool IsVisible { get; set; }
    public int GridSize { get; set; } = 16;

    public GridOverlayManager(SystemManagers systemManagers)
    {
        _lineGrid = new LineGrid(systemManagers);
        _lineGrid.Z = 1;

        var alpha = (int)(.2f * 0xFF);
        // premultiplied
        _lineGrid.Color = Color.FromArgb(alpha, alpha, alpha, alpha);
    }

    public void AddToLayer(Layer layer)
    {
        ShapeManager.Self.Add(_lineGrid, layer);
    }

    /// <summary>
    /// Recomputes the overlay's position and cell counts from the current camera view. Call after
    /// <see cref="IsVisible"/>/<see cref="GridSize"/> change and whenever the camera pans or zooms.
    /// </summary>
    public void Refresh(Camera camera)
    {
        _lineGrid.Visible = IsVisible && GridSize > 0;

        if (!_lineGrid.Visible)
        {
            return;
        }

        GridOverlayCalculator.Calculate(
            camera.AbsoluteLeft, camera.AbsoluteTop, camera.AbsoluteRight, camera.AbsoluteBottom, GridSize,
            out float originX, out float originY, out int columnCount, out int rowCount);

        var ipso = (IPositionedSizedObject)_lineGrid;
        ipso.X = originX;
        ipso.Y = originY;

        _lineGrid.ColumnWidth = GridSize;
        _lineGrid.RowWidth = GridSize;
        _lineGrid.ColumnCount = columnCount;
        _lineGrid.RowCount = rowCount;
    }
}
