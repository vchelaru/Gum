using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Gum.ViewModels;

namespace Gum.Avalonia.Plugins.TreeView;

/// <summary>
/// The Avalonia Project panel: collapse buttons, the search box and its results, the element tree,
/// and the Standards chip palette, presented to <see cref="ElementTreeViewManager"/> as an
/// <see cref="IElementTreeView"/>. The counterpart of the WPF <c>WpfElementTreeView</c>.
/// </summary>
public sealed class AvaloniaElementTreeView : IElementTreeView
{
    private const double TreeMenuIconSize = 16;
    private const double DragThreshold = 4;

    private readonly DockPanel _content;
    private readonly AvaloniaGumTreeView _tree;
    private readonly TextBox _searchBox;
    private readonly CheckBox _deepSearch;
    private readonly ListBox _results;
    private readonly ObservableCollection<SearchItemViewModel> _resultItems;
    private readonly AvaloniaStandardsPalette _palette;
    private readonly Button _collapseAllButton;
    private readonly Button _collapseToElementButton;
    private readonly ContextMenu _contextMenu;

    private Point _resultPressPoint;
    private SearchItemViewModel? _resultDragCandidate;

    /// <summary>Builds the panel.</summary>
    public AvaloniaElementTreeView()
    {
        _tree = new AvaloniaGumTreeView
        {
            Margin = new Thickness(0, 4, 0, 0),
        };
        _tree.Selection.IsSelectingOnPush = false;
        _tree.Selection.AlwaysHaveOneNodeSelected = false;
        _tree.Selection.MultiSelectBehavior = MultiSelectBehavior.CtrlDown;
        _contextMenu = new ContextMenu();
        _resultItems = new ObservableCollection<SearchItemViewModel>();

        _collapseAllButton = ToolButton("Collapse all", "Collapse all nodes in the tree", () => CollapseAllRequested?.Invoke());
        _collapseToElementButton = ToolButton("Collapse to elements", "Collapse to element level (preserves folder expansion state)",
            () => CollapseToElementLevelRequested?.Invoke());
        StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 0, 0, 4) };
        buttons.Children.Add(_collapseAllButton);
        buttons.Children.Add(_collapseToElementButton);

        _searchBox = new TextBox { Watermark = "Search..." };
        _deepSearch = new CheckBox
        {
            Content = "Include Variables",
            IsVisible = false,
            Focusable = false,
            Margin = new Thickness(0, 2, 0, 0),
        };
        _results = new ListBox
        {
            ItemsSource = _resultItems,
            IsVisible = false,
            Margin = new Thickness(0, 4, 0, 0),
            ItemTemplate = new FuncDataTemplate<SearchItemViewModel>((item, _) => SearchResultRow(item)),
        };
        _palette = new AvaloniaStandardsPalette { IsVisible = false };

        Grid treeHost = new Grid();
        treeHost.Children.Add(_tree);
        treeHost.Children.Add(_results);

        _content = new DockPanel { Margin = new Thickness(4) };
        DockPanel.SetDock(buttons, global::Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(_searchBox, global::Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(_deepSearch, global::Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(_palette, global::Avalonia.Controls.Dock.Bottom);
        _content.Children.Add(buttons);
        _content.Children.Add(_searchBox);
        _content.Children.Add(_deepSearch);
        _content.Children.Add(_palette);
        _content.Children.Add(treeHost);

        WireSearch();
        WireTree();
        _palette.AddToCurrentRequested = typeName => AddStandardToCurrentRequested?.Invoke(typeName);
        _palette.EditDefaultsRequested = typeName => EditStandardDefaultsRequested?.Invoke(typeName);
    }

    #region IElementTreeView

    /// <inheritdoc/>
    public object Content => _content;

    /// <inheritdoc/>
    public GumTreeNodeCollection Nodes => _tree.Nodes;

    /// <inheritdoc/>
    public TreeSelectionModel Selection => _tree.Selection;

    /// <inheritdoc/>
    public bool IsPointerOver => _tree.IsPointerOver;

    /// <inheritdoc/>
    public GumTreeNode? NodeUnderPointer => _tree.NodeUnderPointer;

    /// <inheritdoc/>
    public bool IsKeyboardFocusWithin => _tree.IsKeyboardFocusWithin;

    /// <inheritdoc/>
    public bool IsDeepSearchChecked => _deepSearch.IsChecked == true;

    /// <inheritdoc/>
    public Func<IReadOnlyList<ContextMenuItemViewModel>>? ContextMenuProvider { get; set; }

    /// <inheritdoc/>
    public Func<string?>? CurrentElementNameProvider
    {
        get => _palette.CurrentElementNameProvider;
        set => _palette.CurrentElementNameProvider = value;
    }

    /// <inheritdoc/>
    public void FocusTree() => _tree.Focus();

    /// <inheritdoc/>
    public void FocusSearch() => _searchBox.Focus();

    /// <inheritdoc/>
    public void ClearSearchText() => _searchBox.Text = string.Empty;

    /// <inheritdoc/>
    public void EnsureVisible(GumTreeNode node) => _tree.EnsureVisible(node);

    /// <inheritdoc/>
    public void ShowSearchResults(IReadOnlyList<SearchItemViewModel>? results)
    {
        _results.IsVisible = results != null;
        _tree.IsVisible = results == null;
        UpdateDeepSearchVisibility();

        if (results == null)
        {
            return;
        }

        _resultItems.Clear();
        foreach (SearchItemViewModel result in results)
        {
            _resultItems.Add(result);
        }
        _results.SelectedIndex = _resultItems.Count > 0 ? 0 : -1;
    }

    /// <inheritdoc/>
    public void SetStandardsPaletteVisible(bool isVisible) => _palette.IsVisible = isVisible;

    /// <inheritdoc/>
    public void RefreshStandardsPaletteChips(IReadOnlyList<string> standardTypeNames) => _palette.RefreshChips(standardTypeNames);

    /// <inheritdoc/>
    public void SetSelectedStandardType(string? typeName) => _palette.SetSelectedStandardType(typeName);

    /// <inheritdoc/>
    public void ApplyThemeColors() => AvaloniaTreeIcons.NotifyThemeChanged();

    /// <inheritdoc/>
    public void UpdateCollapseButtonSizes(double baseFontSize)
    {
        _collapseAllButton.FontSize = baseFontSize;
        _collapseToElementButton.FontSize = baseFontSize;
    }

    /// <inheritdoc/>
    public event Action<string?>? SearchTextChanged;

    /// <inheritdoc/>
    public event Action<SearchItemViewModel?>? SearchResultChosen;

    /// <inheritdoc/>
    public event Action? DeepSearchChecked;

    /// <inheritdoc/>
    public event Action? CollapseAllRequested;

    /// <inheritdoc/>
    public event Action? CollapseToElementLevelRequested;

    /// <inheritdoc/>
    public event Action? NodeExpansionChangedByUser;

    /// <inheritdoc/>
    public event Action<GumTreeNode?>? PointerMoved;

    /// <inheritdoc/>
    public event Action<GumKeyEventArgs>? KeyDown;

    /// <inheritdoc/>
    public event Action<Exception>? UnhandledException;

    /// <inheritdoc/>
    public event EventHandler<TreeDropValidationEventArgs>? ValidateSortingDrop;

    /// <inheritdoc/>
    public event EventHandler<TreeDropEventArgs>? NodeSortingDropped;

    /// <inheritdoc/>
    public event EventHandler<TreeExternalDragEventArgs>? ExternalDragOver;

    /// <inheritdoc/>
    public event EventHandler<TreeExternalDragEventArgs>? ExternalDrop;

    /// <inheritdoc/>
    public event Action? DragEnded;

    /// <inheritdoc/>
    public event Action<string>? AddStandardToCurrentRequested;

    /// <inheritdoc/>
    public event Action<string>? EditStandardDefaultsRequested;

    #endregion

    #region Wiring

    private void WireTree()
    {
        _tree.KeyPressed += args => KeyDown?.Invoke(args);
        _tree.NodeExpansionChanged += () => NodeExpansionChangedByUser?.Invoke();
        _tree.PointerMovedOverNode += node => PointerMoved?.Invoke(node);
        _tree.UnhandledException += ex => UnhandledException?.Invoke(ex);
        _tree.ValidateSortingDrop += (_, e) => ValidateSortingDrop?.Invoke(this, e);
        _tree.NodeSortingDropped += (_, e) => NodeSortingDropped?.Invoke(this, e);
        _tree.ExternalDragOver += (_, e) => ExternalDragOver?.Invoke(this, e);
        _tree.ExternalDrop += (_, e) => ExternalDrop?.Invoke(this, e);
        _tree.DragEnded += () => DragEnded?.Invoke();
        _tree.ContextMenuRequested += ShowContextMenu;
    }

    private void ShowContextMenu()
    {
        IReadOnlyList<ContextMenuItemViewModel> items =
            ContextMenuProvider?.Invoke() ?? Array.Empty<ContextMenuItemViewModel>();
        if (items.Count == 0)
        {
            // Nothing applies to this selection; suppress rather than open an empty popup.
            return;
        }

        AvaloniaContextMenus.Populate(_contextMenu, items, TreeMenuIconSize);
        _contextMenu.Open(_tree);
    }

    private void WireSearch()
    {
        _searchBox.TextChanged += (_, _) => SearchTextChanged?.Invoke(_searchBox.Text);
        _searchBox.GotFocus += (_, _) => UpdateDeepSearchVisibility();
        _searchBox.LostFocus += (_, _) => UpdateDeepSearchVisibility();
        _searchBox.AddHandler(InputElement.KeyDownEvent, HandleSearchKeyDown, RoutingStrategies.Tunnel);
        _deepSearch.IsCheckedChanged += (_, _) =>
        {
            if (_deepSearch.IsChecked == true)
            {
                DeepSearchChecked?.Invoke();
            }
        };

        _results.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            _resultPressPoint = e.GetPosition(_results);
            _resultDragCandidate = SearchItemFrom(e.Source);
        }, RoutingStrategies.Tunnel);
        _results.AddHandler(InputElement.PointerReleasedEvent, (_, e) =>
        {
            _resultDragCandidate = null;
            if (SearchItemFrom(e.Source) is { } item)
            {
                SearchResultChosen?.Invoke(item);
            }
        }, RoutingStrategies.Bubble, handledEventsToo: true);
        _results.AddHandler(InputElement.PointerMovedEvent, (_, e) =>
        {
            if (_resultDragCandidate is not { } candidate || !e.GetCurrentPoint(_results).Properties.IsLeftButtonPressed)
            {
                return;
            }
            Point current = e.GetPosition(_results);
            if (Math.Abs(current.X - _resultPressPoint.X) < DragThreshold && Math.Abs(current.Y - _resultPressPoint.Y) < DragThreshold)
            {
                return;
            }
            _resultDragCandidate = null;
            StartResultDrag(e, candidate);
        }, RoutingStrategies.Tunnel);
    }

    // A search result stands for an element, instance or behavior that may not be realized in the
    // tree, so it travels as a tag with no node behind it.
    private static async void StartResultDrag(PointerEventArgs e, SearchItemViewModel item)
    {
        try
        {
            TreeDragPayload.SetTags(new[] { item.BackingObject });
            DataTransfer data = new DataTransfer();
            data.Add(DataTransferItem.Create(AvaloniaDragFormats.TreeNodes, "tags"));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy);
        }
        catch (Exception)
        {
            // A failed or cancelled drag must never destabilize the tool.
        }
        finally
        {
            TreeDragPayload.Clear();
        }
    }

    private void HandleSearchKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _searchBox.Text = string.Empty;
                e.Handled = true;
                _tree.Focus();
                break;
            case Key.Back when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                _searchBox.Text = string.Empty;
                e.Handled = true;
                break;
            case Key.Down:
                if (_results.SelectedIndex < _resultItems.Count - 1)
                {
                    _results.SelectedIndex++;
                    _results.ScrollIntoView(_results.SelectedIndex);
                }
                e.Handled = true;
                break;
            case Key.Up:
                if (_results.SelectedIndex > 0)
                {
                    _results.SelectedIndex--;
                    _results.ScrollIntoView(_results.SelectedIndex);
                }
                e.Handled = true;
                break;
            case Key.Enter:
                e.Handled = true;
                _tree.Focus();
                if (_results.SelectedItem is SearchItemViewModel selected)
                {
                    SearchResultChosen?.Invoke(selected);
                    _searchBox.Text = string.Empty;
                }
                break;
        }
    }

    private void UpdateDeepSearchVisibility() =>
        _deepSearch.IsVisible = _searchBox.IsKeyboardFocusWithin || _results.IsVisible;

    private static SearchItemViewModel? SearchItemFrom(object? source)
    {
        for (Visual? current = source as Visual; current != null; current = current.GetVisualParent())
        {
            if (current is Control { DataContext: SearchItemViewModel item })
            {
                return item;
            }
        }
        return null;
    }

    private static Control SearchResultRow(SearchItemViewModel? item)
    {
        StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        if (item != null && AvaloniaTreeIcons.CreateIcon(IconIndexFor(item.BackingObject), 14) is { } icon)
        {
            row.Children.Add(icon);
        }
        row.Children.Add(new TextBlock { Text = item?.Display ?? string.Empty, VerticalAlignment = VerticalAlignment.Center });
        return row;
    }

    private static int IconIndexFor(object? backingObject) => backingObject switch
    {
        ScreenSave => TreeNodeImageIndices.ScreenImageIndex,
        ComponentSave => TreeNodeImageIndices.ComponentImageIndex,
        StandardElementSave => TreeNodeImageIndices.StandardElementImageIndex,
        InstanceSave { Locked: true } => TreeNodeImageIndices.LockedInstanceImageIndex,
        InstanceSave => TreeNodeImageIndices.InstanceImageIndex,
        BehaviorSave => TreeNodeImageIndices.BehaviorImageIndex,
        _ => TreeNodeImageIndices.TransparentImageIndex,
    };

    private static Button ToolButton(string text, string toolTip, Action onClick)
    {
        Button button = new Button
        {
            Content = text,
            Padding = new Thickness(6, 2),
            FontSize = 11,
        };
        ToolTip.SetTip(button, toolTip);
        button.Click += (_, _) => onClick();
        return button;
    }

    #endregion
}

/// <summary>Creates the Avalonia Project panel for <see cref="ElementTreeViewManager"/>.</summary>
public sealed class AvaloniaElementTreeViewFactory : IElementTreeViewFactory
{
    /// <inheritdoc/>
    public IElementTreeView Create() => new AvaloniaElementTreeView();
}
