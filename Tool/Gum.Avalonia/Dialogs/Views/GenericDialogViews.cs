using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Converters;
using Gum.Avalonia.Themes;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>Shows <see cref="MessageDialogViewModel.Message"/>.</summary>
public sealed class MessageDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public MessageDialogView()
    {
        Spacing = 8;
        MaxWidth = 700;
        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(MessageDialogViewModel.Message)));
        Children.Add(message);
    }
}

/// <summary>Prompts for a string: message, optional prefix, text box, validation error, optional check box.</summary>
public sealed class GetUserStringDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public GetUserStringDialogView()
    {
        // The WPF view's width; the text box takes whatever the prefix leaves.
        Width = 450;

        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.Message)));
        Children.Add(message);

        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(0, 8, 0, 0) };
        TextBlock prefix = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
        prefix.Bind(TextBlock.TextProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.Prefix)));
        prefix.Bind(IsVisibleProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.Prefix)) { Converter = NotNullConverter.Instance });
        TextBox textBox = new TextBox();
        Grid.SetColumn(textBox, 1);
        textBox.Bind(TextBox.TextProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.Value)) { Mode = BindingMode.TwoWay });
        row.Children.Add(prefix);
        row.Children.Add(textBox);
        Children.Add(row);

        TextBlock error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Error")
            .WithThemeResource(TextBlock.FontSizeProperty, FrbThemeResources.CaptionFontSizeKey);
        error.Bind(TextBlock.TextProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.Error)));
        error.Bind(IsVisibleProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.Error)) { Converter = NotNullConverter.Instance });
        Children.Add(error);

        CheckBox checkBox = new CheckBox { Margin = new Thickness(0, 8, 0, 0) };
        checkBox.Bind(ContentControl.ContentProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.CheckboxText)));
        checkBox.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.IsCheckboxChecked)) { Mode = BindingMode.TwoWay });
        checkBox.Bind(IsVisibleProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.CheckboxText)) { Converter = NotNullConverter.Instance });
        Children.Add(checkBox);

        DialogWindow.FocusWhenOpened(textBox, () =>
        {
            if (DataContext is GetUserStringDialogBaseViewModel viewModel)
            {
                if (viewModel.PreSelect)
                {
                    textBox.SelectAll();
                }
                // As the WPF view does on load: the error and the disabled OK show before any typing.
                viewModel.Validate();
            }
        });
    }
}

/// <summary>Shows a message and a list of options to pick one from.</summary>
public sealed class ChoiceDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public ChoiceDialogView()
    {
        Spacing = 8;
        MinWidth = 360;
        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(ChoiceDialogViewModel.Message)));
        Children.Add(message);

        ListBox options = new ListBox { MaxHeight = 300 };
        options.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ChoiceDialogViewModel.OptionValues)));
        options.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(ChoiceDialogViewModel.SelectedValue)) { Mode = BindingMode.TwoWay });
        Children.Add(options);
    }
}

/// <summary>Lists loaded plugins with enable check boxes and the scan diagnostics.</summary>
public sealed class PluginsDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public PluginsDialogView()
    {
        Spacing = 8;
        MinWidth = 480;

        ItemsControl plugins = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<PluginItemViewModel>((_, _) =>
            {
                CheckBox box = new CheckBox();
                box.Bind(ContentControl.ContentProperty, new Binding(nameof(PluginItemViewModel.DisplayText)));
                box.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(PluginItemViewModel.IsEnabled)) { Mode = BindingMode.TwoWay });
                return box;
            }),
        };
        plugins.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PluginsDialogViewModel.Plugins)));
        Children.Add(new ScrollViewer { Content = plugins, MaxHeight = 300 });

        TextBox diagnostics = new TextBox { IsReadOnly = true, AcceptsReturn = true, MaxHeight = 160, TextWrapping = TextWrapping.Wrap };
        diagnostics.Bind(TextBox.TextProperty, new Binding(nameof(PluginsDialogViewModel.Diagnostics)));
        Children.Add(diagnostics);

        Button copy = new Button { Content = "Copy diagnostics", HorizontalAlignment = HorizontalAlignment.Left };
        copy.Bind(Button.CommandProperty, new Binding(nameof(PluginsDialogViewModel.CopyDiagnosticsCommand)));
        Children.Add(copy);
    }
}
