using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ImportFromGumxPlugin.ViewModels;

namespace Gum.Avalonia.Plugins.PluginDialogs;

/// <summary>
/// The Import from .gumx dialog: the source (local file or URL) with Browse and Enter to load a
/// preview, the tree of items to import with a check box per row and a Details link on differing
/// Standards, the post-import warning, and the destination subfolder. Twin of the WPF
/// <c>ImportFromGumxView</c>.
/// </summary>
public sealed class ImportFromGumxView : Grid
{
    private readonly TextBox _sourcePath;
    private readonly TreeView _tree;

    /// <summary>Builds the view.</summary>
    public ImportFromGumxView()
    {
        // The dialog opens at a fixed size, as in the WPF head, so a long item list scrolls
        // instead of growing the window.
        Width = 600;
        Height = 560;
        RowDefinitions = new RowDefinitions("Auto,*");

        StackPanel source = new StackPanel();

        StackPanel sourceKind = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        RadioButton localFile = new RadioButton { Content = "Local File", GroupName = "ImportFromGumxSource", Margin = new Thickness(0, 0, 12, 0) };
        localFile.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ImportFromGumxViewModel.IsLocalFile)) { Mode = BindingMode.TwoWay });
        RadioButton url = new RadioButton { Content = "URL", GroupName = "ImportFromGumxSource" };
        url.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ImportFromGumxViewModel.IsUrl)) { Mode = BindingMode.TwoWay });
        sourceKind.Children.Add(localFile);
        sourceKind.Children.Add(url);
        source.Children.Add(sourceKind);

        Grid pathRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 4) };
        _sourcePath = new TextBox { Margin = new Thickness(0, 0, 4, 0), VerticalContentAlignment = VerticalAlignment.Center };
        _sourcePath.Bind(TextBox.TextProperty, new Binding(nameof(ImportFromGumxViewModel.SourcePath)) { Mode = BindingMode.TwoWay });
        _sourcePath.AddHandler(KeyDownEvent, HandleSourcePathKeyDown, RoutingStrategies.Tunnel);
        Button browse = new Button { Content = "Browse...", Padding = new Thickness(8, 2) };
        browse.Bind(Button.CommandProperty, new Binding(nameof(ImportFromGumxViewModel.BrowseCommand)));
        browse.Bind(IsVisibleProperty, new Binding(nameof(ImportFromGumxViewModel.IsBrowseButtonVisible)));
        Grid.SetColumn(browse, 1);
        pathRow.Children.Add(_sourcePath);
        pathRow.Children.Add(browse);
        source.Children.Add(pathRow);

        TextBlock error = new TextBlock { Foreground = Brushes.OrangeRed, Margin = new Thickness(0, 0, 0, 2), TextWrapping = TextWrapping.Wrap };
        error.Bind(TextBlock.TextProperty, new Binding(nameof(ImportFromGumxViewModel.ErrorMessage)));
        error.Bind(IsVisibleProperty, new Binding(nameof(ImportFromGumxViewModel.IsErrorMessageVisible)));
        source.Children.Add(error);

        TextBlock status = new TextBlock { FontStyle = FontStyle.Italic, Opacity = 0.7 };
        status.Bind(TextBlock.TextProperty, new Binding(nameof(ImportFromGumxViewModel.LoadingStatus)));
        status.Bind(IsVisibleProperty, new Binding(nameof(ImportFromGumxViewModel.IsLoadingStatusVisible)));
        source.Children.Add(status);
        Children.Add(source);

        _tree = new TreeView
        {
            BorderThickness = new Thickness(0),
            ItemTemplate = new FuncTreeDataTemplate<ImportTreeNodeViewModel>(CreateNodeRow, node => node.Children),
        };
        _tree.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ImportFromGumxViewModel.RootNodes)));
        _tree.Styles.Add(new Style(selector => selector.OfType<TreeViewItem>())
        {
            Setters = { new Setter(TreeViewItem.IsExpandedProperty, true) },
        });

        TextBlock warning = new TextBlock { Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
        warning.Bind(TextBlock.TextProperty, new Binding(nameof(ImportFromGumxViewModel.WarningMessage)));
        warning.Bind(IsVisibleProperty, new Binding(nameof(ImportFromGumxViewModel.IsWarningMessageVisible)));

        Grid destination = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(0, 8, 0, 0) };
        destination.Children.Add(new TextBlock { Text = "Destination subfolder:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        TextBox subfolder = new TextBox { VerticalContentAlignment = VerticalAlignment.Center };
        subfolder.Bind(TextBox.TextProperty, new Binding(nameof(ImportFromGumxViewModel.DestinationSubfolder)) { Mode = BindingMode.TwoWay });
        Grid.SetColumn(subfolder, 1);
        destination.Children.Add(subfolder);

        Grid listArea = new Grid { RowDefinitions = new RowDefinitions("*,Auto,Auto") };
        Grid.SetRow(warning, 1);
        Grid.SetRow(destination, 2);
        listArea.Children.Add(_tree);
        listArea.Children.Add(warning);
        listArea.Children.Add(destination);

        Grid preview = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), Margin = new Thickness(0, 8, 0, 0) };
        preview.Bind(IsVisibleProperty, new Binding(nameof(ImportFromGumxViewModel.IsPreviewVisible)));
        preview.Children.Add(new TextBlock { Text = "Select items to import", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        Border listBorder = new Border { BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray, Padding = new Thickness(4), Child = listArea };
        Grid.SetRow(listBorder, 1);
        preview.Children.Add(listBorder);
        Grid.SetRow(preview, 1);
        Children.Add(preview);
    }

    /// <summary>The source path box, for tests.</summary>
    internal TextBox SourcePathTextBox => _sourcePath;

    /// <summary>The item tree, for tests.</summary>
    internal TreeView Tree => _tree;

    private async void HandleSourcePathKeyDown(object? sender, KeyEventArgs e)
    {
        // Enter loads the preview rather than pressing the dialog's Import button.
        if (e.Key == Key.Enter && DataContext is ImportFromGumxViewModel viewModel && viewModel.LoadPreviewCommand.CanExecute(null))
        {
            e.Handled = true;
            await viewModel.LoadPreviewCommand.ExecuteAsync(null);
        }
    }

    private Control CreateNodeRow(ImportTreeNodeViewModel node, INameScope scope)
    {
        CheckBox checkBox = new CheckBox { Content = new TextBlock { Text = node.DisplayName } };
        // One-way from the view model; a click goes through the view model's toggle, and the box
        // then shows whatever state the view model settled on.
        checkBox.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ImportTreeNodeViewModel.IsChecked)) { Mode = BindingMode.OneWay });
        checkBox.Click += (_, _) =>
        {
            node.Toggle();
            checkBox.SetCurrentValue(ToggleButton.IsCheckedProperty, node.IsChecked);
        };

        Button details = new Button
        {
            Content = new TextBlock { Text = "Details...", TextDecorations = TextDecorations.Underline },
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        details.Bind(IsVisibleProperty, new Binding(nameof(ImportTreeNodeViewModel.IsDetailsButtonVisible)));
        details.Click += (_, _) => (DataContext as ImportFromGumxViewModel)?.ShowStandardDiffCommand.Execute(node);

        StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(checkBox);
        row.Children.Add(details);
        return row;
    }
}

/// <summary>The read-only list of one Standard's differences from the destination project. Twin of the WPF <c>StandardDiffDetailsView</c>.</summary>
public sealed class StandardDiffDetailsView : ScrollViewer
{
    /// <summary>Builds the view.</summary>
    public StandardDiffDetailsView()
    {
        MinWidth = 480;
        MaxHeight = 500;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        ItemsControl rows = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<StandardDiffRowViewModel>((_, _) =>
            {
                Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("120,*"), Margin = new Thickness(0, 2) };
                TextBlock kind = new TextBlock { Opacity = 0.7, Margin = new Thickness(0, 0, 8, 0) };
                kind.Bind(TextBlock.TextProperty, new Binding("Kind"));
                TextBlock summary = new TextBlock { TextWrapping = TextWrapping.Wrap };
                summary.Bind(TextBlock.TextProperty, new Binding("Summary"));
                Grid.SetColumn(summary, 1);
                row.Children.Add(kind);
                row.Children.Add(summary);
                return row;
            }),
        };
        rows.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(StandardDiffDetailsViewModel.Rows)));
        Content = rows;
    }
}
