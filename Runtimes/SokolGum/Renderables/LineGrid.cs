using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Sokol.SGP;
using SokolGum;

namespace Gum.Renderables;

/// <summary>
/// Rectangular grid of horizontal and vertical lines, typically used for
/// debug overlays. Size is <c>ColumnCount × ColumnWidth</c> wide and
/// <c>RowCount × RowWidth</c> tall. All cell lines share one color.
/// </summary>
public sealed class LineGrid : RenderableBase, IRenderableIpso
{
    public float RowWidth    { get; set; } = 16f;
    public float ColumnWidth { get; set; } = 16f;
    public int   RowCount    { get; set; } = 8;
    public int   ColumnCount { get; set; } = 8;

    public Color Color = Color.White;

    public int Red   { get => Color.R; set => Color.R = (byte)value; }
    public int Green { get => Color.G; set => Color.G = (byte)value; }
    public int Blue  { get => Color.B; set => Color.B = (byte)value; }
    public new int Alpha { get => Color.A; set => Color.A = (byte)value; }
    int IRenderableIpso.Alpha => Color.A;

    public LineGrid() { }
    public LineGrid(SystemManagers? _) { }

    public override void Render(ISystemManagers? managers)
    {
        if (!Visible || RowCount <= 0 || ColumnCount <= 0) return;

        float ox = this.GetAbsoluteLeft();
        float oy = this.GetAbsoluteTop();
        float totalW = ColumnCount * ColumnWidth;
        float totalH = RowCount    * RowWidth;

        sgp_set_color(Color.R / 255f, Color.G / 255f, Color.B / 255f, Color.A / 255f);

        // Horizontal rules (RowCount + 1 lines including top + bottom edges).
        for (int r = 0; r <= RowCount; r++)
        {
            float y = oy + r * RowWidth;
            sgp_draw_line(ox, y, ox + totalW, y);
        }
        // Vertical rules (ColumnCount + 1 lines).
        for (int c = 0; c <= ColumnCount; c++)
        {
            float x = ox + c * ColumnWidth;
            sgp_draw_line(x, oy, x, oy + totalH);
        }

        sgp_reset_color();
    }
}
