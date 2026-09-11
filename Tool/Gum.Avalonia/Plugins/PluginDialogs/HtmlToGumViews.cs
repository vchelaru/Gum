using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Themes;
using HtmlToGumPlugin;

namespace Gum.Avalonia.Plugins.PluginDialogs;

/// <summary>
/// The Import HTML options dialog: the source (local file or URL) with Browse, the CSS root
/// selector, the screen name, the viewport, the responsive flag and the destination subfolder.
/// Twin of the WPF <c>ImportHtmlOptionsView</c>.
/// </summary>
public sealed class ImportHtmlOptionsView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public ImportHtmlOptionsView()
    {
        Width = 460;

        StackPanel sourceKind = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        RadioButton localFile = new RadioButton { Content = "Local file", GroupName = "ImportHtmlSource", Margin = new Thickness(0, 0, 12, 0) };
        localFile.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ImportHtmlOptionsViewModel.IsLocalFile)) { Mode = BindingMode.TwoWay });
        RadioButton url = new RadioButton { Content = "URL", GroupName = "ImportHtmlSource" };
        url.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ImportHtmlOptionsViewModel.IsUrl)) { Mode = BindingMode.TwoWay });
        sourceKind.Children.Add(localFile);
        sourceKind.Children.Add(url);
        Children.Add(sourceKind);

        Grid pathRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 0, 0, 12) };
        TextBox path = new TextBox { Margin = new Thickness(0, 0, 4, 0), VerticalContentAlignment = VerticalAlignment.Center };
        path.Bind(TextBox.TextProperty, new Binding(nameof(ImportHtmlOptionsViewModel.HtmlPath)) { Mode = BindingMode.TwoWay });
        Button browse = new Button { Content = "Browse…", Padding = new Thickness(8, 2) };
        browse.Bind(Button.CommandProperty, new Binding(nameof(ImportHtmlOptionsViewModel.BrowseCommand)));
        Grid.SetColumn(browse, 1);
        pathRow.Children.Add(path);
        pathRow.Children.Add(browse);
        Children.Add(pathRow);

        Grid fields = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("148,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
        };
        AddField(fields, 0, "CSS root selector", BoundTextBox(nameof(ImportHtmlOptionsViewModel.Selector)));
        AddField(fields, 1, "Screen name", BoundTextBox(nameof(ImportHtmlOptionsViewModel.ScreenName)));
        StackPanel viewport = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 8) };
        TextBox width = new TextBox { Width = 80 };
        width.Bind(TextBox.TextProperty, new Binding(nameof(ImportHtmlOptionsViewModel.Width)) { Mode = BindingMode.TwoWay });
        TextBox height = new TextBox { Width = 80 };
        height.Bind(TextBox.TextProperty, new Binding(nameof(ImportHtmlOptionsViewModel.Height)) { Mode = BindingMode.TwoWay });
        viewport.Children.Add(width);
        viewport.Children.Add(height);
        AddField(fields, 2, "Viewport W×H", viewport);
        CheckBox noResponsive = new CheckBox { Content = "Disable responsive units (--no-responsive)", Margin = new Thickness(0, 0, 0, 8) };
        noResponsive.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ImportHtmlOptionsViewModel.NoResponsive)) { Mode = BindingMode.TwoWay });
        Grid.SetRow(noResponsive, 3);
        Grid.SetColumn(noResponsive, 1);
        fields.Children.Add(noResponsive);
        AddField(fields, 4, "Destination subfolder", BoundTextBox(nameof(ImportHtmlOptionsViewModel.DestinationSubfolder), bottomMargin: 0));
        Children.Add(fields);

        TextBlock hint = new TextBlock { Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        hint.Bind(TextBlock.TextProperty, new Binding(nameof(ImportHtmlOptionsViewModel.SubfolderHint)));
        Children.Add(hint);
    }

    private static TextBox BoundTextBox(string path, double bottomMargin = 8)
    {
        TextBox box = new TextBox { Margin = new Thickness(0, 0, 0, bottomMargin) };
        box.Bind(TextBox.TextProperty, new Binding(path) { Mode = BindingMode.TwoWay });
        return box;
    }

    private static void AddField(Grid grid, int row, string label, Control editor)
    {
        TextBlock text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(text, row);
        Grid.SetRow(editor, row);
        Grid.SetColumn(editor, 1);
        grid.Children.Add(text);
        grid.Children.Add(editor);
    }
}

/// <summary>
/// The dialog after an HTML import: the summary, and the converter log behind a Show details
/// toggle. Twin of the WPF <c>ImportHtmlResultView</c>.
/// </summary>
public sealed class ImportHtmlResultView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public ImportHtmlResultView()
    {
        Width = 420;

        TextBlock summary = new TextBlock { TextWrapping = TextWrapping.Wrap };
        summary.Bind(TextBlock.TextProperty, new Binding(nameof(ImportHtmlResultViewModel.Summary)));
        Children.Add(summary);

        Button toggle = new Button { Margin = new Thickness(0, 12, 0, 0), Padding = new Thickness(8, 2), HorizontalAlignment = HorizontalAlignment.Left };
        toggle.Bind(ContentControl.ContentProperty, new Binding(nameof(ImportHtmlResultViewModel.DetailsButtonText)));
        toggle.Bind(Button.CommandProperty, new Binding(nameof(ImportHtmlResultViewModel.ToggleDetailsCommand)));
        toggle.Bind(IsVisibleProperty, new Binding(nameof(ImportHtmlResultViewModel.HasDetails)));
        Children.Add(toggle);

        TextBox details = new TextBox
        {
            Height = 220,
            Margin = new Thickness(0, 8, 0, 0),
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace"),
        };
        details.Bind(TextBox.TextProperty, new Binding(nameof(ImportHtmlResultViewModel.Details)));
        details.Bind(IsVisibleProperty, new Binding(nameof(ImportHtmlResultViewModel.IsDetailsVisible)));
        Children.Add(details);
    }
}
