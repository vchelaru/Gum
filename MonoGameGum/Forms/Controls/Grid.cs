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

    private bool _isRefreshingLayout;
    private Dictionary<GraphicalUiElement, EventHandler> _sizeChangedHandlers;
    private Dictionary<GraphicalUiElement, EventHandler<GraphicalUiElement.ParentChangedEventArgs>> _parentChangedHandlers;
    private Dictionary<FrameworkElement, (int Row, int Column)> _cellPlacements;
    private Dictionary<GraphicalUiElement, (int Row, int Column)> _gueCellPlacements;
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
        _isRefreshingLayout = false;
        _sizeChangedHandlers = new Dictionary<GraphicalUiElement, EventHandler>();
        _parentChangedHandlers = new Dictionary<GraphicalUiElement, EventHandler<GraphicalUiElement.ParentChangedEventArgs>>();
        _cellPlacements = new Dictionary<FrameworkElement, (int Row, int Column)>();
        _gueCellPlacements = new Dictionary<GraphicalUiElement, (int Row, int Column)>();
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
        _isRefreshingLayout = false;
        _sizeChangedHandlers = new Dictionary<GraphicalUiElement, EventHandler>();
        _parentChangedHandlers = new Dictionary<GraphicalUiElement, EventHandler<GraphicalUiElement.ParentChangedEventArgs>>();
        _cellPlacements = new Dictionary<FrameworkElement, (int Row, int Column)>();
        _gueCellPlacements = new Dictionary<GraphicalUiElement, (int Row, int Column)>();
        _rowContainers = new List<InteractiveGue>();

        ColumnDefinitions = new ObservableCollection<ColumnDefinition>();
        RowDefinitions = new ObservableCollection<RowDefinition>();

        ColumnDefinitions.CollectionChanged += (_, _) => RefreshLayout();
        RowDefinitions.CollectionChanged += (_, _) => RefreshLayout();
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// Always thrown. Grid requires explicit row and column placement.
    /// </exception>
    public override void AddChild(FrameworkElement child)
        => throw new NotSupportedException("Grid requires row and column placement. Call AddChild(child, row, column) instead.");

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// Always thrown. Grid requires explicit row and column placement.
    /// </exception>
    public override void AddChild(GraphicalUiElement child)
        => throw new NotSupportedException("Grid requires row and column placement. Call AddChild(child, row, column) instead.");

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
        if (row < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Row index cannot be negative.");
        }
        if (column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(column), "Column index cannot be negative.");
        }

        _cellPlacements[child] = (row, column);
        SubscribeToChildEvents(child.Visual);
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
            UnsubscribeFromChildEvents(child.Visual);
            if (child.Visual.Parent != null)
            {
                child.Visual.Parent.Children.Remove(child.Visual);
            }
            RefreshLayout();
        }
    }

    /// <summary>
    /// Adds a raw <see cref="GraphicalUiElement"/> child to the grid at the specified row and column.
    /// </summary>
    /// <param name="child">The child element to add.</param>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    public void AddChild(GraphicalUiElement child, int row, int column)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }
        if (row < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Row index cannot be negative.");
        }
        if (column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(column), "Column index cannot be negative.");
        }

        _gueCellPlacements[child] = (row, column);
        SubscribeToChildEvents(child);
        RefreshLayout();
    }

    /// <summary>
    /// Removes a raw <see cref="GraphicalUiElement"/> child from the grid.
    /// </summary>
    /// <param name="child">The child element to remove.</param>
    public void RemoveChild(GraphicalUiElement child)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        if (_gueCellPlacements.Remove(child))
        {
            UnsubscribeFromChildEvents(child);
            if (child.Parent != null)
            {
                child.Parent.Children.Remove(child);
            }
            RefreshLayout();
        }
    }

    /// <summary>
    /// Adds a row with the specified height to the grid.
    /// </summary>
    public void AddRow(GridLength height) =>
        RowDefinitions.Add(new RowDefinition(height));

    /// <summary>
    /// Adds a column with the specified width to the grid.
    /// </summary>
    public void AddColumn(GridLength width) =>
        ColumnDefinitions.Add(new ColumnDefinition(width));

    private void RefreshLayout()
    {
        _isRefreshingLayout = true;
        try
        {
            RefreshLayoutCore();
        }
        finally
        {
            _isRefreshingLayout = false;
        }
    }

    private void RefreshLayoutCore()
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

        // Configure row container heights and widths
        for (int r = 0; r < rowCount; r++)
        {
            InteractiveGue rowContainer = _rowContainers[r];
            RowDefinition rowDef = r < RowDefinitions.Count ? RowDefinitions[r] : null;
            GridLength rowHeight = rowDef?.Height ?? new GridLength(1, GridUnitType.Star);
            float minHeight = rowDef?.MinHeight ?? 0f;
            float maxHeight = rowDef?.MaxHeight ?? float.PositiveInfinity;

            ApplyRowHeight(rowContainer, rowHeight, minHeight, maxHeight);

            rowContainer.WidthUnits = Visual.WidthUnits == global::Gum.DataTypes.DimensionUnitType.RelativeToChildren
                ? global::Gum.DataTypes.DimensionUnitType.RelativeToChildren
                : global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            rowContainer.Width = 0;
        }

        // Clear all children from row containers so we can rebuild
        for (int r = 0; r < rowCount; r++)
        {
            _rowContainers[r].Children.Clear();
        }

        // Build cell containers for each cell in the grid
        for (int r = 0; r < rowCount; r++)
        {
            RowDefinition cellRowDef = r < RowDefinitions.Count ? RowDefinitions[r] : null;
            bool isAutoRow = cellRowDef?.Height.IsAuto ?? false;

            for (int c = 0; c < columnCount; c++)
            {
                InteractiveGue cellContainer = new InteractiveGue(new InvisibleRenderable());
                cellContainer.HeightUnits = isAutoRow
                    ? global::Gum.DataTypes.DimensionUnitType.RelativeToChildren
                    : global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                cellContainer.Height = 0;

                ColumnDefinition colDef = c < ColumnDefinitions.Count ? ColumnDefinitions[c] : null;
                GridLength columnWidth = colDef?.Width ?? new GridLength(1, GridUnitType.Star);
                float minWidth = colDef?.MinWidth ?? 0f;
                float maxWidth = colDef?.MaxWidth ?? float.PositiveInfinity;

                ApplyColumnWidth(cellContainer, columnWidth, minWidth, maxWidth);

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

                foreach (KeyValuePair<GraphicalUiElement, (int Row, int Column)> kvp in _gueCellPlacements)
                {
                    int clampedRow = Math.Min(kvp.Value.Row, rowCount - 1);
                    int clampedColumn = Math.Min(kvp.Value.Column, columnCount - 1);

                    if (clampedRow == r && clampedColumn == c)
                    {
                        if (kvp.Key.Parent != null)
                        {
                            kvp.Key.Parent.Children.Remove(kvp.Key);
                        }
                        cellContainer.Children.Add(kvp.Key);
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

        // Third pass: clamp Auto row heights to MinHeight/MaxHeight constraints.
        ApplyMinMaxAutoRowHeights(rowCount);
    }

    private void ApplyAutoColumnWidths(int rowCount, int columnCount)
    {
        for (int c = 0; c < columnCount; c++)
        {
            ColumnDefinition colDef = c < ColumnDefinitions.Count ? ColumnDefinitions[c] : null;
            GridLength columnWidth = colDef?.Width ?? new GridLength(1, GridUnitType.Star);

            if (!columnWidth.IsAuto)
            {
                continue;
            }

            // Find the max absolute width for this column across all rows
            float maxContentWidth = 0;
            for (int r = 0; r < rowCount; r++)
            {
                InteractiveGue cell = (InteractiveGue)_rowContainers[r].Children[c];
                cell.UpdateLayout();
                float cellWidth = cell.GetAbsoluteWidth();
                if (cellWidth > maxContentWidth)
                {
                    maxContentWidth = cellWidth;
                }
            }

            // Clamp to MinWidth/MaxWidth if defined
            if (colDef != null)
            {
                maxContentWidth = Math.Max(colDef.MinWidth, Math.Min(colDef.MaxWidth, maxContentWidth));
            }

            // Apply the unified width to all cells in this column
            for (int r = 0; r < rowCount; r++)
            {
                InteractiveGue cell = (InteractiveGue)_rowContainers[r].Children[c];
                cell.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                cell.Width = maxContentWidth;
            }
        }
    }

    private void ApplyMinMaxAutoRowHeights(int rowCount)
    {
        for (int r = 0; r < rowCount; r++)
        {
            if (r >= RowDefinitions.Count) continue;
            RowDefinition def = RowDefinitions[r];
            if (!def.Height.IsAuto) continue;
            if (def.MinHeight == 0f && float.IsPositiveInfinity(def.MaxHeight)) continue;

            InteractiveGue rowContainer = _rowContainers[r];
            rowContainer.UpdateLayout();
            float computedHeight = rowContainer.GetAbsoluteHeight();
            float clamped = Math.Max(def.MinHeight, Math.Min(def.MaxHeight, computedHeight));
            if (clamped != computedHeight)
            {
                rowContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                rowContainer.Height = clamped;
            }
        }
    }

    private void ApplyRowHeight(InteractiveGue rowContainer, GridLength height, float minHeight, float maxHeight)
    {
        if (height.IsAuto)
        {
            rowContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            rowContainer.Height = 0;
            rowContainer.MinHeight = null;
            rowContainer.MaxHeight = null;
            // Min/Max for Auto rows are applied in ApplyMinMaxAutoRowHeights after cells are built.
        }
        else if (height.IsStar)
        {
            rowContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
            rowContainer.Height = height.Value;
            rowContainer.MinHeight = minHeight > 0f ? minHeight : (float?)null;
            rowContainer.MaxHeight = float.IsPositiveInfinity(maxHeight) ? (float?)null : maxHeight;
        }
        else
        {
            float clamped = Math.Max(minHeight, Math.Min(maxHeight, height.Value));
            rowContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            rowContainer.Height = clamped;
        }
    }

    private void ApplyColumnWidth(InteractiveGue cellContainer, GridLength width, float minWidth, float maxWidth)
    {
        if (width.IsAuto)
        {
            // Initially set to RelativeToChildren so layout can compute the natural width.
            // ApplyAutoColumnWidths will unify these to Absolute after all cells are built.
            cellContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            cellContainer.Width = 0;
            cellContainer.MinWidth = null;
            cellContainer.MaxWidth = null;
        }
        else if (width.IsStar)
        {
            cellContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
            cellContainer.Width = width.Value;
            cellContainer.MinWidth = minWidth > 0f ? minWidth : (float?)null;
            cellContainer.MaxWidth = float.IsPositiveInfinity(maxWidth) ? (float?)null : maxWidth;
        }
        else
        {
            float clamped = Math.Max(minWidth, Math.Min(maxWidth, width.Value));
            cellContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            cellContainer.Width = clamped;
        }
    }

    private void SubscribeToChildEvents(GraphicalUiElement childVisual)
    {
        if (_sizeChangedHandlers.ContainsKey(childVisual))
        {
            UnsubscribeFromChildEvents(childVisual);
        }

        EventHandler sizeHandler = OnChildSizeChanged;
        EventHandler<GraphicalUiElement.ParentChangedEventArgs> parentHandler = OnChildParentChanged;

        _sizeChangedHandlers[childVisual] = sizeHandler;
        _parentChangedHandlers[childVisual] = parentHandler;

        childVisual.SizeChanged += sizeHandler;
        childVisual.ParentChanged += parentHandler;
    }

    private void UnsubscribeFromChildEvents(GraphicalUiElement childVisual)
    {
        if (_sizeChangedHandlers.TryGetValue(childVisual, out EventHandler sizeHandler))
        {
            childVisual.SizeChanged -= sizeHandler;
            _sizeChangedHandlers.Remove(childVisual);
        }

        if (_parentChangedHandlers.TryGetValue(childVisual, out EventHandler<GraphicalUiElement.ParentChangedEventArgs> parentHandler))
        {
            childVisual.ParentChanged -= parentHandler;
            _parentChangedHandlers.Remove(childVisual);
        }
    }

    private bool IsWithinGrid(IRenderableIpso? node)
    {
        IRenderableIpso? current = node;
        while (current != null)
        {
            if (current == Visual)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private void OnChildSizeChanged(object sender, EventArgs e)
    {
        if (_isRefreshingLayout)
        {
            return;
        }

        bool hasAutoColumn = false;
        foreach (ColumnDefinition col in ColumnDefinitions)
        {
            if (col.Width.IsAuto)
            {
                hasAutoColumn = true;
                break;
            }
        }

        bool hasAutoRow = false;
        foreach (RowDefinition row in RowDefinitions)
        {
            if (row.Height.IsAuto)
            {
                hasAutoRow = true;
                break;
            }
        }

        if (hasAutoColumn || hasAutoRow)
        {
            RefreshLayout();
        }
    }

    private void OnChildParentChanged(object sender, GraphicalUiElement.ParentChangedEventArgs e)
    {
        if (_isRefreshingLayout)
        {
            return;
        }

        if (IsWithinGrid(e.NewValue))
        {
            return;
        }

        GraphicalUiElement childVisual = (GraphicalUiElement)sender;
        UnsubscribeFromChildEvents(childVisual);

        // Remove from FrameworkElement placements
        FrameworkElement? feToRemove = null;
        foreach (KeyValuePair<FrameworkElement, (int Row, int Column)> kvp in _cellPlacements)
        {
            if (kvp.Key.Visual == childVisual)
            {
                feToRemove = kvp.Key;
                break;
            }
        }
        if (feToRemove != null)
        {
            _cellPlacements.Remove(feToRemove);
        }

        // Remove from GUE placements
        _gueCellPlacements.Remove(childVisual);

        RefreshLayout();
    }
}
