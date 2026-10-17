using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Themes;
using Gum.SettingsWindow;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>
/// The Settings window: a search box and the page list on the left, the selected page's title, scope
/// and view on the right. The pages' views come from <see cref="SettingsPageViewRegistry"/>.
/// </summary>
public sealed class SettingsDialogView : Grid
{
    /// <summary>Width the window opens at.</summary>
    public const double WindowWidth = 860;

    /// <summary>Height the window opens at (shrinks on a shorter screen).</summary>
    public const double WindowHeight = 560;

    private const double TitleScale = 1.5;

    private readonly SettingsPageViewRegistry _pageViews;

    /// <summary>Builds the view.</summary>
    public SettingsDialogView()
    {
        _pageViews = new SettingsPageViewRegistry();

        Width = WindowWidth;
        DialogWindow.SetPreferredHeight(this, WindowHeight);
        DialogWindow.SetScrollContent(this, false);
        // The page list is as wide as its longest name, so a large zoom does not clip names.
        ColumnDefinitions = new ColumnDefinitions("Auto,*");

        Control navigation = BuildNavigation();
        Control page = BuildPage();
        SetColumn(page, 1);
        Children.Add(navigation);
        Children.Add(page);
    }

    private Control BuildNavigation()
    {
        TextBox search = new TextBox { Watermark = "Search settings", Margin = new Thickness(0, 0, 0, 8) };
        search.Bind(TextBox.TextProperty, new Binding(nameof(SettingsDialogViewModel.SearchText)) { Mode = BindingMode.TwoWay });
        DockPanel.SetDock(search, Dock.Top);

        ListBox pages = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<SettingsNavigationEntry>((_, _) => BuildEntry()),
        };
        pages.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(SettingsDialogViewModel.NavigationEntries)));
        pages.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(SettingsDialogViewModel.SelectedEntry)) { Mode = BindingMode.TwoWay });
        // A section header is a label, not a choice: keep the pointer and keyboard off it.
        pages.ContainerPrepared += (_, e) =>
        {
            bool isHeader = e.Container.DataContext is SettingsNavigationEntry { IsHeader: true };
            e.Container.IsHitTestVisible = !isHeader;
            e.Container.Focusable = !isHeader;
        };

        DockPanel navigation = new DockPanel { Margin = new Thickness(0, 0, 12, 0), MinWidth = 200 };
        navigation.Children.Add(search);
        navigation.Children.Add(pages);
        return navigation;
    }

    private static Control BuildEntry()
    {
        TextBlock header = new TextBlock { FontWeight = FontWeight.Medium, Margin = new Thickness(0, 8, 0, 0) }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        header.Bind(TextBlock.TextProperty, new Binding(nameof(SettingsNavigationEntry.Title)));
        header.Bind(Visual.IsVisibleProperty, new Binding(nameof(SettingsNavigationEntry.IsHeader)));

        TextBlock page = new TextBlock { Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        page.Bind(TextBlock.TextProperty, new Binding(nameof(SettingsNavigationEntry.Title)));
        page.Bind(Visual.IsVisibleProperty, new Binding(nameof(SettingsNavigationEntry.IsHeader))
        {
            Converter = global::Avalonia.Data.Converters.BoolConverters.Not,
        });

        return new Grid { Children = { header, page } };
    }

    private Control BuildPage()
    {
        ContentControl content = new ContentControl
        {
            ContentTemplate = new FuncDataTemplate<SettingsPage>((page, _) => page == null ? null : BuildSelectedPage(page)),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        content.Bind(ContentControl.ContentProperty, new Binding(nameof(SettingsDialogViewModel.SelectedPage)));
        return content;
    }

    // The standard for a page: its title, scope line and view scroll together as one page, so a page
    // view is plain content that grows. A reused view with scrollers of its own is given the window's
    // height under a title that stays put instead.
    private Control BuildSelectedPage(SettingsPage page)
    {
        TextBlock title = new TextBlock { Text = page.Title, FontWeight = FontWeight.Medium };
        // Larger than the body text at every zoom level, so it follows the base font size the tool zooms by.
        title.Bind(TextBlock.FontSizeProperty, title.GetResourceObservable(
            "ControlContentThemeFontSize", size => (size is double baseSize ? baseSize : 12d) * TitleScale));

        TextBlock scope = new TextBlock { Margin = new Thickness(0, 2, 0, 12) }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        scope.Bind(TextBlock.TextProperty, new Binding($"{nameof(DataContext)}.{nameof(SettingsDialogViewModel.SelectedScopeText)}")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(SettingsDialogView) },
        });

        StackPanel heading = new StackPanel { Children = { title, scope } };
        Control view = _pageViews.CreateView(page.Content);

        if (_pageViews.ScrollsItself(page.Content.GetType()))
        {
            DockPanel fixedTitle = new DockPanel();
            DockPanel.SetDock(heading, Dock.Top);
            fixedTitle.Children.Add(heading);
            fixedTitle.Children.Add(view);
            return fixedTitle;
        }

        return new ScrollViewer
        {
            Content = new StackPanel { Margin = new Thickness(0, 0, 8, 0), Children = { heading, view } },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
    }
}
