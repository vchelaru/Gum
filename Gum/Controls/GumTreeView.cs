using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// The element tree's control. A WPF <see cref="TreeView"/> that supports multi-selection, which
/// the base control does not.
/// </summary>
/// <remarks>
/// <para>
/// Selection is tracked on the <see cref="GumTreeNode"/> models rather than through
/// <see cref="TreeViewItem.IsSelected"/>, because <see cref="TreeView"/> enforces a single selected
/// item and would clear the previous one on every change. The row template binds its selected
/// visual to <see cref="GumTreeNode.IsSelected"/>, so any number of rows can show as selected.
/// </para>
/// <para>
/// The selection rules themselves live in the framework-neutral <see cref="TreeSelectionModel"/>,
/// shared with the Avalonia tree. This control hit-tests, detects expander clicks, and feeds the
/// model pointer, key and drag input. Because the base control's selection is never engaged,
/// keyboard navigation is handled here too rather than inherited.
/// </para>
/// </remarks>
public partial class GumTreeView : TreeView
{
    /// <summary>
    /// Roughly how many rows fit in the viewport. Drives PageUp/PageDown, replacing the WinForms
    /// control's <c>VisibleCount</c>.
    /// </summary>
    private int VisibleRowCount
    {
        get
        {
            double rowHeight = FontSize * 2;
            return rowHeight > 0 ? Math.Max(1, (int)(ActualHeight / rowHeight)) : 1;
        }
    }

    public GumTreeView()
    {
        Nodes = new GumTreeNodeCollection();
        // ModifierKeys and TreeModifierKeys share their flag values.
        Selection = new TreeSelectionModel(Nodes, () => (TreeModifierKeys)Keyboard.Modifiers, EnsureVisible);
        Selection.UnhandledException += ex => UnhandledException?.Invoke(ex);
        ItemsSource = Nodes;
        Focusable = true;
        AllowDrop = true;

        // Rows raise these when their bound IsExpanded changes, whoever changed it - matching what
        // the WinForms AfterExpand/AfterCollapse pair covered.
        AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler((_, _) => NotifyExpansionChangedByUser()));
        AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler((_, _) => NotifyExpansionChangedByUser()));
    }

    /// <summary>
    /// Edge length of each row's icon. Scales with the UI font size, which is what kept the WinForms
    /// tree's icons in proportion when the app font changed - there, by regenerating the whole image
    /// list at a new pixel size.
    /// </summary>
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize), typeof(double), typeof(GumTreeView), new FrameworkPropertyMetadata(16.0));

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    #region Nodes and selection

    /// <summary>
    /// The tree's root nodes. Named to match the WinForms collection it replaces so the
    /// tree-building code reads unchanged.
    /// </summary>
    public GumTreeNodeCollection Nodes { get; }

    /// <summary>The selection this control feeds with pointer and key input.</summary>
    public TreeSelectionModel Selection { get; }

    /// <inheritdoc cref="TreeSelectionModel.AlwaysHaveOneNodeSelected"/>
    public bool AlwaysHaveOneNodeSelected
    {
        get => Selection.AlwaysHaveOneNodeSelected;
        set => Selection.AlwaysHaveOneNodeSelected = value;
    }

    /// <inheritdoc cref="TreeSelectionModel.IsSelectingOnPush"/>
    public bool IsSelectingOnPush
    {
        get => Selection.IsSelectingOnPush;
        set => Selection.IsSelectingOnPush = value;
    }

    /// <inheritdoc cref="TreeSelectionModel.MultiSelectBehavior"/>
    public MultiSelectBehavior MultiSelectBehavior
    {
        get => Selection.MultiSelectBehavior;
        set => Selection.MultiSelectBehavior = value;
    }

    /// <inheritdoc cref="TreeSelectionModel.SelectedNode"/>
    public GumTreeNode? SelectedNode
    {
        get => Selection.SelectedNode;
        set => Selection.SelectedNode = value;
    }

    /// <inheritdoc cref="TreeSelectionModel.SelectedNodes"/>
    public IReadOnlyList<GumTreeNode> SelectedNodes
    {
        get => Selection.SelectedNodes;
        set => Selection.SelectedNodes = value;
    }

    /// <inheritdoc cref="TreeSelectionModel.SetExternalHotNode"/>
    public void SetExternalHotNode(GumTreeNode? node) => Selection.SetExternalHotNode(node);

    #endregion

    #region Events

    /// <inheritdoc cref="TreeSelectionModel.AfterClickSelect"/>
    public event Action<GumTreeNode?>? AfterClickSelect
    {
        add => Selection.AfterClickSelect += value;
        remove => Selection.AfterClickSelect -= value;
    }

    /// <inheritdoc cref="TreeSelectionModel.AfterSelect"/>
    public event Action<GumTreeNode?>? AfterSelect
    {
        add => Selection.AfterSelect += value;
        remove => Selection.AfterSelect -= value;
    }

    /// <summary>
    /// Raised when an exception is caught while reacting to tree input. This control has no
    /// dependency on the host's dialog service, so it hands the exception off here instead of
    /// showing UI itself.
    /// </summary>
    public event Action<Exception>? UnhandledException;

    /// <summary>
    /// Raised when a node is expanded or collapsed by the user, as opposed to by code.
    /// </summary>
    public event EventHandler? NodeExpansionChangedByUser;

    /// <inheritdoc cref="TreeSelectionModel.CallAfterClickSelect"/>
    public void CallAfterClickSelect(GumTreeNode? node) => Selection.CallAfterClickSelect(node);

    #endregion

    #region Expansion

    /// <summary>
    /// Scrolls <paramref name="node"/> into view, expanding its ancestors first so it is reachable.
    /// </summary>
    public void EnsureVisible(GumTreeNode node)
    {
        node.EnsureAncestorsExpanded();

        // The container for a freshly-expanded ancestor's child does not exist until layout runs.
        Dispatcher.BeginInvoke(
            new Action(() => ContainerFor(node)?.BringIntoView()),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The realized row for a node, or null if it has not been created (its parent is collapsed, or
    /// layout has not run yet).
    /// </summary>
    internal TreeViewItem? ContainerFor(GumTreeNode node)
    {
        ItemsControl? owner = node.Parent is { } parent ? ContainerFor(parent) : this;
        return owner?.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem;
    }

    #endregion

    #region Hit testing

    /// <summary>
    /// The node whose row contains <paramref name="point"/>, in this control's coordinates, or null
    /// if the point is not over a row.
    /// </summary>
    public GumTreeNode? GetNodeAt(Point point)
    {
        if (InputHitTest(point) is not DependencyObject hit)
        {
            return null;
        }

        return FindAncestorItem(hit)?.DataContext as GumTreeNode;
    }

    /// <summary>
    /// Whether a click landed on a row's expand/collapse toggle rather than on the row itself.
    /// </summary>
    private static bool IsExpanderClick(object? originalSource)
    {
        if (originalSource is not DependencyObject source)
        {
            return false;
        }

        // Stop at the row: a ToggleButton found above it belongs to an ancestor row, not this click.
        for (DependencyObject? current = source;
             current is not null and not TreeViewItem;
             current = GetVisualOrLogicalParent(current))
        {
            if (current is System.Windows.Controls.Primitives.ToggleButton)
            {
                return true;
            }
        }

        return false;
    }

    private static TreeViewItem? FindAncestorItem(DependencyObject start)
    {
        for (DependencyObject? current = start; current != null; current = GetVisualOrLogicalParent(current))
        {
            if (current is TreeViewItem item)
            {
                return item;
            }
        }

        return null;
    }

    private static DependencyObject? GetVisualOrLogicalParent(DependencyObject current) =>
        current is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(current)
            : LogicalTreeHelper.GetParent(current);

    #endregion

    #region Mouse input

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        // An expander click changes expansion only. Letting it fall through would also select the
        // row, which makes expanding a folder disruptive - it would move the selection away from
        // whatever the user is working on.
        if (IsExpanderClick(e.OriginalSource))
        {
            base.OnPreviewMouseDown(e);
            return;
        }

        try
        {
            // MouseButton and TreePointerButton share their order.
            Selection.HandlePointerPressed(GetNodeAt(e.GetPosition(this)), (TreePointerButton)e.ChangedButton);

            BeginPotentialDrag(e);
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }

        base.OnPreviewMouseDown(e);
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        if (IsExpanderClick(e.OriginalSource))
        {
            EndPotentialDrag();
            base.OnPreviewMouseUp(e);
            return;
        }

        try
        {
            EndPotentialDrag();

            Selection.HandlePointerReleased(GetNodeAt(e.GetPosition(this)), (TreePointerButton)e.ChangedButton);
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }

        base.OnPreviewMouseUp(e);
    }

    #endregion

    #region Keyboard input

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl)
        {
            return;
        }

        if (Selection.HandleKeyDown(ToNavigationKey(e.Key), VisibleRowCount))
        {
            e.Handled = true;
        }

        base.OnKeyDown(e);
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

    /// <summary>
    /// Called by the row template when the user toggles an expander, so the collapse-toggle service
    /// can tell a user-driven change from a programmatic one.
    /// </summary>
    internal void NotifyExpansionChangedByUser() =>
        NodeExpansionChangedByUser?.Invoke(this, EventArgs.Empty);
}
