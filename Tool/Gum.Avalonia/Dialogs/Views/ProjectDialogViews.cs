using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Converters;
using Gum.Dialogs;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>
/// Base name for exposing a color's channels, with a live preview of the variable names. Twin of
/// the WPF <c>ExposeColorDialogView</c>.
/// </summary>
public sealed class ExposeColorDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public ExposeColorDialogView()
    {
        Width = 450;
        DialogWindow.SetDialogTitle(this, "Expose Color");

        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(ExposeColorDialogViewModel.Message)));
        Children.Add(message);

        TextBox baseName = new TextBox { Margin = new Thickness(0, 8, 0, 0) };
        baseName.Bind(TextBox.TextProperty, new Binding(nameof(ExposeColorDialogViewModel.BaseName)) { Mode = BindingMode.TwoWay });
        Children.Add(baseName);

        Children.Add(new TextBlock { Text = "Variables to create:", Margin = new Thickness(0, 12, 0, 2), Opacity = 0.7 });

        ItemsControl names = new ItemsControl
        {
            Margin = new Thickness(8, 0, 0, 0),
            ItemTemplate = new FuncDataTemplate<string>((_, _) =>
            {
                TextBlock name = new TextBlock { FontWeight = FontWeight.Bold };
                name.Bind(TextBlock.TextProperty, new Binding("."));
                return name;
            }),
        };
        names.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ExposeColorDialogViewModel.ExposedNames)));
        Children.Add(names);

        // Always laid out with room for one message, so the window does not jump when an error appears.
        TextBlock error = new TextBlock { MinHeight = 32, Margin = new Thickness(0, 10, 0, 0), Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap };
        error.Bind(TextBlock.TextProperty, new Binding(nameof(ExposeColorDialogViewModel.Error)));
        Children.Add(error);

        DialogViewHelpers.FocusAndSelectAllWhenShown(this, baseName);
    }
}

/// <summary>
/// Lists the screens, components, instances and variables that reference an element; selecting one
/// selects it in the tool. Twin of the WPF <c>DisplayReferencesDialogView</c>.
/// </summary>
public sealed class DisplayReferencesDialogView : DockPanel
{
    /// <summary>Builds the view.</summary>
    public DisplayReferencesDialogView()
    {
        MinWidth = 400;
        DialogWindow.SetDialogTitle(this, "References");

        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(DisplayReferencesDialog.Message)));
        SetDock(message, Dock.Top);
        Children.Add(message);

        ListBox references = new ListBox { Margin = new Thickness(0, 8, 0, 0), MaxHeight = 400 };
        references.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(DisplayReferencesDialog.References)));
        references.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(DisplayReferencesDialog.SelectedReference)) { Mode = BindingMode.TwoWay });
        references.Bind(IsVisibleProperty, new Binding(nameof(DisplayReferencesDialog.References) + ".Count") { Converter = NonZeroConverter.Instance });
        Children.Add(references);
    }
}

/// <summary>
/// Recent projects, favorites first; the star toggles a favorite and double-clicking loads. Twin of
/// the WPF <c>LoadRecentWindow</c> and its <c>RecentFileItem</c> row.
/// </summary>
public sealed class LoadRecentDialogView : DockPanel
{
    /// <summary>Builds the view.</summary>
    public LoadRecentDialogView()
    {
        MinWidth = 520;
        DialogWindow.SetDialogTitle(this, "Recent Gum Projects");

        ListBox items = new ListBox
        {
            MaxHeight = 480,
            ItemTemplate = new FuncDataTemplate<RecentItemViewModel>((_, _) => CreateRow()),
        };
        items.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(LoadRecentViewModel.FilteredItems)));
        items.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(LoadRecentViewModel.SelectedItem)) { Mode = BindingMode.TwoWay });
        DialogViewHelpers.AffirmOnDoubleTap(items);
        Children.Add(items);
    }

    private static Control CreateRow()
    {
        StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

        ToggleButton favorite = new ToggleButton { Content = "★", Padding = new Thickness(4, 0), MinWidth = 0 };
        ToolTip.SetTip(favorite, "Favorite");
        favorite.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(RecentItemViewModel.IsFavorite)) { Mode = BindingMode.TwoWay });
        row.Children.Add(favorite);

        TextBlock name = new TextBlock { FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
        name.Bind(TextBlock.TextProperty, new Binding(nameof(RecentItemViewModel.StrippedName)));
        row.Children.Add(name);

        TextBlock path = new TextBlock { FontSize = 10, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
        path.Bind(TextBlock.TextProperty, new Binding(nameof(RecentItemViewModel.FullPath)));
        row.Children.Add(path);

        return row;
    }
}

/// <summary>
/// Picks project files to import as screens, components, or behaviors, with a filter box and a
/// Browse button for files outside the project. Twin of the WPF <c>ImportFileView</c>.
/// </summary>
public sealed class ImportFileDialogView : DockPanel
{
    private readonly ListBox _files;

    /// <summary>Builds the view. The window title comes from the view model's <c>Title</c>.</summary>
    public ImportFileDialogView()
    {
        Width = 550;
        Height = 350;

        DockPanel filterRow = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
        TextBlock filterLabel = new TextBlock { Text = "Filter:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        SetDock(filterLabel, Dock.Left);
        filterRow.Children.Add(filterLabel);
        TextBox filter = new TextBox();
        filter.Bind(TextBox.TextProperty, new Binding(nameof(ImportBaseDialogViewModel.SearchText)) { Mode = BindingMode.TwoWay });
        filter.KeyDown += HandleFilterKeyDown;
        filterRow.Children.Add(filter);
        SetDock(filterRow, Dock.Top);
        Children.Add(filterRow);

        TextBlock note = new TextBlock
        {
            Margin = new Thickness(0, 5, 0, 0),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Text = "The list above shows all files in the project folder not yet imported.\n" +
                "To add files from outside of the project folder, click the Browse button. Files from outside of the content folder will be copied to the content folder.",
        };
        SetDock(note, Dock.Bottom);
        Children.Add(note);

        _files = new ListBox { SelectionMode = SelectionMode.Multiple };
        _files.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ImportBaseDialogViewModel.FilteredFiles)));
        _files.SelectionChanged += (_, _) => SyncSelectedFiles();
        DialogViewHelpers.AffirmOnDoubleTap(_files);
        Children.Add(_files);

        Button browse = new Button { Content = "Browse..." };
        browse.Bind(Button.CommandProperty, new Binding(nameof(ImportBaseDialogViewModel.BrowseCommand)));
        DialogWindow.SetAuxiliaryActions(this, browse);

        filter.AttachedToVisualTree += (_, _) => filter.Focus();
    }

    // The WPF view binds the list's selection into SelectedFiles through a behavior; this is that sync.
    private void SyncSelectedFiles()
    {
        if (DataContext is not ImportBaseDialogViewModel viewModel)
        {
            return;
        }
        viewModel.SelectedFiles.Clear();
        foreach (string file in _files.SelectedItems?.OfType<string>() ?? Enumerable.Empty<string>())
        {
            viewModel.SelectedFiles.Add(file);
        }
    }

    // Up and Down in the filter box move the list selection, so the keyboard never has to leave the box.
    private void HandleFilterKeyDown(object? sender, KeyEventArgs e)
    {
        int current = _files.SelectedIndex;
        int max = _files.ItemCount - 1;
        int? direction = e.Key switch
        {
            Key.Down when current < max => 1,
            Key.Up when current > 0 => -1,
            _ => null,
        };

        if (direction.HasValue)
        {
            _files.SelectedIndex = current + direction.Value;
            e.Handled = true;
        }
    }
}
