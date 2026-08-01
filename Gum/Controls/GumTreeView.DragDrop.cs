using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// Where a drop lands relative to the row it is over.
/// </summary>
public enum TreeDropKind
{
    None,

    /// <summary>Insert above the target, as a sibling.</summary>
    Before,

    /// <summary>Drop onto the target, appending to its children.</summary>
    Into,

    /// <summary>Drop onto the target as its first child. Used when it is expanded.</summary>
    IntoFirst,

    /// <summary>Insert below the target, as a sibling.</summary>
    After,
}

/// <summary>
/// Asks whether a drop should be allowed, before it happens.
/// </summary>
public sealed class TreeDropValidationEventArgs : EventArgs
{
    internal TreeDropValidationEventArgs(IReadOnlyList<GumTreeNode> draggedNodes, GumTreeNode? targetNode, TreeDropKind kind)
    {
        DraggedNodes = draggedNodes;
        TargetNode = targetNode;
        Kind = kind;
    }

    public IReadOnlyList<GumTreeNode> DraggedNodes { get; }
    public GumTreeNode? TargetNode { get; set; }
    public TreeDropKind Kind { get; set; }

    /// <summary>Set true to permit the drop. Defaults to false.</summary>
    public bool Allow { get; set; }
}

/// <summary>
/// Reports a drop that has been accepted.
/// </summary>
public sealed class TreeDropEventArgs : EventArgs
{
    internal TreeDropEventArgs(IReadOnlyList<GumTreeNode> draggedNodes, GumTreeNode? targetNode, TreeDropKind kind)
    {
        DraggedNodes = draggedNodes;
        TargetNode = targetNode;
        Kind = kind;
    }

    public IReadOnlyList<GumTreeNode> DraggedNodes { get; }
    public GumTreeNode? TargetNode { get; set; }
    public TreeDropKind Kind { get; set; }
}

public partial class GumTreeView
{
    private Point _mouseDownPoint;
    private GumTreeNode? _dragCandidateNode;
    private bool _isDragging;
    private DropAdorner? _dropAdorner;

    /// <summary>
    /// Fraction of a row's height at its top and bottom that means "insert beside" rather than
    /// "drop onto".
    /// </summary>
    private const double EdgeBandFraction = 0.25;

    /// <summary>
    /// Distance from the top or bottom of the viewport within which a drag scrolls the tree.
    /// </summary>
    private const double AutoScrollBand = 20;

    /// <summary>
    /// Raised while dragging over a row, to decide whether the drop is legal. Handlers set
    /// <see cref="TreeDropValidationEventArgs.Allow"/>.
    /// </summary>
    public event EventHandler<TreeDropValidationEventArgs>? ValidateSortingDrop;

    /// <summary>
    /// Raised when nodes are dropped onto the tree.
    /// </summary>
    public event EventHandler<TreeDropEventArgs>? NodeSortingDropped;

    private void BeginPotentialDrag(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _mouseDownPoint = e.GetPosition(this);
        _dragCandidateNode = GetNodeAt(_mouseDownPoint);
    }

    private void EndPotentialDrag() => _dragCandidateNode = null;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        UpdateHotNode(e.GetPosition(this));

        if (_isDragging || _dragCandidateNode == null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point current = e.GetPosition(this);
        if (Math.Abs(current.X - _mouseDownPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _mouseDownPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        StartDrag(_dragCandidateNode);
    }

    private void UpdateHotNode(Point position) => SetExternalHotNode(GetNodeAt(position));

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        SetExternalHotNode(null);
    }

    private void StartDrag(GumTreeNode node)
    {
        // Dragging a row that is not part of the selection drags just that row, and takes the
        // selection with it.
        if (!_selectedNodes.Contains(node))
        {
            SelectSingleNode(node);
            OnAfterSelect(node);
        }

        List<GumTreeNode> dragged = _selectedNodes.Count > 0
            ? new List<GumTreeNode>(_selectedNodes)
            : new List<GumTreeNode> { node };

        _isDragging = true;
        _dragCandidateNode = null;

        try
        {
            TreeDragPayload.SetNodes(dragged);

            DataObject data = new();
            data.SetData(TreeDragPayload.DataFormat, true);

            DragDrop.DoDragDrop(this, data, DragDropEffects.Move | DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }
        finally
        {
            TreeDragPayload.Clear();
            _isDragging = false;
            ClearDropAdornment();
        }
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);

        AutoScrollWhileDragging(e.GetPosition(this));

        IReadOnlyList<GumTreeNode> dragged = DraggedNodesFrom(e);
        if (dragged.Count == 0)
        {
            // An external payload - files, or a Standards chip. The manager's own handler decides
            // the effect; this control only owns node reordering.
            ClearDropAdornment();
            return;
        }

        (GumTreeNode? target, TreeDropKind kind) = GetDropAt(e.GetPosition(this), dragged);

        TreeDropValidationEventArgs validation = new(dragged, target, kind);
        ValidateSortingDrop?.Invoke(this, validation);

        if (validation.Allow && validation.TargetNode != null)
        {
            e.Effects = DragDropEffects.Move;
            UpdateDropAdornment(validation.TargetNode, validation.Kind);
        }
        else
        {
            e.Effects = DragDropEffects.None;
            ClearDropAdornment();
        }

        // WPF re-asks on every move rather than carrying the previous answer forward, so the effect
        // has to be re-applied here and not just on DragEnter.
        e.Handled = true;
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        ClearDropAdornment();
    }

    protected override void OnDrop(DragEventArgs e)
    {
        ClearDropAdornment();

        IReadOnlyList<GumTreeNode> dragged = DraggedNodesFrom(e);
        if (dragged.Count == 0)
        {
            base.OnDrop(e);
            return;
        }

        (GumTreeNode? target, TreeDropKind kind) = GetDropAt(e.GetPosition(this), dragged);

        if (target != null && kind != TreeDropKind.None)
        {
            NodeSortingDropped?.Invoke(this, new TreeDropEventArgs(dragged, target, kind));
        }

        e.Handled = true;
        base.OnDrop(e);
    }

    private static IReadOnlyList<GumTreeNode> DraggedNodesFrom(DragEventArgs e) =>
        e.Data?.GetDataPresent(TreeDragPayload.DataFormat) == true && TreeDragPayload.Nodes is { } nodes
            ? nodes
            : Array.Empty<GumTreeNode>();

    /// <summary>
    /// Decides which row a point drops onto and how, rejecting targets that would move a node into
    /// its own subtree.
    /// </summary>
    private (GumTreeNode? node, TreeDropKind kind) GetDropAt(Point point, IReadOnlyList<GumTreeNode> dragged)
    {
        if (GetNodeAt(point) is not { } target || ContainerFor(target) is not { } container)
        {
            return (null, TreeDropKind.None);
        }

        if (IsNodeOrDescendantOfAny(target, dragged))
        {
            return (null, TreeDropKind.None);
        }

        // The container's own height includes its children, so measure against the row only.
        double rowHeight = container.ActualHeight;
        foreach (object child in container.Items)
        {
            if (container.ItemContainerGenerator.ContainerFromItem(child) is FrameworkElement realized)
            {
                rowHeight -= realized.ActualHeight;
            }
        }

        Point inContainer = TranslatePoint(point, container);
        double fraction = rowHeight > 0 ? inContainer.Y / rowHeight : 0.5;

        if (fraction <= EdgeBandFraction)
        {
            return (target, TreeDropKind.Before);
        }

        if (fraction >= 1 - EdgeBandFraction)
        {
            // Dropping just below an expanded row means "first child", not "next sibling" - the row
            // immediately underneath is its child.
            return (target, target.IsExpanded && target.ChildCount > 0
                ? TreeDropKind.IntoFirst
                : TreeDropKind.After);
        }

        return (target, TreeDropKind.Into);
    }

    private static bool IsNodeOrDescendantOfAny(GumTreeNode node, IReadOnlyList<GumTreeNode> candidates)
    {
        for (GumTreeNode? current = node; current != null; current = current.Parent)
        {
            if (candidates.Contains(current))
            {
                return true;
            }
        }

        return false;
    }

    private void AutoScrollWhileDragging(Point point)
    {
        if (FindScrollViewer() is not { } scrollViewer)
        {
            return;
        }

        if (point.Y < AutoScrollBand)
        {
            scrollViewer.LineUp();
        }
        else if (point.Y > ActualHeight - AutoScrollBand)
        {
            scrollViewer.LineDown();
        }
    }

    private ScrollViewer? FindScrollViewer() =>
        GetTemplateChild("PART_ScrollViewer") as ScrollViewer;

    #region Drop adornment

    private void UpdateDropAdornment(GumTreeNode node, TreeDropKind kind)
    {
        if (ContainerFor(node) is not { } container)
        {
            ClearDropAdornment();
            return;
        }

        AdornerLayer? layer = AdornerLayer.GetAdornerLayer(this);
        if (layer == null)
        {
            return;
        }

        if (_dropAdorner == null)
        {
            _dropAdorner = new DropAdorner(this);
            layer.Add(_dropAdorner);
        }

        _dropAdorner.Update(container, kind, this);
    }

    private void ClearDropAdornment()
    {
        if (_dropAdorner == null)
        {
            return;
        }

        AdornerLayer.GetAdornerLayer(this)?.Remove(_dropAdorner);
        _dropAdorner = null;
    }

    /// <summary>
    /// Draws where a drop will land: a line between rows for an insert, an outline around the row
    /// for a drop onto it.
    /// </summary>
    private sealed class DropAdorner : Adorner
    {
        private Rect _rowBounds;
        private TreeDropKind _kind;

        public DropAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
        }

        public void Update(FrameworkElement container, TreeDropKind kind, UIElement relativeTo)
        {
            Point topLeft = container.TranslatePoint(new Point(0, 0), relativeTo);

            // The container spans its children too; the row itself is the top strip.
            double rowHeight = container.ActualHeight;
            if (container is ItemsControl itemsControl)
            {
                foreach (object child in itemsControl.Items)
                {
                    if (itemsControl.ItemContainerGenerator.ContainerFromItem(child) is FrameworkElement realized)
                    {
                        rowHeight -= realized.ActualHeight;
                    }
                }
            }

            _rowBounds = new Rect(topLeft, new Size(container.ActualWidth, Math.Max(rowHeight, 1)));
            _kind = kind;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_kind == TreeDropKind.None)
            {
                return;
            }

            Brush brush = Application.Current?.TryFindResource("Frb.Brushes.Primary") as Brush ?? Brushes.DodgerBlue;
            Pen pen = new(brush, 2);
            pen.Freeze();

            switch (_kind)
            {
                case TreeDropKind.Before:
                    DrawInsertLine(drawingContext, pen, _rowBounds.Top);
                    break;
                case TreeDropKind.After:
                    DrawInsertLine(drawingContext, pen, _rowBounds.Bottom);
                    break;
                case TreeDropKind.IntoFirst:
                    // Indented, because the insert point is inside the row above it.
                    DrawInsertLine(drawingContext, pen, _rowBounds.Bottom, indent: 14);
                    break;
                case TreeDropKind.Into:
                    drawingContext.DrawRectangle(null, pen, _rowBounds);
                    break;
            }
        }

        private void DrawInsertLine(DrawingContext drawingContext, Pen pen, double y, double indent = 0) =>
            drawingContext.DrawLine(pen, new Point(_rowBounds.Left + indent, y), new Point(_rowBounds.Right, y));
    }

    #endregion
}
