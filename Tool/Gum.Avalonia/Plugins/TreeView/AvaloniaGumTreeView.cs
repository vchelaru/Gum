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
using AvaloniaDataUi;
using Gum.Avalonia.Services;
using Gum.Avalonia.Themes;
using Gum.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using FluentIcons.Avalonia;

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

    private readonly ObservableCollection<TreeRow> _rows;
    private readonly ItemsControl _itemsControl;
    private readonly ScrollViewer _scrollViewer;
    private readonly global::Avalonia.Controls.Canvas _dropOverlay;
    private readonly Border _dropParentHighlight;
    private readonly Border _dropIndicator;
    private readonly HashSet<GumTreeNode> _subscribedNodes;
    private readonly HashSet<GumTreeNodeCollection> _subscribedCollections;

    private KeyModifiers _modifiers;
    private bool _rebuildPending;
    private bool _isReleaseFromExpander;
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
        // Rows span the viewport, so a click or right click past a short name still lands on the row
        // (#4694); a name wider than the viewport still scrolls.
        _scrollViewer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ScrollViewer.ViewportProperty)
            {
                _itemsControl.MinWidth = _scrollViewer.Viewport.Width;
            }
        };
        // A soft wash behind the row that will become the dropped nodes' new parent, so a
        // Before/After/IntoFirst insert doesn't leave the parent to be inferred from the line's
        // position alone (#4913). Drawn under the line/rectangle, and lighter than it, so it reads
        // as context rather than competing with it.
        _dropParentHighlight = new Border
        {
            IsVisible = false,
            IsHitTestVisible = false,
            CornerRadius = new CornerRadius(2),
        }.WithThemeResource(Border.BackgroundProperty, "Frb.Brushes.Primary.Transparent");
        _dropIndicator = new Border
        {
            IsVisible = false,
            IsHitTestVisible = false,
        }.WithThemeResource(Border.BorderBrushProperty, "Frb.Brushes.Primary");
        _dropOverlay = new global::Avalonia.Controls.Canvas { IsHitTestVisible = false };
        _dropOverlay.Children.Add(_dropParentHighlight);
        _dropOverlay.Children.Add(_dropIndicator);

        Grid grid = new Grid();
        grid.Children.Add(_scrollViewer);
        grid.Children.Add(_dropOverlay);
        Content = grid;

        Nodes.CollectionChanged += (_, _) => RequestRebuild();

        DragDrop.SetAllowDrop(this, true);
        // DragEnterEvent gets its own tick, not just DragOverEvent: Avalonia's DragDropDevice tracks
        // the hit-tested drop target and, whenever it changes - including between two Controls of the
        // same row's template (the label, the icon, the blank space) - fires DragLeave on the old one
        // and DragEnter on the new one INSTEAD of DragOver for that pointer move. Left unhandled,
        // DragEnter falls through with no explicit effect, so the OS showed its own default cursor for
        // one tick every time the pointer crossed a sub-element boundary within a row, before the next
        // DragOver corrected it back (#4906 follow-up). Handling it identically to DragOver closes that gap.
        AddHandler(DragDrop.DragEnterEvent, HandleDragOver);
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

        (GumTreeNode anchorNode, double anchorOffsetWithinRow, double rowHeight)? anchor = CaptureScrollAnchor();

        SyncRows(rows);

        if (anchor is { } captured)
        {
            RestoreScrollAnchor(captured.anchorNode, captured.anchorOffsetWithinRow, captured.rowHeight);
        }
    }

    /// <summary>
    /// The node currently at the top of the viewport, how far scrolled into its row, and the row
    /// height at capture time - so a row inserted or removed above the viewport (e.g. adding an
    /// instance while scrolled past its container, #4882) can be compensated for instead of visually
    /// shifting everything already on screen. The row height is captured once and reused by
    /// <see cref="RestoreScrollAnchor"/> rather than re-measured after the rows change, since a
    /// rebuild can scroll the only realized rows out from under <see cref="RowHeight"/> before the
    /// next layout pass catches up.
    /// </summary>
    private (GumTreeNode, double, double)? CaptureScrollAnchor()
    {
        double rowHeight = RowHeight;
        if (rowHeight <= 0 || _rows.Count == 0)
        {
            return null;
        }

        int anchorIndex = Math.Clamp((int)(_scrollViewer.Offset.Y / rowHeight), 0, _rows.Count - 1);
        double offsetWithinRow = _scrollViewer.Offset.Y - anchorIndex * rowHeight;
        return (_rows[anchorIndex].Node, offsetWithinRow, rowHeight);
    }

    /// <summary>Shifts the scroll offset so <paramref name="anchorNode"/> is back at the same on-screen position it held before the rebuild, if it's still in the tree.</summary>
    private void RestoreScrollAnchor(GumTreeNode anchorNode, double anchorOffsetWithinRow, double rowHeight)
    {
        int newIndex = IndexOfRow(anchorNode);
        if (newIndex < 0)
        {
            return;
        }

        double newOffsetY = Math.Max(0, newIndex * rowHeight + anchorOffsetWithinRow);
        if (newOffsetY != _scrollViewer.Offset.Y)
        {
            _scrollViewer.Offset = _scrollViewer.Offset.WithY(newOffsetY);
        }
    }

    /// <summary>
    /// The height of a realized row, or a fallback estimate when none is realized. Reads any
    /// currently-realized row rather than assuming index 0 is realized, since scrolling past the top
    /// (the common case a scroll-anchor is captured in) virtualizes it away.
    /// </summary>
    private double RowHeight
    {
        get
        {
            double realized = _itemsControl.GetVisualDescendants().OfType<TreeRowView>()
                .Select(row => row.Bounds.Height)
                .FirstOrDefault(height => height > 0);
            return realized > 0 ? realized : DefaultRowHeight;
        }
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

    /// <summary>
    /// Called by a row when its expander takes a press: the matching release changes no selection
    /// and opens no menu, so the chevron only expands or collapses (#4694).
    /// </summary>
    internal void NotifyExpanderPressed() => _isReleaseFromExpander = true;

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragCandidate = null;
        _modifiers = e.KeyModifiers;
        if (_isReleaseFromExpander)
        {
            _isReleaseFromExpander = false;
            return;
        }
        GumTreeNode? node = NodeAt(e.GetPosition(this));
        TreePointerButton button = ToButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind);

        Selection.HandlePointerReleased(node, button);

        if (button == TreePointerButton.Right)
        {
            ContextMenuRequested?.Invoke();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        // No release follows a lost capture; the next click must not be swallowed.
        _isReleaseFromExpander = false;
        _dragCandidate = null;
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
    public GumTreeNode? NodeAt(Point position) => RowAt(position)?.Node;

    /// <summary>One row's node and its on-screen extent, in this control's coordinates.</summary>
    private readonly record struct LogicalRow(GumTreeNode Node, double Top, double Height);

    /// <summary>
    /// Finds the row at <paramref name="position"/> from the row list and the scroll offset directly,
    /// rather than hit-testing the realized <see cref="TreeRowView"/> visuals. A drag can move the
    /// scroll offset (<see cref="AutoScrollWhileDragging"/>) and immediately ask where the pointer
    /// landed in the same tick, before the virtualizing panel's next layout pass has realized rows at
    /// their new positions - hit-testing then sees stale visuals and drops or misidentifies the target,
    /// which is what made the drop indicator and the drag cursor flicker while auto-scrolling (#4906).
    /// Row math against <see cref="_rows"/> (always complete, never virtualized) has no such lag.
    /// </summary>
    private LogicalRow? RowAt(Point position)
    {
        double rowHeight = RowHeight;
        if (rowHeight <= 0 || _rows.Count == 0 || position.Y < 0 || position.X > _scrollViewer.Viewport.Width)
        {
            return null;
        }

        double contentY = position.Y + _scrollViewer.Offset.Y;
        int index = (int)(contentY / rowHeight);
        if (index < 0 || index >= _rows.Count)
        {
            return null;
        }

        double top = index * rowHeight - _scrollViewer.Offset.Y;
        return new LogicalRow(_rows[index].Node, top, rowHeight);
    }

    private static TreePointerButton ToButton(PointerUpdateKind kind) => kind switch
    {
        PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => TreePointerButton.Right,
        PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => TreePointerButton.Middle,
        PointerUpdateKind.XButton1Pressed or PointerUpdateKind.XButton1Released => TreePointerButton.XButton1,
        PointerUpdateKind.XButton2Pressed or PointerUpdateKind.XButton2Released => TreePointerButton.XButton2,
        _ => TreePointerButton.Left,
    };

    private static TreeModifierKeys ToTreeModifiers(KeyModifiers modifiers) =>
        ToTreeModifiers(modifiers, PlatformKeyModifiers.Command);

    /// <summary>
    /// Maps Avalonia modifiers to the tree's, with <paramref name="commandModifiers"/> as
    /// <see cref="TreeModifierKeys.Control"/> - the selection logic compares against Control
    /// exactly, so on macOS Cmd must come through as Control alone, not Control plus Windows.
    /// </summary>
    internal static TreeModifierKeys ToTreeModifiers(KeyModifiers modifiers, KeyModifiers commandModifiers)
    {
        TreeModifierKeys result = TreeModifierKeys.None;
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= TreeModifierKeys.Alt;
        }
        if (modifiers.HasFlag(commandModifiers))
        {
            result |= TreeModifierKeys.Control;
        }
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= TreeModifierKeys.Shift;
        }
        if (modifiers.HasFlag(KeyModifiers.Meta) && commandModifiers != KeyModifiers.Meta)
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

    private int VisibleRowCount => Math.Max(1, (int)(_scrollViewer.Viewport.Height / RowHeight));

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

        (LogicalRow? row, GumTreeNode? target, TreeDropKind kind) = GetDropAt(position, dragged);

        TreeDropValidationEventArgs validation = new TreeDropValidationEventArgs(dragged, target, kind);
        ValidateSortingDrop?.Invoke(this, validation);

        if (validation.Allow && validation.TargetNode != null && row != null)
        {
            e.DragEffects = DragDropEffects.Move;
            ShowDropIndicator(row.Value, validation.Kind);
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

    private (LogicalRow? Row, GumTreeNode? Target, TreeDropKind Kind) GetDropAt(Point position, IReadOnlyList<GumTreeNode> dragged)
    {
        if (RowAt(position) is not { } row)
        {
            return (null, null, TreeDropKind.None);
        }

        if (TreeDropLogic.IsNodeOrDescendantOfAny(row.Node, dragged))
        {
            return (null, null, TreeDropKind.None);
        }

        double fraction = row.Height > 0 ? (position.Y - row.Top) / row.Height : 0.5;
        return (row, row.Node, TreeDropLogic.GetKind(row.Node, fraction));
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

    // A line between rows for an insert, an outline around the row for a drop onto it. The line's
    // left margin matches where the drop will land in the hierarchy - flush with the target row's own
    // highlight for a sibling or an append (Before/After/Into), one level further in for a new first
    // child (IntoFirst) - rather than always spanning the full width, which gave no visual cue of the
    // resulting nesting (#4913). A Before/After/IntoFirst insert also washes the row that will become
    // the new parent, so it doesn't have to be inferred from the line's position alone.
    private void ShowDropIndicator(LogicalRow row, TreeDropKind kind)
    {
        Point topLeft = new Point(-_scrollViewer.Offset.X, row.Top);
        double width = Math.Max(_scrollViewer.Viewport.Width, _scrollViewer.Extent.Width);
        double height = Math.Max(1, row.Height);
        const double lineThickness = 2;
        double indent = TreeDropLogic.GetIndicatorIndent(row.Node, kind, TreeRowView.Indent);

        switch (kind)
        {
            case TreeDropKind.Into:
                _dropIndicator.BorderThickness = new Thickness(lineThickness);
                Place(topLeft.X + indent, topLeft.Y, Math.Max(0, width - indent), height);
                break;
            case TreeDropKind.Before:
                _dropIndicator.BorderThickness = new Thickness(0, lineThickness, 0, 0);
                Place(topLeft.X + indent, topLeft.Y - lineThickness / 2, Math.Max(0, width - indent), lineThickness);
                break;
            case TreeDropKind.After:
                _dropIndicator.BorderThickness = new Thickness(0, lineThickness, 0, 0);
                Place(topLeft.X + indent, topLeft.Y + height - lineThickness / 2, Math.Max(0, width - indent), lineThickness);
                break;
            case TreeDropKind.IntoFirst:
                // Indented, because the insert point is inside the row above it.
                _dropIndicator.BorderThickness = new Thickness(0, lineThickness, 0, 0);
                Place(topLeft.X + indent, topLeft.Y + height - lineThickness / 2, Math.Max(0, width - indent), lineThickness);
                break;
            default:
                ClearDropIndicator();
                break;
        }

        ShowParentHighlight(TreeDropLogic.GetParentHighlightNode(row.Node, kind), width);

        void Place(double x, double y, double w, double h)
        {
            global::Avalonia.Controls.Canvas.SetLeft(_dropIndicator, x);
            global::Avalonia.Controls.Canvas.SetTop(_dropIndicator, y);
            _dropIndicator.Width = w;
            _dropIndicator.Height = h;
            _dropIndicator.IsVisible = true;
        }
    }

    private void ShowParentHighlight(GumTreeNode? parentNode, double width)
    {
        if (parentNode == null || RowFor(parentNode) is not { } parentRow)
        {
            _dropParentHighlight.IsVisible = false;
            return;
        }

        double indent = parentNode.Level * TreeRowView.Indent;
        global::Avalonia.Controls.Canvas.SetLeft(_dropParentHighlight, -_scrollViewer.Offset.X + indent);
        global::Avalonia.Controls.Canvas.SetTop(_dropParentHighlight, parentRow.Top);
        _dropParentHighlight.Width = Math.Max(0, width - indent);
        _dropParentHighlight.Height = Math.Max(1, parentRow.Height);
        _dropParentHighlight.IsVisible = true;
    }

    /// <summary>The visible row for <paramref name="node"/>, or null when it isn't currently shown.</summary>
    private LogicalRow? RowFor(GumTreeNode node)
    {
        int index = IndexOfRow(node);
        if (index < 0)
        {
            return null;
        }

        double rowHeight = RowHeight;
        double top = index * rowHeight - _scrollViewer.Offset.Y;
        return new LogicalRow(node, top, rowHeight);
    }

    private void ClearDropIndicator()
    {
        _dropIndicator.IsVisible = false;
        _dropParentHighlight.IsVisible = false;
    }

    #endregion
}

/// <summary>One visible row: a node and its depth.</summary>
internal sealed record TreeRow(GumTreeNode Node, int Level);

/// <summary>
/// Draws one row of <see cref="AvaloniaGumTreeView"/>: indentation, then a highlight box holding the
/// expander, the node's icon and text, which shows the selected/hovered background from the indent
/// to the row's edge as the WPF row does. Follows its node's property changes while attached.
/// </summary>
internal sealed class TreeRowView : Border
{
    /// <summary>Horizontal space per tree level.</summary>
    internal const double Indent = 16;

    private static readonly IBrush FallbackFill = new SolidColorBrush(Color.FromArgb(0x26, 0x3e, 0x9e, 0xce));
    private static readonly IBrush FallbackBorder = new SolidColorBrush(Color.Parse("#3e9ece"));

    private readonly AvaloniaGumTreeView _owner;
    private readonly Border _indent;
    private readonly Border _highlight;
    private readonly Border _expander;
    private readonly FluentIcon _expanderIcon;
    private readonly ContentControl _iconHost;
    private readonly TextBlock _text;
    private GumTreeNode? _node;
    private int _iconIndex;

    public TreeRowView(AvaloniaGumTreeView owner)
    {
        _owner = owner;
        _iconIndex = -1;
        // Transparent rather than unset, so the whole row takes the pointer.
        Background = Brushes.Transparent;

        _indent = new Border();
        // The WPF tree's chevrons, right when collapsed and down when expanded.
        _expanderIcon = GumFluentIcons.Create(FluentIcons.Common.Icon.ChevronRight, 12);
        _expanderIcon.HorizontalAlignment = HorizontalAlignment.Center;
        _expanderIcon.VerticalAlignment = VerticalAlignment.Center;
        _expander = new Border
        {
            Width = 14,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Brushes.Transparent,
            Child = _expanderIcon,
        };
        _expander.PointerPressed += (_, e) =>
        {
            // Only the left button toggles; a right click on the chevron is a click on the row.
            if (!e.GetCurrentPoint(_expander).Properties.IsLeftButtonPressed)
            {
                return;
            }
            if (_node is { HasChildren: true } node)
            {
                node.IsExpanded = !node.IsExpanded;
            }
            _owner.NotifyExpanderPressed();
            e.Handled = true;
        };
        _iconHost = new ContentControl { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
        _text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

        StackPanel content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(_expander);
        content.Children.Add(_iconHost);
        content.Children.Add(_text);
        _highlight = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(1, 0),
            Child = content,
        };
        Grid.SetColumn(_highlight, 1);

        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        row.Children.Add(_indent);
        row.Children.Add(_highlight);
        Child = row;

        DoubleTapped += (_, _) =>
        {
            if (_node is { HasChildren: true } node)
            {
                node.IsExpanded = !node.IsExpanded;
            }
        };
    }

    public TreeRow? Row => DataContext as TreeRow;

    /// <summary>The box that shows the hovered and selected state, for tests.</summary>
    internal Border Highlight => _highlight;

    /// <summary>The expander area, for tests.</summary>
    internal Control Expander => _expander;

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
        _expanderIcon.IsVisible = _node.HasChildren;
        _expanderIcon.Icon = _node.IsExpanded ? FluentIcons.Common.Icon.ChevronDown : FluentIcons.Common.Icon.ChevronRight;
        _text.Text = _node.Text;
        // Hovered and selected rows share the primary wash; a selected row adds the primary border.
        _highlight.Background = _node.IsSelected || _node.IsHot
            ? ThemeBrushes.Get(this, "Frb.Brushes.Primary.Transparent", FallbackFill)
            : Brushes.Transparent;
        _highlight.BorderBrush = _node.IsSelected ? ThemeBrushes.Get(this, "Frb.Brushes.Primary", FallbackBorder) : Brushes.Transparent;

        if (_iconIndex != _node.ImageIndex)
        {
            _iconIndex = _node.ImageIndex;
            _iconHost.Content = AvaloniaTreeIcons.CreateIcon(_iconIndex, _owner.IconSize);
        }
    }
}
