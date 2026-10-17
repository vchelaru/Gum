using System;
using Avalonia;
using Avalonia.Controls;
using AvaloniaDataUi.Controls;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi;

/// <summary>
/// A <see cref="PairedRow"/>: two half-width cells side by side, each framed by the grid's
/// <see cref="DataUiGrid.RowDecorator"/> and holding an ordinary editor with a label only as wide as
/// its text. Each cell keeps its own marker, detail text and context menu. When the row is too narrow
/// for both cells to show their label and a usable field, the cells stack, one above the other.
/// </summary>
internal sealed class PairedRowView : Panel
{
    private readonly Control[] _cells;
    private readonly SingleDataUiContainer[] _containers;
    private bool _isStacked;

    public PairedRowView(DataUiGrid grid, PairedRow row)
    {
        _cells = new Control[2];
        _containers = new SingleDataUiContainer[2];
        AddCell(grid, row.First, 0);
        AddCell(grid, row.Second, 1);
    }

    private void AddCell(DataUiGrid grid, InstanceMember member, int index)
    {
        SingleDataUiContainer container = new SingleDataUiContainer(grid) { IsCompact = true };
        Control cell = grid.RowDecorator?.Invoke(container) ?? container;
        // The decorator wraps the container, so the member goes on the whole cell for both to inherit.
        cell.DataContext = member;
        _cells[index] = cell;
        _containers[index] = container;
        Children.Add(cell);
    }

    /// <summary>Whether the cells are stacked rather than side by side, for tests.</summary>
    internal bool IsStacked => _isStacked;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // What both cells need side by side: each one's frame plus its label and field floor. Detail
        // text is left out, since it wraps to whatever width the cell has.
        double needed = 0;
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i].Measure(Size.Infinity);
            double frame = _cells[i].DesiredSize.Width - _containers[i].DesiredSize.Width;
            double content = (_containers[i].Displayer as DataUiDisplayBase)?.CompactMinimumWidth ?? 0;
            needed += frame + content;
        }

        _isStacked = !double.IsInfinity(availableSize.Width) && needed > availableSize.Width;

        if (_isStacked)
        {
            double width = 0;
            double height = 0;
            foreach (Control cell in _cells)
            {
                cell.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                width = Math.Max(width, cell.DesiredSize.Width);
                height += cell.DesiredSize.Height;
            }
            return new Size(width, height);
        }

        double half = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / 2;
        double rowHeight = 0;
        double rowWidth = 0;
        foreach (Control cell in _cells)
        {
            cell.Measure(new Size(half, double.PositiveInfinity));
            rowWidth += cell.DesiredSize.Width;
            rowHeight = Math.Max(rowHeight, cell.DesiredSize.Height);
        }
        return new Size(double.IsInfinity(half) ? rowWidth : availableSize.Width, rowHeight);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_isStacked)
        {
            double top = 0;
            foreach (Control cell in _cells)
            {
                cell.Arrange(new Rect(0, top, finalSize.Width, cell.DesiredSize.Height));
                top += cell.DesiredSize.Height;
            }
            return finalSize;
        }

        // Both cells take the whole row's height, so their separators line up under a taller cell.
        double half = finalSize.Width / 2;
        _cells[0].Arrange(new Rect(0, 0, half, finalSize.Height));
        _cells[1].Arrange(new Rect(half, 0, half, finalSize.Height));
        return finalSize;
    }
}
