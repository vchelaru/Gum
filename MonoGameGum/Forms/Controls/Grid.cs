using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

// EXPERIMENTAL: This control is experimental and subject to breaking changes.

/// <summary>
/// A WPF-style grid control that arranges children in rows and columns.
/// Internally translates grid definitions into Gum's nested stack layout.
/// This control is experimental and may change in future releases.
/// </summary>
public class Grid :
#if FRB
    FrameworkElement
#else
    Gum.Forms.Controls.FrameworkElement
#endif
{
    #region Fields/Properties

    private Dictionary<FrameworkElement, (int Row, int Column)> _cellPlacements;
    private List<InteractiveGue> _rowContainers;

    /// <summary>
    /// The column definitions for this grid. Changes to this collection trigger a layout refresh.
    /// </summary>
    public ObservableCollection<ColumnDefinition> ColumnDefinitions { get; }

    /// <summary>
    /// The row definitions for this grid. Changes to this collection trigger a layout refresh.
    /// </summary>
    public ObservableCollection<RowDefinition> RowDefinitions { get; }

    #endregion

    /// <summary>
    /// Creates a new Grid using default visuals that fill the parent.
    /// </summary>
    public Grid() :
        base(new InteractiveGue(new InvisibleRenderable()))
    {
        _cellPlacements = new Dictionary<FrameworkElement, (int Row, int Column)>();
        _rowContainers = new List<InteractiveGue>();

        ColumnDefinitions = new ObservableCollection<ColumnDefinition>();
        RowDefinitions = new ObservableCollection<RowDefinition>();

        ColumnDefinitions.CollectionChanged += (_, _) => RefreshLayout();
        RowDefinitions.CollectionChanged += (_, _) => RefreshLayout();

        IsVisible = true;

        Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Visual.Width = 0;
        Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Visual.Height = 0;
        Visual.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
    }

    /// <summary>
    /// Creates a new Grid using the specified visual.
    /// </summary>
    /// <param name="visual">The visual to use.</param>
    public Grid(InteractiveGue visual) : base(visual)
    {
        _cellPlacements = new Dictionary<FrameworkElement, (int Row, int Column)>();
        _rowContainers = new List<InteractiveGue>();

        ColumnDefinitions = new ObservableCollection<ColumnDefinition>();
        RowDefinitions = new ObservableCollection<RowDefinition>();

        ColumnDefinitions.CollectionChanged += (_, _) => RefreshLayout();
        RowDefinitions.CollectionChanged += (_, _) => RefreshLayout();
    }

    /// <summary>
    /// Adds a child element to the grid at the specified row and column.
    /// </summary>
    /// <param name="child">The child element to add.</param>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    public void AddChild(FrameworkElement child, int row, int column)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        _cellPlacements[child] = (row, column);
        RefreshLayout();
    }

    /// <summary>
    /// Removes a child element from the grid.
    /// </summary>
    /// <param name="child">The child element to remove.</param>
    public void RemoveChild(FrameworkElement child)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        if (_cellPlacements.Remove(child))
        {
            if (child.Visual.Parent != null)
            {
                child.Visual.Parent.Children.Remove(child.Visual);
            }
            RefreshLayout();
        }
    }

    private void RefreshLayout()
    {
        int rowCount = Math.Max(RowDefinitions.Count, 1);
        int columnCount = Math.Max(ColumnDefinitions.Count, 1);

        // Ensure we have the right number of row containers
        while (_rowContainers.Count < rowCount)
        {
            InteractiveGue rowContainer = new InteractiveGue(new InvisibleRenderable());
            rowContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            rowContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            rowContainer.Width = 0;
            _rowContainers.Add(rowContainer);
            Visual.Children.Add(rowContainer);
        }

        while (_rowContainers.Count > rowCount)
        {
            InteractiveGue last = _rowContainers[_rowContainers.Count - 1];
            Visual.Children.Remove(last);
            _rowContainers.RemoveAt(_rowContainers.Count - 1);
        }

        // Configure row container heights
        for (int r = 0; r < rowCount; r++)
        {
            InteractiveGue rowContainer = _rowContainers[r];
            GridLength rowHeight = (r < RowDefinitions.Count)
                ? RowDefinitions[r].Height
                : new GridLength(1, GridUnitType.Star);

            ApplyRowHeight(rowContainer, rowHeight);
        }

        // Clear all children from row containers so we can rebuild
        for (int r = 0; r < rowCount; r++)
        {
            _rowContainers[r].Children.Clear();
        }

        // Build cell containers for each cell in the grid
        for (int r = 0; r < rowCount; r++)
        {
            for (int c = 0; c < columnCount; c++)
            {
                InteractiveGue cellContainer = new InteractiveGue(new InvisibleRenderable());
                cellContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                cellContainer.Height = 0;

                GridLength columnWidth = (c < ColumnDefinitions.Count)
                    ? ColumnDefinitions[c].Width
                    : new GridLength(1, GridUnitType.Star);

                ApplyColumnWidth(cellContainer, columnWidth);

                // Find children placed in this cell. Row/column values beyond the
                // defined count are clamped to the last valid index, matching WPF behavior.
                foreach (KeyValuePair<FrameworkElement, (int Row, int Column)> kvp in _cellPlacements)
                {
                    int clampedRow = Math.Min(kvp.Value.Row, rowCount - 1);
                    int clampedColumn = Math.Min(kvp.Value.Column, columnCount - 1);

                    if (clampedRow == r && clampedColumn == c)
                    {
                        // Remove from previous parent if needed
                        if (kvp.Key.Visual.Parent != null)
                        {
                            kvp.Key.Visual.Parent.Children.Remove(kvp.Key.Visual);
                        }
                        cellContainer.Children.Add(kvp.Key.Visual);
                    }
                }

                _rowContainers[r].Children.Add(cellContainer);
            }
        }

        // Second pass: unify Auto column widths across all rows.
        // Each Auto column should be as wide as the widest content in that column,
        // matching WPF behavior. We first let RelativeToChildren compute per-cell
        // widths, then find the max per column and apply it as Absolute to all cells.
        ApplyAutoColumnWidths(rowCount, columnCount);
    }

    private void ApplyAutoColumnWidths(int rowCount, int columnCount)
    {
        for (int c = 0; c < columnCount; c++)
        {
            GridLength columnWidth = (c < ColumnDefinitions.Count)
                ? ColumnDefinitions[c].Width
                : new GridLength(1, GridUnitType.Star);

            if (!columnWidth.IsAuto)
            {
                continue;
            }

            // Find the max absolute width for this column across all rows
            float maxWidth = 0;
            for (int r = 0; r < rowCount; r++)
            {
                InteractiveGue cell = (InteractiveGue)_rowContainers[r].Children[c];
                cell.UpdateLayout();
                float cellWidth = cell.GetAbsoluteWidth();
                if (cellWidth > maxWidth)
                {
                    maxWidth = cellWidth;
                }
            }

            // Apply the unified width to all cells in this column
            for (int r = 0; r < rowCount; r++)
            {
                InteractiveGue cell = (InteractiveGue)_rowContainers[r].Children[c];
                cell.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                cell.Width = maxWidth;
            }
        }
    }

    private void ApplyRowHeight(InteractiveGue rowContainer, GridLength height)
    {
        if (height.IsAuto)
        {
            rowContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            rowContainer.Height = 0;
        }
        else if (height.IsStar)
        {
            rowContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
            rowContainer.Height = height.Value;
        }
        else
        {
            rowContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            rowContainer.Height = height.Value;
        }
    }

    private void ApplyColumnWidth(InteractiveGue cellContainer, GridLength width)
    {
        if (width.IsAuto)
        {
            // Initially set to RelativeToChildren so layout can compute the natural width.
            // ApplyAutoColumnWidths will unify these to Absolute after all cells are built.
            cellContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            cellContainer.Width = 0;
        }
        else if (width.IsStar)
        {
            cellContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
            cellContainer.Width = width.Value;
        }
        else
        {
            cellContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            cellContainer.Width = width.Value;
        }
    }
}
