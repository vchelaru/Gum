using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Shell;

/// <summary>
/// The five-region panel layout of the WPF <c>MainPanelControl</c>: left column, center column
/// split top/bottom, right column split top/bottom, with splitters between them. Each region is a
/// tab control bound to the tab manager's list for that location.
/// </summary>
public sealed class MainPanelView : Grid
{
    private readonly AvaloniaTabManager _tabs;
    private readonly Grid _rightGrid;
    private readonly List<Control> _toolPanels = new List<Control>();

    /// <summary>Builds the layout over the tab manager.</summary>
    public MainPanelView(AvaloniaTabManager tabs)
    {
        _tabs = tabs;

        ColumnDefinitions = new ColumnDefinitions
        {
            new ColumnDefinition(new GridLength(tabs.LeftColumnWidth, GridUnitType.Pixel)),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(new GridLength(tabs.CenterColumnWidth, GridUnitType.Pixel)),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
        };

        Grid centerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions
            {
                new RowDefinition(new GridLength(1, GridUnitType.Star)),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(new GridLength(2, GridUnitType.Star)),
            },
        };
        centerGrid.Children.Add(CreateRegion(tabs.CenterTop, 0, 0));
        _toolPanels.Add(centerGrid);
        centerGrid.Children.Add(CreateSplitter(GridResizeDirection.Rows, 1, 0));
        centerGrid.Children.Add(CreateRegion(tabs.CenterBottom, 2, 0));

        Grid rightGrid = _rightGrid = new Grid
        {
            RowDefinitions = new RowDefinitions
            {
                new RowDefinition(new GridLength(1, GridUnitType.Star)),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(new GridLength(tabs.BottomRightHeight, GridUnitType.Pixel)),
            },
        };
        rightGrid.Children.Add(CreateRegion(tabs.RightTop, 0, 0));
        rightGrid.Children.Add(CreateSplitter(GridResizeDirection.Rows, 1, 0));
        Grid bottomRight = CreateRegion(tabs.RightBottom, 2, 0);
        rightGrid.Children.Add(bottomRight);
        GridSplitter rightSplitter = (GridSplitter)rightGrid.Children[1];

        Grid left = CreateRegion(tabs.Left, 0, 0);
        GridSplitter leftSplitter = CreateSplitter(GridResizeDirection.Columns, 0, 1);
        GridSplitter middleSplitter = CreateSplitter(GridResizeDirection.Columns, 0, 3);
        _toolPanels.AddRange(new Control[] { left, leftSplitter, middleSplitter, bottomRight, rightSplitter });

        Children.Add(left);
        Children.Add(leftSplitter);
        SetColumn(centerGrid, 2);
        Children.Add(centerGrid);
        Children.Add(middleSplitter);
        SetColumn(rightGrid, 4);
        Children.Add(rightGrid);

        // Persist the user's splitter positions through the tab manager.
        LayoutUpdated += (_, _) =>
        {
            // Hidden panels measure 0; the sizes to bring back stay as the user left them.
            if (!_tabs.IsToolsVisible)
            {
                return;
            }
            _tabs.LeftColumnWidth = ColumnDefinitions[0].ActualWidth;
            _tabs.CenterColumnWidth = ColumnDefinitions[2].ActualWidth;
            _tabs.BottomRightHeight = rightGrid.RowDefinitions[2].ActualHeight;
        };

        // View > Hide Tools: collapse everything but the canvas region; Show Tools brings back the
        // sizes the manager kept.
        _tabs.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AvaloniaTabManager.IsToolsVisible))
            {
                ApplyToolsVisibility();
            }
        };
        ApplyToolsVisibility();
    }

    private void ApplyToolsVisibility()
    {
        bool visible = _tabs.IsToolsVisible;
        // Collapsing the panels (not only zeroing their lengths) also takes their controls out of
        // the tab order, so focus cannot land in a hidden panel.
        foreach (Control panel in _toolPanels)
        {
            panel.IsVisible = visible;
        }
        ColumnDefinitions[0].Width = new GridLength(visible ? _tabs.LeftColumnWidth : 0, GridUnitType.Pixel);
        ColumnDefinitions[2].Width = new GridLength(visible ? _tabs.CenterColumnWidth : 0, GridUnitType.Pixel);
        _rightGrid.RowDefinitions[2].Height = new GridLength(visible ? _tabs.BottomRightHeight : 0, GridUnitType.Pixel);
    }

    private Grid CreateRegion(ObservableCollection<AvaloniaPluginTab> source, int row, int column)
    {
        TabControl tabControl = new TabControl
        {
            ItemsSource = source,
            ItemTemplate = new FuncDataTemplate<AvaloniaPluginTab>((tab, _) =>
            {
                if (tab?.HeaderContent is { } custom)
                {
                    return custom;
                }
                return CreateHeader(tab);
            }),
            ContentTemplate = new FuncDataTemplate<AvaloniaPluginTab>((tab, _) =>
                tab?.Content is Control control ? control : new ContentControl { Content = tab?.Content }),
        };
        tabControl.Classes.Add(GumChromeStyles.MainTabsClass);
        // The strip shows the window background and the content sits on Surface01, as the WPF
        // MainTabControl.Style (the content host is styled in GumChromeStyles).
        tabControl.WithThemeResource(TemplatedControl.BackgroundProperty, "Frb.Brushes.Background");
        tabControl.SelectionChanged += (_, _) =>
        {
            // While the manager reorders the list the tab control picks neighbors on its own; the
            // manager settles the selection itself once the list is in order.
            if (!_tabs.IsSyncing && tabControl.SelectedItem is AvaloniaPluginTab selected)
            {
                _tabs.Select(selected);
            }
        };
        // A plugin selecting its tab (Edit > Properties, say) brings it to the front here.
        _tabs.TabSelected += tab =>
        {
            if (source.Contains(tab) && tabControl.SelectedItem != tab)
            {
                tabControl.SelectedItem = tab;
            }
        };

        Grid host = new Grid().WithThemeResource(Panel.BackgroundProperty, "Frb.Brushes.Background");
        host.Children.Add(tabControl);
        SetRow(host, row);
        SetColumn(host, column);
        return host;
    }

    /// <summary>The name of the close button in a tab's header.</summary>
    public const string CloseButtonName = "PART_CloseTab";

    // The WPF tab header: the title, then a small flat close button for a closable tab. A middle
    // click on the header closes it too.
    private static Control CreateHeader(AvaloniaPluginTab? tab)
    {
        TextBlock title = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        title.Bind(TextBlock.TextProperty, new Binding(nameof(AvaloniaPluginTab.Title)));

        Button close = new Button
        {
            Name = CloseButtonName,
            Width = 14,
            Height = 14,
            Padding = new Thickness(0),
            Margin = new Thickness(6, 0, -1, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = GumFluentIcons.Create(FluentIcons.Common.Icon.Dismiss, 10),
        }.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        close.Classes.Add(GumChromeStyles.FlatButtonClass);
        ToolTip.SetTip(close, "Close tab");
        close.Bind(IsVisibleProperty, new Binding(nameof(AvaloniaPluginTab.CanClose)));
        close.Click += (_, e) =>
        {
            tab?.Hide();
            e.Handled = true;
        };

        StackPanel header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(title);
        header.Children.Add(close);
        header.PointerReleased += (_, e) =>
        {
            if (tab is { CanClose: true } && e.InitialPressMouseButton == MouseButton.Middle)
            {
                tab.Hide();
            }
        };
        return header;
    }

    /// <summary>The gap between regions; the splitter's grab area is the gap itself.</summary>
    internal const double SplitterThickness = 4;

    // As the WPF splitters: an invisible grab area, so the gap between regions is the only
    // separator (#4694).
    private static GridSplitter CreateSplitter(GridResizeDirection direction, int row, int column)
    {
        bool isColumn = direction == GridResizeDirection.Columns;
        GridSplitter splitter = new GridSplitter
        {
            ResizeDirection = direction,
            Background = Brushes.Transparent,
            // Transparent rather than unset, so the whole grab area takes the pointer.
            Template = new FuncControlTemplate<GridSplitter>((_, _) => new Border { Background = Brushes.Transparent }),
        };
        if (isColumn)
        {
            // The theme's MinWidth is wider than the gap.
            splitter.MinWidth = SplitterThickness;
            splitter.Width = SplitterThickness;
            splitter.HorizontalAlignment = HorizontalAlignment.Center;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
        }
        else
        {
            splitter.MinHeight = SplitterThickness;
            splitter.Height = SplitterThickness;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Center;
        }
        SetRow(splitter, row);
        SetColumn(splitter, column);
        return splitter;
    }
}
