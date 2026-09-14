using Gum.Controls;
using Gum.Extensions;
using Gum.Input;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Gum.ViewModels;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Grid = System.Windows.Controls.Grid;
using WpfInput = System.Windows.Input;

namespace Gum.Managers;

/// <summary>
/// The WPF Project panel: the element tree, its search box, the collapse buttons, the flat search
/// results, and the Standards chip palette, presented to <see cref="ElementTreeViewManager"/> as an
/// <see cref="IElementTreeView"/>. It translates WPF input and drag-and-drop into the interface's
/// neutral events; what the panel shows is decided by the manager.
/// </summary>
internal class WpfElementTreeView : IElementTreeView
{
    private const double DefaultBaseFontSize = 12.0;
    private const double DefaultIconHeight = 14.0;
    private const double TreeMenuIconSize = 16;

    private readonly Grid _content;
    private readonly WpfInput.Cursor _addCursor;

    #region Controls

    internal GumTreeView ObjectTreeView { get; }
    internal ContextMenu ContextMenu { get; }
    internal FlatSearchListBox FlatList { get; private set; } = null!;
    internal TextBox SearchTextBox { get; private set; } = null!;
    internal CheckBox DeepSearchCheckBox { get; private set; } = null!;
    internal Button CollapseAllButton { get; private set; } = null!;
    internal Button CollapseToElementButton { get; private set; } = null!;

    /// <summary>
    /// The experimental Standards chip palette pinned to the bottom of the Project panel. Hidden
    /// (collapsed) unless the UseStandardsPalette setting is on.
    /// </summary>
    internal StandardsPaletteView StandardsPalette { get; private set; } = null!;

    #endregion

    public WpfElementTreeView()
    {
        _addCursor = LoadAddCursor();
        ObjectTreeView = new GumTreeView
        {
            Name = "ObjectTreeView",
            IsSelectingOnPush = false,
            AllowDrop = true,
            AlwaysHaveOneNodeSelected = false,
            MultiSelectBehavior = MultiSelectBehavior.CtrlDown,
        };
        ContextMenu = new ContextMenu();
        _content = CreateView();
        WireTreeEvents();
    }

    #region IElementTreeView

    /// <inheritdoc/>
    public object Content => _content;

    /// <inheritdoc/>
    public GumTreeNodeCollection Nodes => ObjectTreeView.Nodes;

    /// <inheritdoc/>
    public TreeSelectionModel Selection => ObjectTreeView.Selection;

    /// <inheritdoc/>
    public bool IsPointerOver
    {
        get
        {
            // Measured against the bounds rather than read from IsMouseOver, which stays false while
            // a drag has captured the mouse.
            Point position = WpfInput.Mouse.GetPosition(ObjectTreeView);
            return position.X >= 0 && position.Y >= 0 &&
                position.X <= ObjectTreeView.ActualWidth && position.Y <= ObjectTreeView.ActualHeight;
        }
    }

    /// <inheritdoc/>
    public GumTreeNode? NodeUnderPointer => ObjectTreeView.GetNodeAt(WpfInput.Mouse.GetPosition(ObjectTreeView));

    /// <inheritdoc/>
    public bool IsKeyboardFocusWithin => ObjectTreeView.IsKeyboardFocusWithin;

    /// <inheritdoc/>
    public bool IsDeepSearchChecked => DeepSearchCheckBox.IsChecked is true;

    /// <inheritdoc/>
    public Func<IReadOnlyList<ContextMenuItemViewModel>>? ContextMenuProvider { get; set; }

    /// <inheritdoc/>
    public Func<string?>? CurrentElementNameProvider
    {
        get => StandardsPalette.CurrentElementNameProvider;
        set => StandardsPalette.CurrentElementNameProvider = value;
    }

    /// <inheritdoc/>
    public void FocusTree() => ObjectTreeView.Focus();

    /// <inheritdoc/>
    public void FocusSearch() => SearchTextBox.Focus();

    /// <inheritdoc/>
    public void ClearSearchText() => SearchTextBox.Text = null;

    /// <inheritdoc/>
    public void EnsureVisible(GumTreeNode node) => ObjectTreeView.EnsureVisible(node);

    /// <inheritdoc/>
    public void ShowSearchResults(IReadOnlyList<SearchItemViewModel>? results)
    {
        bool showResults = results != null;
        FlatList.Visibility = showResults ? Visibility.Visible : Visibility.Collapsed;
        ObjectTreeView.Visibility = showResults ? Visibility.Collapsed : Visibility.Visible;

        if (results == null)
        {
            return;
        }

        FlatList.FlatList.Items.Clear();
        foreach (SearchItemViewModel result in results)
        {
            FlatList.FlatList.Items.Add(result);
        }

        if (FlatList.FlatList.Items.Count > 0)
        {
            FlatList.FlatList.SelectedIndex = 0;
        }
    }

    /// <inheritdoc/>
    public void SetStandardsPaletteVisible(bool isVisible) =>
        StandardsPalette.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

    /// <inheritdoc/>
    public void RefreshStandardsPaletteChips(IReadOnlyList<string> standardTypeNames) =>
        StandardsPalette.RefreshChips(standardTypeNames);

    /// <inheritdoc/>
    public void SetSelectedStandardType(string? typeName) => StandardsPalette.SetSelectedStandardType(typeName);

    /// <summary>
    /// Re-resolves everything the tree draws from theme resources. Icons re-tint themselves; there
    /// is no image list to rebuild.
    /// </summary>
    public void ApplyThemeColors()
    {
        TreeIconRegistry.NotifyThemeChanged();
        UpdateTreeviewIcons();
    }

    /// <inheritdoc/>
    public void UpdateCollapseButtonSizes(double baseFontSize)
    {
        double scale = baseFontSize / DefaultBaseFontSize;
        double iconHeight = DefaultIconHeight * scale;

        if (CollapseAllButton?.Content is PackIcon collapseAllIcon)
        {
            collapseAllIcon.Width = iconHeight;
            collapseAllIcon.Height = iconHeight;
        }

        if (CollapseToElementButton?.Content is PackIcon collapseToElementIcon)
        {
            collapseToElementIcon.Width = iconHeight;
            collapseToElementIcon.Height = iconHeight;
        }
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

    #region Tree events

    private void WireTreeEvents()
    {
        ObjectTreeView.KeyDown += (_, e) =>
        {
            GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
            KeyDown?.Invoke(keyArgs);
            e.Handled = keyArgs.Handled;
        };
        ObjectTreeView.ContextMenuOpening += HandleContextMenuOpening;
        ObjectTreeView.ContextMenu = ContextMenu;
        ObjectTreeView.NodeExpansionChangedByUser += (_, _) => NodeExpansionChangedByUser?.Invoke();
        ObjectTreeView.UnhandledException += ex => UnhandledException?.Invoke(ex);
        ObjectTreeView.MouseMove += (_, e) => PointerMoved?.Invoke(ObjectTreeView.GetNodeAt(e.GetPosition(ObjectTreeView)));

        ObjectTreeView.DragOver += HandleTreeDragOver;
        ObjectTreeView.Drop += HandleTreeDrop;
        ObjectTreeView.ValidateSortingDrop += (_, e) => ValidateSortingDrop?.Invoke(this, e);
        ObjectTreeView.NodeSortingDropped += (_, e) => NodeSortingDropped?.Invoke(this, e);
        ObjectTreeView.GiveFeedback += HandleTreeGiveFeedback;
        ObjectTreeView.QueryContinueDrag += (_, e) =>
        {
            if (e.Action != DragAction.Continue)
            {
                DragEnded?.Invoke();
            }
        };

        StandardsPalette.AddToCurrentRequested = typeName => AddStandardToCurrentRequested?.Invoke(typeName);
        StandardsPalette.EditDefaultsRequested = typeName => EditStandardDefaultsRequested?.Invoke(typeName);
    }

    private void HandleContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ContextMenu.Items.Clear();

        IReadOnlyList<ContextMenuItemViewModel> items =
            ContextMenuProvider?.Invoke() ?? Array.Empty<ContextMenuItemViewModel>();
        foreach (ContextMenuItemViewModel item in items)
        {
            ContextMenu.Items.Add(item.ToMenuItem(TreeMenuIconSize));
        }

        if (ContextMenu.Items.Count == 0)
        {
            // Nothing applies to this selection; suppress rather than open an empty popup.
            e.Handled = true;
        }
    }

    // Files and Standards chips. Node drags are handled (and marked handled) by the control itself,
    // so they never reach this handler.
    private void HandleTreeDragOver(object sender, DragEventArgs e)
    {
        bool hasFiles = e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
        bool hasChip = e.Data?.GetDataPresent(DragDropManager.StandardElementNameDataFormat) == true;

        TreeExternalDragEventArgs args = new(
            hasFiles ? e.Data!.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>() : null,
            hasChip ? e.Data!.GetData(DragDropManager.StandardElementNameDataFormat) as string ?? string.Empty : null,
            ObjectTreeView.GetNodeAt(e.GetPosition(ObjectTreeView)));
        ExternalDragOver?.Invoke(this, args);

        if (hasFiles || hasChip)
        {
            e.Effects = args.Accepted ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }
    }

    private void HandleTreeDrop(object sender, DragEventArgs e)
    {
        string[]? files = e.Data?.GetData(DataFormats.FileDrop) as string[];
        string? standardTypeName = files == null
            ? e.Data?.GetData(DragDropManager.StandardElementNameDataFormat) as string
            : null;

        if (files == null && standardTypeName == null)
        {
            return;
        }

        ExternalDrop?.Invoke(this, new TreeExternalDragEventArgs(
            files, standardTypeName, ObjectTreeView.GetNodeAt(e.GetPosition(ObjectTreeView))));
    }

    private void HandleTreeGiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        if (InputLibrary.Cursor.Self.IsInWindow)
        {
            e.UseDefaultCursors = false;
            WpfInput.Mouse.SetCursor(_addCursor);
            e.Handled = true;
        }
    }

    private static WpfInput.Cursor LoadAddCursor()
    {
        try
        {
            using System.IO.Stream? stream = typeof(Gum.Program).Assembly
                .GetManifestResourceStream("Gum.Content.Cursors.AddCursor.cur");

            return stream != null ? new WpfInput.Cursor(stream) : WpfInput.Cursors.Arrow;
        }
        catch
        {
            // This has crashed on at least one machine. It is only a cursor, so tolerate it.
            return WpfInput.Cursors.Arrow;
        }
    }

    #endregion

    #region Building the panel

    private Grid CreateView()
    {
        Grid grid = new() { Margin = new Thickness(4) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        // Row 4: the Standards chip palette, pinned below the tree.
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        ObjectTreeView.Margin = new Thickness(0, 4, 0, 0);
        Grid.SetRow(ObjectTreeView, 3);
        grid.Children.Add(ObjectTreeView);

        StackPanel buttonPanel = CreateCollapseButtonsPanel();
        Grid.SetRow(buttonPanel, 0);
        grid.Children.Add(buttonPanel);

        TextBox searchBarUi = CreateSearchBoxUi();
        Grid.SetRow(searchBarUi, 1);
        grid.Children.Add(searchBarUi);

        CheckBox checkBoxUi = CreateSearchCheckBoxUi();
        checkBoxUi.Visibility = Visibility.Collapsed;
        checkBoxUi.Focusable = false;
        checkBoxUi.Margin = new Thickness(0, 2, 0, 0);
        Grid.SetRow(checkBoxUi, 2);
        grid.Children.Add(checkBoxUi);

        FlatList = new FlatSearchListBox();
        FlatList.SelectSearchNode += item => SearchResultChosen?.Invoke(item);
        FlatList.HorizontalAlignment = HorizontalAlignment.Stretch;
        FlatList.VerticalAlignment = VerticalAlignment.Stretch;
        FlatList.Margin = new Thickness(0, 4, 0, 0);
        FlatList.Visibility = Visibility.Collapsed;
        Grid.SetRow(FlatList, 3);
        grid.Children.Add(FlatList);

        StandardsPalette = new StandardsPaletteView { Visibility = Visibility.Collapsed };
        Grid.SetRow(StandardsPalette, 4);
        grid.Children.Add(StandardsPalette);

        searchBarUi.GotKeyboardFocus += (_, _) => UpdateCheckBoxVisibility();
        searchBarUi.LostKeyboardFocus += (_, _) => UpdateCheckBoxVisibility();
        FlatList.IsVisibleChanged += (_, _) => UpdateCheckBoxVisibility();

        void UpdateCheckBoxVisibility()
        {
            bool textBoxFocused = SearchTextBox.IsKeyboardFocusWithin;
            bool listViewVisible = FlatList.Visibility == Visibility.Visible;

            checkBoxUi.Visibility = textBoxFocused || listViewVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        ApplyThemeColors();

        return grid;
    }

    /// <summary>
    /// Scales the row icons to the current UI font size.
    /// </summary>
    /// <param name="scale">Multiplier relative to the default icon size.</param>
    private void UpdateTreeviewIcons(double scale = 1.0)
    {
        const double baseIconSize = 16;
        ObjectTreeView.IconSize = baseIconSize * scale;
    }

    private TextBox CreateSearchBoxUi()
    {
        SearchTextBox = new TextBox();
        SearchTextBox.SetValue(TextFieldAssist.HasClearButtonProperty, true);
        SearchTextBox.SetValue(HintAssist.HintProperty, "Search...");
        SearchTextBox.SetValue(HintAssist.IsFloatingProperty, false);
        SearchTextBox.VerticalAlignment = VerticalAlignment.Center;
        SearchTextBox.TextChanged += (_, _) => SearchTextChanged?.Invoke(SearchTextBox.Text);
        SearchTextBox.PreviewKeyDown += (_, args) =>
        {
            bool isCtrlDown = WpfInput.Keyboard.IsKeyDown(WpfInput.Key.LeftCtrl)
                || WpfInput.Keyboard.IsKeyDown(WpfInput.Key.RightCtrl);

            if (args.Key == WpfInput.Key.Escape)
            {
                SearchTextBox.Text = null;
                args.Handled = true;
                ObjectTreeView.Focus();
            }
            else if (args.Key == WpfInput.Key.Back && isCtrlDown)
            {
                SearchTextBox.Text = null;
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Down)
            {
                if (FlatList.FlatList.SelectedIndex < FlatList.FlatList.Items.Count - 1)
                {
                    FlatList.FlatList.SelectedIndex++;
                    BringSelectedIntoView();
                }
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Up)
            {
                if (FlatList.FlatList.SelectedIndex > 0)
                {
                    FlatList.FlatList.SelectedIndex--;
                    BringSelectedIntoView();
                }
                args.Handled = true;
            }
            else if (args.Key == WpfInput.Key.Enter)
            {
                args.Handled = true;
                ObjectTreeView.Focus();

                if (FlatList.FlatList.SelectedItem is SearchItemViewModel selectedItem)
                {
                    SearchResultChosen?.Invoke(selectedItem);
                    SearchTextBox.Text = null;
                }
            }
        };

        return SearchTextBox;

        void BringSelectedIntoView()
        {
            if (FlatList.FlatList.SelectedItem is { } selected)
            {
                FlatList.Dispatcher.BeginInvoke(
                    () => FlatList.FlatList.ScrollIntoView(selected),
                    DispatcherPriority.Loaded);
            }
        }
    }

    private StackPanel CreateCollapseButtonsPanel()
    {
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 4)
        };

        CollapseAllButton = CreateToolButton(
            PackIconKind.UnfoldLessHorizontal, "Collapse all nodes in the tree", () => CollapseAllRequested?.Invoke());
        CollapseToElementButton = CreateToolButton(
            PackIconKind.FileTree, "Collapse to element level (preserves folder expansion state)",
            () => CollapseToElementLevelRequested?.Invoke());

        panel.Children.Add(CollapseAllButton);
        panel.Children.Add(CollapseToElementButton);

        return panel;
    }

    private static Button CreateToolButton(PackIconKind iconKind, string toolTip, Action onClick)
    {
        Button button = new()
        {
            Content = new PackIcon { Kind = iconKind, Width = DefaultIconHeight, Height = DefaultIconHeight },
            Margin = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(4, 2, 4, 2),
            ToolTip = toolTip,
            Style = Application.Current.TryFindResource("MaterialDesignToolForegroundButton") as Style
        };

        RippleAssist.SetIsDisabled(button, true);
        button.Click += (_, _) => onClick();

        return button;
    }

    private CheckBox CreateSearchCheckBoxUi()
    {
        DeepSearchCheckBox = new CheckBox
        {
            IsChecked = false,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Content = "Include Variables"
        };
        DeepSearchCheckBox.Checked += (_, _) => DeepSearchChecked?.Invoke();

        return DeepSearchCheckBox;
    }

    #endregion
}

/// <summary>Creates the WPF Project panel for <see cref="ElementTreeViewManager"/>.</summary>
internal class WpfElementTreeViewFactory : IElementTreeViewFactory
{
    /// <inheritdoc/>
    public IElementTreeView Create() => new WpfElementTreeView();
}
