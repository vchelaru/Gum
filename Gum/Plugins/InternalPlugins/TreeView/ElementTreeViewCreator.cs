using Gum.Controls;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Grid = System.Windows.Controls.Grid;
using WpfInput = System.Windows.Input;

namespace Gum.Managers;

/// <summary>
/// Builds and themes the Project panel: the element tree, its search box, the collapse buttons, the
/// flat search results, and the Standards chip palette.
/// </summary>
/// <remarks>
/// The panel is WPF all the way down. It used to host the tree inside a <c>WindowsFormsHost</c>,
/// which is why the tree's events were previously threaded through this class as a dozen
/// constructor-style delegates - the host had to be wired at creation time. Now the tree is an
/// ordinary child, so callers subscribe to <see cref="ObjectTreeView"/> directly and only the
/// genuinely panel-local controls take callbacks.
/// </remarks>
internal class ElementTreeViewCreator
{
    #region Properties

    internal GumTreeView ObjectTreeView { get; private set; } = null!;
    internal ContextMenu ContextMenu { get; private set; } = null!;
    internal FlatSearchListBox FlatList { get; private set; } = null!;
    internal TextBox SearchTextBox { get; private set; } = null!;
    internal CheckBox DeepSearchCheckBox { get; private set; } = null!;
    internal Button CollapseAllButton { get; private set; } = null!;
    internal Button CollapseToElementButton { get; private set; } = null!;

    /// <summary>
    /// The experimental Standards chip palette pinned to the bottom of the Project panel. Hidden
    /// (collapsed) unless the UseStandardsPalette setting is on; the manager toggles its visibility
    /// and populates its chips.
    /// </summary>
    internal StandardsPaletteView StandardsPalette { get; private set; } = null!;

    #endregion

    /// <summary>
    /// Builds the panel. Tree events are not taken here - subscribe to <see cref="ObjectTreeView"/>.
    /// </summary>
    /// <param name="onFilterTextChanged">Called when the search text changes.</param>
    /// <param name="onSearchNodeSelected">Called when a search result is chosen.</param>
    /// <param name="onCollapseAll">Called when the Collapse All button is clicked.</param>
    /// <param name="onCollapseToElementLevel">Called when the Collapse To Element button is clicked.</param>
    /// <param name="onDeepSearchChecked">Called when the Include Variables box is checked.</param>
    public Grid CreateView(
        Action<string?> onFilterTextChanged,
        Action<SearchItemViewModel?> onSearchNodeSelected,
        Action onCollapseAll,
        Action onCollapseToElementLevel,
        Action onDeepSearchChecked)
    {
        CreateObjectTreeView();
        ContextMenu = new ContextMenu();

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

        StackPanel buttonPanel = CreateCollapseButtonsPanel(onCollapseAll, onCollapseToElementLevel);
        Grid.SetRow(buttonPanel, 0);
        grid.Children.Add(buttonPanel);

        TextBox searchBarUi = CreateSearchBoxUi(onFilterTextChanged, onSearchNodeSelected);
        Grid.SetRow(searchBarUi, 1);
        grid.Children.Add(searchBarUi);

        CheckBox checkBoxUi = CreateSearchCheckBoxUi(onDeepSearchChecked);
        checkBoxUi.Visibility = Visibility.Collapsed;
        checkBoxUi.Focusable = false;
        checkBoxUi.Margin = new Thickness(0, 2, 0, 0);
        Grid.SetRow(checkBoxUi, 2);
        grid.Children.Add(checkBoxUi);

        FlatList = CreateFlatSearchList(onSearchNodeSelected);
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

    private void CreateObjectTreeView()
    {
        ObjectTreeView = new GumTreeView
        {
            Name = "ObjectTreeView",
            IsSelectingOnPush = false,
            AllowDrop = true,
            AlwaysHaveOneNodeSelected = false,
            MultiSelectBehavior = MultiSelectBehavior.CtrlDown,
        };
    }

    /// <summary>
    /// Re-resolves everything the tree draws from theme resources. Icons re-tint themselves; there
    /// is no image list to rebuild.
    /// </summary>
    internal void ApplyThemeColors()
    {
        TreeIconRegistry.NotifyThemeChanged();
        UpdateTreeviewIcons();
    }

    /// <summary>
    /// Scales the row icons to the current UI font size.
    /// </summary>
    /// <param name="scale">Multiplier relative to the default icon size.</param>
    internal void UpdateTreeviewIcons(double scale = 1.0)
    {
        const double baseIconSize = 16;
        ObjectTreeView.IconSize = baseIconSize * scale;
    }

    private FlatSearchListBox CreateFlatSearchList(Action<SearchItemViewModel?> onSearchNodeSelected)
    {
        FlatSearchListBox list = new();
        list.SelectSearchNode += onSearchNodeSelected;
        return list;
    }

    private TextBox CreateSearchBoxUi(
        Action<string?> onFilterTextChanged,
        Action<SearchItemViewModel?> onSearchNodeSelected)
    {
        SearchTextBox = new TextBox();
        SearchTextBox.SetValue(TextFieldAssist.HasClearButtonProperty, true);
        SearchTextBox.SetValue(HintAssist.HintProperty, "Search...");
        SearchTextBox.SetValue(HintAssist.IsFloatingProperty, false);
        SearchTextBox.VerticalAlignment = VerticalAlignment.Center;
        SearchTextBox.TextChanged += (_, _) => onFilterTextChanged(SearchTextBox.Text);
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
                    onSearchNodeSelected(selectedItem);
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

    private StackPanel CreateCollapseButtonsPanel(Action onCollapseAll, Action onCollapseToElementLevel)
    {
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 4)
        };

        CollapseAllButton = CreateToolButton(
            PackIconKind.UnfoldLessHorizontal, "Collapse all nodes in the tree", onCollapseAll);
        CollapseToElementButton = CreateToolButton(
            PackIconKind.FileTree, "Collapse to element level (preserves folder expansion state)",
            onCollapseToElementLevel);

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

    private const double DefaultBaseFontSize = 12.0;
    private const double DefaultIconHeight = 14.0;

    internal void UpdateCollapseButtonSizes(double baseFontSize)
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

    private CheckBox CreateSearchCheckBoxUi(Action onDeepSearchChecked)
    {
        DeepSearchCheckBox = new CheckBox
        {
            IsChecked = false,
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Content = "Include Variables"
        };
        DeepSearchCheckBox.Checked += (_, _) => onDeepSearchChecked();

        return DeepSearchCheckBox;
    }

    internal void CollapseAll() => ObjectTreeView.CollapseAll();

    internal void CollapseToElementLevel() => CollapseElementNodesRecursively(ObjectTreeView.Nodes);

    /// <summary>
    /// Collapses element nodes while leaving folder nodes as the user left them.
    /// </summary>
    private static void CollapseElementNodesRecursively(GumTreeNodeCollection nodes)
    {
        foreach (GumTreeNode node in nodes)
        {
            // A node with a Tag is an element (Screen, Component, Behavior, Instance).
            if (node.Tag != null)
            {
                node.Collapse();
            }
            else if (node.IsTopElementContainerTreeNode() ||
                     node.IsScreensFolderTreeNode() ||
                     node.IsComponentsFolderTreeNode())
            {
                CollapseElementNodesRecursively(node.Nodes);
            }
        }
    }
}
