using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Themes;
using Gum.Plugins.FontsPlugin;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Settings;

/// <summary>
/// The per-font character ranges of the Fonts page as one compact list: a header, one line per font (the
/// project-wide default line first: font, ranges, remove) and an Add button, with a font's error or warning
/// under its line only when it has one. A label-less row, shown for <see cref="Gum.Plugins.InternalPlugins.VariableGrid.GumDisplayers.FontRanges"/>;
/// the member's value is the <see cref="FontsViewModel"/>.
/// </summary>
public sealed class FontRangesDisplay : DataUiDisplayBase
{
    private readonly StackPanel _body;
    private readonly TextBlock _hint;

    /// <summary>Builds the displayer.</summary>
    public FontRangesDisplay()
    {
        _hint = CreateHintTextBlock();
        _hint.Margin = new Thickness(0, 0, 4, 4);

        _body = new StackPanel { Spacing = 4 };
        _body.Children.Add(_hint);
        _body.Children.Add(BuildTableHeader());

        ItemsControl rows = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<FontRangeEntryViewModel>((_, _) => BuildRow()),
        };
        rows.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(FontsViewModel.Entries)));
        _body.Children.Add(rows);

        Button add = new Button { Content = "Add Font Range", HorizontalAlignment = HorizontalAlignment.Left };
        add.Bind(Button.CommandProperty, new Binding(nameof(FontsViewModel.AddEntryCommand)));
        _body.Children.Add(add);

        Content = _body;
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        if (InstanceMember.Value is FontsViewModel viewModel && !ReferenceEquals(_body.DataContext, viewModel))
        {
            _body.DataContext = viewModel;
        }
        RefreshHint(_hint);
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        result = _body.DataContext;
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object? value) => ApplyValueResult.Success;

    private static ColumnDefinitions Columns() => new ColumnDefinitions("3*,4*,Auto");

    private static Control BuildTableHeader()
    {
        Grid header = new Grid { ColumnDefinitions = Columns() };
        TextBlock font = new TextBlock { Text = "Font", Margin = new Thickness(0, 0, 4, 0) }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        TextBlock rangesHeader = new TextBlock { Text = "Ranges" }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        Grid.SetColumn(rangesHeader, 1);
        header.Children.Add(font);
        header.Children.Add(rangesHeader);
        return header;
    }

    private static Control BuildRow()
    {
        Grid row = new Grid { ColumnDefinitions = Columns(), RowDefinitions = new RowDefinitions("Auto,Auto"), Margin = new Thickness(0, 2) };

        // The default row names no font; every other row picks one from the fonts the project uses, or types one.
        TextBlock defaultLabel = new TextBlock { Text = "All other fonts", FontStyle = FontStyle.Italic, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        defaultLabel.Bind(Visual.IsVisibleProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)));
        row.Children.Add(defaultLabel);

        ComboBox family = new ComboBox { IsEditable = true, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 4, 0), MinHeight = 26 };
        family.Bind(ComboBox.ItemsSourceProperty, new Binding($"{nameof(DataContext)}.{nameof(FontsViewModel.AvailableFonts)}")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(ItemsControl) },
        });
        family.Bind(ComboBox.TextProperty, new Binding(nameof(FontRangeEntryViewModel.FontFamily))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
        });
        family.Bind(Visual.IsVisibleProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)) { Converter = BoolConverters.Not });
        CommitOnEnter(family);
        row.Children.Add(family);

        TextBox ranges = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 26 };
        ranges.Bind(TextBox.TextProperty, new Binding(nameof(FontRangeEntryViewModel.Ranges))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
        });
        ranges.Bind(TextBox.IsReadOnlyProperty, new Binding(nameof(FontRangeEntryViewModel.IsRangesReadOnly)));
        CommitOnEnter(ranges);
        Grid.SetColumn(ranges, 1);
        row.Children.Add(ranges);

        Button remove = new Button
        {
            Content = GumFluentIcons.Create(FluentIcons.Common.Icon.Dismiss, 12),
            Margin = new Thickness(4, 0, 0, 0),
            Classes = { GumChromeStyles.IconButtonClass },
        };
        ToolTip.SetTip(remove, "Remove this row");
        remove.Bind(Button.CommandProperty, new Binding($"{nameof(DataContext)}.{nameof(FontsViewModel.RemoveEntryCommand)}")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(ItemsControl) },
        });
        remove.Bind(Button.CommandParameterProperty, new Binding());
        // The default row keeps an invisible button so its column lines up with the other rows.
        remove.Bind(Visual.OpacityProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)) { Converter = new FuncValueConverter<bool, double>(isDefault => isDefault ? 0 : 1) });
        remove.Bind(InputElement.IsHitTestVisibleProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)) { Converter = BoolConverters.Not });
        remove.Bind(InputElement.FocusableProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)) { Converter = BoolConverters.Not });
        Grid.SetColumn(remove, 2);
        row.Children.Add(remove);

        TextBlock error = new TextBlock { TextWrapping = TextWrapping.Wrap }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Error");
        error.Bind(TextBlock.TextProperty, new Binding(nameof(FontRangeEntryViewModel.ErrorText)));
        error.Bind(Visual.IsVisibleProperty, new Binding(nameof(FontRangeEntryViewModel.HasError)));
        Grid.SetRow(error, 1);
        Grid.SetColumnSpan(error, 3);
        row.Children.Add(error);

        TextBlock warning = new TextBlock { TextWrapping = TextWrapping.Wrap }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Icon.Manilla");
        warning.Bind(TextBlock.TextProperty, new Binding(nameof(FontRangeEntryViewModel.WarningText)));
        warning.Bind(Visual.IsVisibleProperty, new Binding(nameof(FontRangeEntryViewModel.HasWarning)));
        Grid.SetRow(warning, 1);
        Grid.SetColumnSpan(warning, 3);
        row.Children.Add(warning);

        return row;
    }

    // A text edit commits when the box loses focus; Enter gives the focus up so it commits at once too.
    private static void CommitOnEnter(Control box)
    {
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                TopLevel.GetTopLevel(box)?.FocusManager?.ClearFocus();
                e.Handled = true;
            }
        };
    }
}
