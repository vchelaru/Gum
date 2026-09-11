using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
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
        centerGrid.Children.Add(CreateSplitter(GridResizeDirection.Rows, 1, 0));
        centerGrid.Children.Add(CreateRegion(tabs.CenterBottom, 2, 0));

        Grid rightGrid = new Grid
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
        rightGrid.Children.Add(CreateRegion(tabs.RightBottom, 2, 0));

        Children.Add(CreateRegion(tabs.Left, 0, 0));
        Children.Add(CreateSplitter(GridResizeDirection.Columns, 0, 1));
        SetColumn(centerGrid, 2);
        Children.Add(centerGrid);
        Children.Add(CreateSplitter(GridResizeDirection.Columns, 0, 3));
        SetColumn(rightGrid, 4);
        Children.Add(rightGrid);

        // Persist the user's splitter positions through the tab manager.
        LayoutUpdated += (_, _) =>
        {
            _tabs.LeftColumnWidth = ColumnDefinitions[0].ActualWidth;
            _tabs.CenterColumnWidth = ColumnDefinitions[2].ActualWidth;
            _tabs.BottomRightHeight = rightGrid.RowDefinitions[2].ActualHeight;
        };
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
                TextBlock header = new TextBlock();
                header.Bind(TextBlock.TextProperty, new Binding(nameof(AvaloniaPluginTab.Title)));
                return header;
            }),
            ContentTemplate = new FuncDataTemplate<AvaloniaPluginTab>((tab, _) =>
                tab?.Content is Control control ? control : new ContentControl { Content = tab?.Content }),
        };
        tabControl.Classes.Add(GumChromeStyles.MainTabsClass);
        tabControl.SelectionChanged += (_, _) =>
        {
            if (tabControl.SelectedItem is AvaloniaPluginTab selected)
            {
                _tabs.Select(selected);
            }
        };

        Grid host = new Grid().WithThemeResource(Panel.BackgroundProperty, "Frb.Surface01");
        host.Children.Add(tabControl);
        SetRow(host, row);
        SetColumn(host, column);
        return host;
    }

    private static GridSplitter CreateSplitter(GridResizeDirection direction, int row, int column)
    {
        GridSplitter splitter = new GridSplitter
        {
            ResizeDirection = direction,
        }.WithThemeResource(TemplatedControl.BackgroundProperty, "Frb.Brushes.Border");
        if (direction == GridResizeDirection.Columns)
        {
            splitter.Width = 4;
            splitter.HorizontalAlignment = HorizontalAlignment.Center;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
        }
        else
        {
            splitter.Height = 4;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Center;
        }
        SetRow(splitter, row);
        SetColumn(splitter, column);
        return splitter;
    }
}
