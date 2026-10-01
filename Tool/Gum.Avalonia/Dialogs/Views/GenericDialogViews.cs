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
        // Selectable so part of a message (a path, an error) can be copied; the window copies the
        // whole message on the copy gesture when nothing is selected.
        SelectableTextBlock message = new SelectableTextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(MessageDialogViewModel.Message)));
        Children.Add(message);
    }
}

/// <summary>
/// Prompts for a string: message, optional prefix, text box, validation error, optional check box.
/// Only the message scrolls (a rename prompt can list every affected reference); the text box and
/// what follows it stay pinned beneath it, next to the window's buttons.
/// </summary>
public sealed class GetUserStringDialogView : Grid
{
    /// <summary>Builds the view.</summary>
    public GetUserStringDialogView()
    {
        // The WPF view's width; the text box takes whatever the prefix leaves.
        Width = 450;
        RowDefinitions = new RowDefinitions("*,Auto,Auto,Auto");
        DialogWindow.SetScrollContent(this, false);

        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.Message)));
        Children.Add(new ScrollViewer
        {
            Content = message,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        });

        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(0, 8, 0, 0) };
        Grid.SetRow(row, 1);
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
        Grid.SetRow(error, 2);
        Children.Add(error);

        CheckBox checkBox = new CheckBox { Margin = new Thickness(0, 8, 0, 0) };
        checkBox.Bind(ContentControl.ContentProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.CheckboxText)));
        checkBox.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.IsCheckboxChecked)) { Mode = BindingMode.TwoWay });
        checkBox.Bind(IsVisibleProperty, new Binding(nameof(GetUserStringDialogBaseViewModel.CheckboxText)) { Converter = NotNullConverter.Instance });
        Grid.SetRow(checkBox, 3);
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

/// <summary>
/// Shows a message and a list of options to pick one from. The selected option has keyboard focus
/// once the window opens, so the arrow keys change the choice and Enter confirms it.
/// </summary>
public sealed class ChoiceDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public ChoiceDialogView()
    {
        // The WPF view's width; a long option wraps at it instead of widening the window.
        Width = 450;
        Spacing = 8;
        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(ChoiceDialogViewModel.Message)));
        Children.Add(message);

        ListBox options = new ListBox
        {
            MaxHeight = 300,
            ItemTemplate = new FuncDataTemplate<string>((_, _) =>
            {
                TextBlock option = new TextBlock { TextWrapping = TextWrapping.Wrap };
                option.Bind(TextBlock.TextProperty, new Binding());
                return option;
            }),
        };
        options.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ChoiceDialogViewModel.OptionValues)));
        options.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(ChoiceDialogViewModel.SelectedValue)) { Mode = BindingMode.TwoWay });
        Children.Add(options);

        DialogWindow.FocusWhenOpened(options, () => options.ContainerFromIndex(options.SelectedIndex)?.Focus());
    }
}

/// <summary>
/// Lists loaded plugins with enable check boxes on one tab and the folder scan on another, with Copy
/// Scan on the dialog's button row. Twin of the WPF <c>PluginsDialogView</c>.
/// </summary>
public sealed class PluginsDialogView : Border
{
    /// <summary>Builds the view.</summary>
    public PluginsDialogView()
    {
        // Fixed, as the WPF window stops sizing to content once open: the unwrapped scan would
        // otherwise widen the window when its tab is picked.
        Width = 500;
        // Each tab scrolls inside a fixed-height dialog, so switching tabs does not resize the window.
        DialogWindow.SetPreferredHeight(this, 450);
        DialogWindow.SetScrollContent(this, false);

        ItemsControl plugins = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<PluginItemViewModel>((_, _) =>
            {
                CheckBox box = new CheckBox();
                box.Bind(ContentControl.ContentProperty, new Binding(nameof(PluginItemViewModel.DisplayText)));
                box.Bind(CheckBox.IsCheckedProperty, new Binding(nameof(PluginItemViewModel.IsEnabled)) { Mode = BindingMode.TwoWay });
                box.Bind(IsEnabledProperty, new Binding(nameof(PluginItemViewModel.CanToggle)));
                box.Bind(ToolTip.TipProperty, new Binding(nameof(PluginItemViewModel.ToolTip)));
                ToolTip.SetShowOnDisabled(box, true);
                return box;
            }),
        };
        plugins.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PluginsDialogViewModel.Plugins)));
        ScrollViewer pluginList = new ScrollViewer
        {
            Content = plugins,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        // Read-only but selectable, for pasting into a bug report. Unwrapped and monospace: these
        // are long file paths, and wrapping breaks them mid-path.
        TextBox scan = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4),
            FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,DejaVu Sans Mono,monospace"),
            VerticalContentAlignment = VerticalAlignment.Top,
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(scan, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(scan, ScrollBarVisibility.Auto);
        scan.Bind(TextBox.TextProperty, new Binding(nameof(PluginsDialogViewModel.Diagnostics)) { Mode = BindingMode.OneWay });

        Child = new TabControl
        {
            Items =
            {
                new TabItem { Header = "Plugins", Content = pluginList },
                new TabItem { Header = "Folder scan", Content = scan },
            },
        };

        // On the dialog's own button row next to Close: a button inside the tab reads as though it
        // belongs to the text box.
        StackPanel copyContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        copyContent.Children.Add(GumFluentIcons.Create(FluentIcons.Common.Icon.Copy, 16));
        copyContent.Children.Add(new TextBlock { Text = "Copy Scan", VerticalAlignment = VerticalAlignment.Center });
        Button copy = new Button { Content = copyContent };
        ToolTip.SetTip(copy, "Copy the folder scan to the clipboard");
        copy.Bind(Button.CommandProperty, new Binding(nameof(PluginsDialogViewModel.CopyDiagnosticsCommand)));
        DialogWindow.SetAuxiliaryActions(this, copy);
    }
}
