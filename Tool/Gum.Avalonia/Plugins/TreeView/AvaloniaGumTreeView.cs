using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Services;
using Gum.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;

namespace Gum.Avalonia.Plugins.TreeView;

/// <summary>
/// The Avalonia element tree control. Shows the visible nodes of a <see cref="GumTreeNodeCollection"/>
/// as a flat, virtualized list of indented rows, and feeds pointer, key and drag input to the shared
/// <see cref="TreeSelectionModel"/>. The counterpart of the WPF <c>GumTreeView</c>.
/// </summary>
/// <remarks>
/// A flat row list rather than Avalonia's <c>TreeView</c>: selection lives on the nodes (so any
/// number of rows can be selected), a drop needs the bounds of one row without its children, and
/// scrolling a node into view needs its row index. The rows are rebuilt from the node model whenever
/// a visible node's children or expansion change, coalesced to one rebuild per dispatcher pass.
/// </remarks>
public sealed class AvaloniaGumTreeView : UserControl
{
    private const double DefaultRowHeight = 20;
    private const double DragThreshold = 4;
    private const double AutoScrollBand = 20;
    private const double AutoScrollStep = 12;
    private const double IntoFirstIndent = 14;

    private readonly ObservableCollection<TreeRow> _rows;
    private readonly ItemsControl _itemsControl;
    private readonly ScrollViewer _scrollViewer;
    private readonly global::Avalonia.Controls.Canvas _dropOverlay;
    private readonly Border _dropIndicator;
    private readonly HashSet<GumTreeNode> _subscribedNodes;
    private readonly HashSet<GumTreeNodeCollection> _subscribedCollections;

    private KeyModifiers _modifiers;
    private bool _rebuildPending;
    private Point _pressPoint;
    private GumTreeNode? _dragCandidate;
    private bool _isDragging;

    /// <summary>Creates an empty tree.</summary>
    public AvaloniaGumTreeView()
    {
        Nodes = new GumTreeNodeCollection();
        Selection = new TreeSelectionModel(Nodes, () => ToTreeModifiers(_modifiers), EnsureVisible);
        Selection.UnhandledException += ex => UnhandledException?.Invoke(ex);
        _rows = new ObservableCollection<TreeRow>();
        _subscribedNodes = new HashSet<GumTreeNode>();
        _subscribedCollections = new HashSet<GumTreeNodeCollection>();
        IconSize = 16;
        Focusable = true;
        Background = Brushes.Transparent;

        _itemsControl = new ItemsControl
        {
            ItemsSource = _rows,
            ItemTemplate = new FuncDataTemplate<TreeRow>((_, _) => new TreeRowView(this)),
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()),
        };
        _scrollViewer = new ScrollViewer
        {
            Content = _itemsControl,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
        _dropIndicator = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#3e9ece")),
            IsVisible = false,
            IsHitTestVisible = false,
        };
        _dropOverlay = new global::Avalonia.Controls.Canvas { IsHitTestVisible = false };
        _dropOverlay.Children.Add(_dropIndicator);

        Grid grid = new Grid();
        grid.Children.Add(_scrollViewer);
        grid.Children.Add(_dropOverlay);
        Content = grid;

        Nodes.CollectionChanged += (_, _) => RequestRebuild();

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, HandleDragOver);
        AddHandler(DragDrop.DragLeaveEvent, (_, _) => ClearDropIndicator());
        AddHandler(DragDrop.DropEvent, HandleDrop);
    }

    #region Properties and events

    /// <summary>The tree's root nodes.</summary>
    public GumTreeNodeCollection Nodes { get; }

    /// <summary>The selection this control feeds.</summary>
    public TreeSelectionModel Selection { get; }

    /// <summary>Edge length of each row's icon.</summary>
    public double IconSize { get; set; }

    /// <summary>The node whose row is under the pointer, or null.</summary>
    public GumTreeNode? NodeUnderPointer { get; private set; }

    /// <summary>The nodes currently shown as rows, top to bottom.</summary>
    internal IReadOnlyList<GumTreeNode> VisibleNodes => _rows.Select(row => row.Node).ToList();

    /// <summary>Raised on every pointer move over the tree, with the node under the pointer.</summary>
    public event Action<GumTreeNode?>? PointerMovedOverNode;

    /// <summary>Raised when the right button is released over the tree, after the selection updates.</summary>
    public event Action? ContextMenuRequested;

    /// <summary>Raised for key presses the tree's own navigation did not handle.</summary>
    public event Action<GumKeyEventArgs>? KeyPressed;

    /// <summary>Raised when a node's expansion changes.</summary>
    public event Action? NodeExpansionChanged;

    /// <summary>Raised when an exception is caught while reacting to input.</summary>
    public event Action<Exception>? UnhandledException;

    /// <summary>Raised while nodes are dragged over a row.</summary>
    public event EventHandler<TreeDropValidationEventArgs>? ValidateSortingDrop;

    /// <summary>Raised when nodes are dropped onto the tree.</summary>
    public event EventHandler<TreeDropEventArgs>? NodeSortingDropped;

    /// <summary>Raised while anything other than tree nodes is dragged over the tree.</summary>
    public event EventHandler<TreeExternalDragEventArgs>? ExternalDragOver;

    /// <summary>Raised when files or a palette chip are dropped onto the tree.</summary>
    public event EventHandler<TreeExternalDragEventArgs>? ExternalDrop;

    /// <summary>Raised when a drag started from this tree ends.</summary>
    public event Action? DragEnded;

    #endregion

    #region Rows

    /// <summary>Expands <paramref name="node"/>'s ancestors and scrolls its row into view.</summary>
    public void EnsureVisible(GumTreeNode node)
    {
        node.EnsureAncestorsExpanded();
        RebuildIfPending();

        // The row may not be realized until the next layout pass.
        Dispatcher.UIThread.Post(() =>
        {
            int index = IndexOfRow(node);
            if (index >= 0)
            {
                _itemsControl.ScrollIntoView(index);
            }
        }, DispatcherPriority.Loaded);
    }

    private int IndexOfRow(GumTreeNode node)
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].Node == node)
            {
                return i;
            }
        }
        return -1;
    }

    private void RequestRebuild()
    {
        if (_rebuildPending)
        {
            return;
        }

        _rebuildPending = true;
        Dispatcher.UIThread.Post(RebuildIfPending, DispatcherPriority.Normal);
    }

    private void RebuildIfPending()
    {
        if (!_rebuildPending)
        {
            return;
        }

        _rebuildPending = false;
        RebuildRows();
    }

    private void RebuildRows()
    {
        foreach (GumTreeNode node in _subscribedNodes)
        {
            node.PropertyChanged -= HandleNodePropertyChanged;
        }
        _subscribedNodes.Clear();
        foreach (GumTreeNodeCollection collection in _subscribedCollections)
        {
            collection.CollectionChanged -= HandleChildrenChanged;
        }
        _subscribedCollections.Clear();

        List<TreeRow> rows = new List<TreeRow>();
        foreach (GumTreeNode root in Nodes)
        {
            AddVisibleRows(root, 0, rows);
        }

        SyncRows(rows);
    }

    private void AddVisibleRows(GumTreeNode node, int level, List<TreeRow> rows)
    {
        rows.Add(new TreeRow(node, level));

        if (_subscribedNodes.Add(node))
        {
            node.PropertyChanged += HandleNodePropertyChanged;
        }
        if (_subscribedCollections.Add(node.Nodes))
        {
            node.Nodes.CollectionChanged += HandleChildrenChanged;
        }

        if (!node.IsExpanded)
        {
            return;
        }

        foreach (GumTreeNode child in node.Nodes)
        {
            AddVisibleRows(child, level + 1, rows);
        }
    }

    // Replaces only the changed middle of the list, so rows outside an expanded or collapsed range
    // keep their containers and the scroll position holds.
    private void SyncRows(List<TreeRow> rows)
    {
        int prefix = 0;
        while (prefix < _rows.Count && prefix < rows.Count && _rows[prefix].Equals(rows[prefix]))
        {
            prefix++;
        }

        int suffix = 0;
        while (suffix < _rows.Count - prefix && suffix < rows.Count - prefix &&
               _rows[_rows.Count - 1 - suffix].Equals(rows[rows.Count - 1 - suffix]))
        {
            suffix++;
        }

        for (int i = _rows.Count - suffix - 1; i >= prefix; i--)
        {
            _rows.RemoveAt(i);
        }
        for (int i = prefix; i < rows.Count - suffix; i++)
        {
            _rows.Insert(i, rows[i]);
        }
    }

    private void HandleNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GumTreeNode.IsExpanded))
        {
            RequestRebuild();
            NodeExpansionChanged?.Invoke();
        }
    }

    private void HandleChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e) => RequestRebuild();

    #endregion

    #region Pointer input

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // A row's expander handles its own press and changes expansion only.
        if (e.Handled)
        {
            return;
        }

        Focus();
        _modifiers = e.KeyModifiers;
        Point position = e.GetPosition(this);
        GumTreeNode? node = NodeAt(position);
        TreePointerButton button = ToButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind);

        Selection.HandlePointerPressed(node, button);

        if (button == TreePointerButton.Left && node != null)
        {
            _pressPoint = position;
            _dragCandidate = node;
        }
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragCandidate = null;
        _modifiers = e.KeyModifiers;
        GumTreeNode? node = NodeAt(e.GetPosition(this));
        TreePointerButton button = ToButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind);

        Selection.HandlePointerReleased(node, button);

        if (button == TreePointerButton.Right)
        {
            ContextMenuRequested?.Invoke();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _modifiers = e.KeyModifiers;
        Point position = e.GetPosition(this);
        GumTreeNode? node = NodeAt(position);
        NodeUnderPointer = node;
        Selection.SetExternalHotNode(node);
        PointerMovedOverNode?.Invoke(node);

        if (_dragCandidate != null && !_isDragging && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
            (Math.Abs(position.X - _pressPoint.X) >= DragThreshold || Math.Abs(position.Y - _pressPoint.Y) >= DragThreshold))
        {
            StartDrag(e, _dragCandidate);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        NodeUnderPointer = null;
        Selection.SetExternalHotNode(null);
    }

    /// <summary>The node whose row is at <paramref name="position"/> (in this control's coordinates), or null.</summary>
    public GumTreeNode? NodeAt(Point position) => RowViewAt(position)?.Row?.Node;

    private TreeRowView? RowViewAt(Point position)
    {
        if (this.InputHitTest(position) is not Visual hit)
        {
            return null;
        }

        for (Visual? current = hit; current != null && current != this; current = current.GetVisualParent())
        {
            if (current is TreeRowView rowView)
            {
                return rowView;
            }
        }
        return null;
    }

    private static TreePointerButton ToButton(PointerUpdateKind kind) => kind switch
    {
        PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => TreePointerButton.Right,
        PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => TreePointerButton.Middle,
        PointerUpdateKind.XButton1Pressed or PointerUpdateKind.XButton1Released => TreePointerButton.XButton1,
        PointerUpdateKind.XButton2Pressed or PointerUpdateKind.XButton2Released => TreePointerButton.XButton2,
        _ => TreePointerButton.Left,
    };

    private static TreeModifierKeys ToTreeModifiers(KeyModifiers modifiers)
    {
        TreeModifierKeys result = TreeModifierKeys.None;
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= TreeModifierKeys.Alt;
        }
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            result |= TreeModifierKeys.Control;
        }
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= TreeModifierKeys.Shift;
        }
        if (modifiers.HasFlag(KeyModifiers.Meta))
        {
            result |= TreeModifierKeys.Windows;
        }
        return result;
    }

    #endregion

    #region Keyboard input

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _modifiers = e.KeyModifiers;
        if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl)
        {
            base.OnKeyDown(e);
            return;
        }

        if (Selection.HandleKeyDown(ToNavigationKey(e.Key), VisibleRowCount))
        {
            e.Handled = true;
            return;
        }

        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
        KeyPressed?.Invoke(keyArgs);
        e.Handled = keyArgs.Handled;
        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        _modifiers = e.KeyModifiers;
        base.OnKeyUp(e);
    }

    private int VisibleRowCount
    {
        get
        {
            double rowHeight = _itemsControl.ContainerFromIndex(0)?.Bounds.Height is > 0 and double height
                ? height
                : DefaultRowHeight;
            return Math.Max(1, (int)(_scrollViewer.Viewport.Height / rowHeight));
        }
    }

    private static TreeNavigationKey? ToNavigationKey(Key key) => key switch
    {
        Key.Left => TreeNavigationKey.Left,
        Key.Right => TreeNavigationKey.Right,
        Key.Up => TreeNavigationKey.Up,
        Key.Down => TreeNavigationKey.Down,
        Key.Home => TreeNavigationKey.Home,
        Key.End => TreeNavigationKey.End,
        Key.PageUp => TreeNavigationKey.PageUp,
        Key.PageDown => TreeNavigationKey.PageDown,
        _ => null,
    };

    #endregion

    #region Drag and drop

    private async void StartDrag(PointerEventArgs e, GumTreeNode node)
    {
        _dragCandidate = null;
        _isDragging = true;
        try
        {
            IReadOnlyList<GumTreeNode> dragged = Selection.BeginDrag(node);
            TreeDragPayload.SetNodes(dragged);

            // Gum's objects cannot ride on the data transfer; it carries a marker and the nodes
            // travel in TreeDragPayload.
            DataTransfer data = new DataTransfer();
            data.Add(DataTransferItem.Create(AvaloniaDragFormats.TreeNodes, "nodes"));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move | DragDropEffects.Copy);
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }
        finally
        {
            TreeDragPayload.Clear();
            _isDragging = false;
            ClearDropIndicator();
            DragEnded?.Invoke();
        }
    }

    private void HandleDragOver(object? sender, DragEventArgs e)
    {
        Point position = e.GetPosition(this);
        AutoScrollWhileDragging(position);

        IReadOnlyList<GumTreeNode> dragged = DraggedNodesFrom(e);
        if (dragged.Count == 0)
        {
            ClearDropIndicator();
            TreeExternalDragEventArgs args = ReadExternalPayload(e, NodeAt(position));
            ExternalDragOver?.Invoke(this, args);
            bool hasPayload = args.Files != null || args.StandardElementTypeName != null;
            e.DragEffects = hasPayload && args.Accepted ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
            return;
        }

        (TreeRowView? rowView, GumTreeNode? target, TreeDropKind kind) = GetDropAt(position, dragged);

        TreeDropValidationEventArgs validation = new TreeDropValidationEventArgs(dragged, target, kind);
        ValidateSortingDrop?.Invoke(this, validation);

        if (validation.Allow && validation.TargetNode != null && rowView != null)
        {
            e.DragEffects = DragDropEffects.Move;
            ShowDropIndicator(rowView, validation.Kind);
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
            ClearDropIndicator();
        }
        e.Handled = true;
    }

    private void HandleDrop(object? sender, DragEventArgs e)
    {
        ClearDropIndicator();
        Point position = e.GetPosition(this);

        IReadOnlyList<GumTreeNode> dragged = DraggedNodesFrom(e);
        if (dragged.Count == 0)
        {
            TreeExternalDragEventArgs args = ReadExternalPayload(e, NodeAt(position));
            if (args.Files != null || args.StandardElementTypeName != null)
            {
                ExternalDrop?.Invoke(this, args);
            }
            e.Handled = true;
            return;
        }

        (_, GumTreeNode? target, TreeDropKind kind) = GetDropAt(position, dragged);
        if (target != null && kind != TreeDropKind.None)
        {
            NodeSortingDropped?.Invoke(this, new TreeDropEventArgs(dragged, target, kind));
        }
        e.Handled = true;
    }

    private static IReadOnlyList<GumTreeNode> DraggedNodesFrom(DragEventArgs e) =>
        e.DataTransfer.Contains(AvaloniaDragFormats.TreeNodes) && TreeDragPayload.Nodes is { } nodes
            ? nodes
            : Array.Empty<GumTreeNode>();

    private static TreeExternalDragEventArgs ReadExternalPayload(DragEventArgs e, GumTreeNode? target)
    {
        IStorageItem[]? items = e.DataTransfer.TryGetFiles();
        string[]? files = items?
            .Select(item => item.TryGetLocalPath())
            .Where(path => path != null)
            .Select(path => path!)
            .ToArray();
        string? standardElementTypeName = e.DataTransfer.TryGetValue(AvaloniaDragFormats.StandardElementName);
        return new TreeExternalDragEventArgs(files is { Length: > 0 } ? files : null, standardElementTypeName, target);
    }

    private (TreeRowView? RowView, GumTreeNode? Target, TreeDropKind Kind) GetDropAt(Point position, IReadOnlyList<GumTreeNode> dragged)
    {
        if (RowViewAt(position) is not { Row: { } row } rowView)
        {
            return (null, null, TreeDropKind.None);
        }

        if (TreeDropLogic.IsNodeOrDescendantOfAny(row.Node, dragged))
        {
            return (null, null, TreeDropKind.None);
        }

        Point inRow = this.TranslatePoint(position, rowView) ?? default;
        double fraction = rowView.Bounds.Height > 0 ? inRow.Y / rowView.Bounds.Height : 0.5;
        return (rowView, row.Node, TreeDropLogic.GetKind(row.Node, fraction));
    }

    private void AutoScrollWhileDragging(Point position)
    {
        if (position.Y < AutoScrollBand)
        {
            _scrollViewer.Offset = _scrollViewer.Offset.WithY(Math.Max(0, _scrollViewer.Offset.Y - AutoScrollStep));
        }
        else if (position.Y > Bounds.Height - AutoScrollBand)
        {
            _scrollViewer.Offset = _scrollViewer.Offset.WithY(_scrollViewer.Offset.Y + AutoScrollStep);
        }
    }

    // A line between rows for an insert, an outline around the row for a drop onto it.
    private void ShowDropIndicator(TreeRowView rowView, TreeDropKind kind)
    {
        if (rowView.TranslatePoint(new Point(0, 0), _dropOverlay) is not { } topLeft)
        {
            ClearDropIndicator();
            return;
        }

        double width = rowView.Bounds.Width;
        double height = Math.Max(1, rowView.Bounds.Height);
        const double lineThickness = 2;

        switch (kind)
        {
            case TreeDropKind.Into:
                _dropIndicator.BorderThickness = new Thickness(lineThickness);
                Place(topLeft.X, topLeft.Y, width, height);
                break;
            case TreeDropKind.Before:
                _dropIndicator.BorderThickness = new Thickness(0, lineThickness, 0, 0);
                Place(topLeft.X, topLeft.Y - lineThickness / 2, width, lineThickness);
                break;
            case TreeDropKind.After:
                _dropIndicator.BorderThickness = new Thickness(0, lineThickness, 0, 0);
                Place(topLeft.X, topLeft.Y + height - lineThickness / 2, width, lineThickness);
                break;
            case TreeDropKind.IntoFirst:
                // Indented, because the insert point is inside the row above it.
                _dropIndicator.BorderThickness = new Thickness(0, lineThickness, 0, 0);
                Place(topLeft.X + IntoFirstIndent, topLeft.Y + height - lineThickness / 2, Math.Max(0, width - IntoFirstIndent), lineThickness);
                break;
            default:
                ClearDropIndicator();
                break;
        }

        void Place(double x, double y, double w, double h)
        {
            global::Avalonia.Controls.Canvas.SetLeft(_dropIndicator, x);
            global::Avalonia.Controls.Canvas.SetTop(_dropIndicator, y);
            _dropIndicator.Width = w;
            _dropIndicator.Height = h;
            _dropIndicator.IsVisible = true;
        }
    }

    private void ClearDropIndicator() => _dropIndicator.IsVisible = false;

    #endregion
}

/// <summary>One visible row: a node and its depth.</summary>
internal sealed record TreeRow(GumTreeNode Node, int Level);

/// <summary>
/// Draws one row of <see cref="AvaloniaGumTreeView"/>: indentation, an expander, the node's icon and
/// text, and the selected/hovered background. Follows its node's property changes while attached.
/// </summary>
internal sealed class TreeRowView : Border
{
    private const double Indent = 16;

    private static readonly IBrush SelectedBrush = new SolidColorBrush(Color.FromArgb(0x90, 0x3e, 0x9e, 0xce));
    private static readonly IBrush HotBrush = new SolidColorBrush(Color.FromArgb(0x38, 0x3e, 0x9e, 0xce));

    private readonly AvaloniaGumTreeView _owner;
    private readonly Border _indent;
    private readonly TextBlock _expander;
    private readonly ContentControl _iconHost;
    private readonly TextBlock _text;
    private GumTreeNode? _node;
    private int _iconIndex;

    public TreeRowView(AvaloniaGumTreeView owner)
    {
        _owner = owner;
        _iconIndex = -1;
        Background = Brushes.Transparent;
        Padding = new Thickness(2, 1);

        _indent = new Border();
        _expander = new TextBlock
        {
            Width = 14,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brushes.Transparent,
        };
        _expander.PointerPressed += (_, e) =>
        {
            if (_node is { HasChildren: true } node)
            {
                node.IsExpanded = !node.IsExpanded;
            }
            e.Handled = true;
        };
        _iconHost = new ContentControl { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
        _text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

        StackPanel content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(_indent);
        content.Children.Add(_expander);
        content.Children.Add(_iconHost);
        content.Children.Add(_text);
        Child = content;

        DoubleTapped += (_, _) =>
        {
            if (_node is { HasChildren: true } node)
            {
                node.IsExpanded = !node.IsExpanded;
            }
        };
    }

    public TreeRow? Row => DataContext as TreeRow;

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        Attach(Row?.Node);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AvaloniaTreeIcons.ThemeChanged += HandleThemeChanged;
        Attach(Row?.Node);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        AvaloniaTreeIcons.ThemeChanged -= HandleThemeChanged;
        Attach(null);
    }

    private void Attach(GumTreeNode? node)
    {
        if (_node == node)
        {
            Refresh();
            return;
        }
        if (_node != null)
        {
            _node.PropertyChanged -= HandleNodePropertyChanged;
        }
        _node = node;
        _iconIndex = -1;
        if (_node != null)
        {
            _node.PropertyChanged += HandleNodePropertyChanged;
        }
        Refresh();
    }

    private void HandleNodePropertyChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    private void HandleThemeChanged(object? sender, EventArgs e)
    {
        _iconIndex = -1;
        Refresh();
    }

    private void Refresh()
    {
        if (_node == null || Row == null)
        {
            return;
        }

        _indent.Width = Row.Level * Indent;
        _expander.Text = _node.HasChildren ? (_node.IsExpanded ? "▾" : "▸") : string.Empty;
        _text.Text = _node.Text;
        Background = _node.IsSelected ? SelectedBrush : _node.IsHot ? HotBrush : Brushes.Transparent;

        if (_iconIndex != _node.ImageIndex)
        {
            _iconIndex = _node.ImageIndex;
            _iconHost.Content = AvaloniaTreeIcons.CreateIcon(_iconIndex, _owner.IconSize);
        }
    }
}
