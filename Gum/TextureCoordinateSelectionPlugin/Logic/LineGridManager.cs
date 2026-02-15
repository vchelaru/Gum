using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Math.Geometry;
using Color = System.Drawing.Color;

namespace TextureCoordinateSelectionPlugin.Logic;

public class LineGridManager : IVisualOverlayManager
{
    private LineGrid _lineGrid;

    public bool IsVisible { get; set; }
    public int GridSize { get; set; }
    public Texture2D CurrentTexture { get; set; }

    public void Initialize(SystemManagers systemManagers)
    {
        _lineGrid = new LineGrid(systemManagers);
        _lineGrid.ColumnWidth = 16;
        _lineGrid.ColumnCount = 16;

        _lineGrid.RowWidth = 16;
        _lineGrid.RowCount = 16;

        _lineGrid.Visible = true;
        _lineGrid.Z = 1;

        var alpha = (int)(.2f * 0xFF);

        // premultiplied
        _lineGrid.Color = Color.FromArgb(alpha, alpha, alpha, alpha);

        systemManagers.Renderer.MainLayer.Add(_lineGrid);
    }

    public void Refresh()
    {
        _lineGrid.Visible = IsVisible;

        _lineGrid.ColumnWidth = GridSize;
        _lineGrid.RowWidth = GridSize;

        if (CurrentTexture != null)
        {
            var totalWidth = CurrentTexture.Width;

            var columnCount = (totalWidth / _lineGrid.ColumnWidth);
            if (columnCount != (int)columnCount)
            {
                columnCount++;
            }

            _lineGrid.ColumnCount = (int)columnCount;


            var totalHeight = CurrentTexture.Height;
            var rowCount = (totalHeight / _lineGrid.RowWidth);
            if (rowCount != (int)rowCount)
            {
                rowCount++;
            }

            _lineGrid.RowCount = (int)rowCount;
        }
    }
}
