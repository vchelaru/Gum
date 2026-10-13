using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaDataUi;
using Gum.Avalonia.Themes;
using Gum.Plugins.FontsPlugin;

namespace Gum.Avalonia.Panels;

/// <summary>
/// The Fonts tab: a Character Ranges table (the project-wide default row, then one row per font) above
/// a property grid of the generation settings, over the <see cref="FontsViewModel"/> the plugin hands
/// to the tab manager. The grid rows are laid out by the shared <see cref="FontsGridPresenter"/>.
/// </summary>
public sealed class FontsView : DockPanel
{
    private readonly FontsGridPresenter _presenter;

    /// <summary>Builds the view.</summary>
    public FontsView()
    {
        SettingsGrid = new DataUiGrid();
        _presenter = new FontsGridPresenter(SettingsGrid);

        Control ranges = BuildRangesSection();
        SetDock(ranges, Dock.Top);
        Children.Add(ranges);
        Children.Add(SettingsGrid);

        DataContextChanged += (_, _) => _presenter.Bind(DataContext as FontsViewModel);
    }

    /// <summary>The generation settings grid.</summary>
    public DataUiGrid SettingsGrid { get; }

    private Control BuildRangesSection()
    {
        StackPanel body = new StackPanel { Margin = new Thickness(8, 6, 8, 8), Spacing = 6 };

        CheckBox characterFile = new CheckBox { Content = "Use Font Character File (.gumfcs)" };
        characterFile.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(FontsViewModel.UseFontCharacterFile)) { Mode = BindingMode.TwoWay });
        TextBlock characterFileHint = new TextBlock
        {
            Text = "Sets the default ranges to every character in the .gumfcs file next to your project, so you can list the text your game shows instead of typing ranges.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(26, 0, 0, 0),
        }.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        body.Children.Add(new StackPanel { Children = { characterFile, characterFileHint } });

        body.Children.Add(BuildTableHeader());

        ItemsControl rows = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<FontRangeEntryViewModel>((_, _) => BuildRow()),
        };
        rows.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(FontsViewModel.Entries)));
        body.Children.Add(rows);

        Button add = new Button { Content = "Add Font Range", HorizontalAlignment = HorizontalAlignment.Left };
        add.Bind(Button.CommandProperty, new Binding(nameof(FontsViewModel.AddEntryCommand)));
        body.Children.Add(add);

        return BuildCategory("Character Ranges", body);
    }

    // The same strip the grid draws for each of its categories: a chevron and the name, over rows that
    // a click on the strip shows or hides.
    private static Control BuildCategory(string title, Control body)
    {
        TextBlock name = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.Medium,
            Margin = new Thickness(0, 4),
            VerticalAlignment = VerticalAlignment.Center,
        }.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.ThemeContrast");

        RotateTransform rotation = new RotateTransform(90);
        Path chevron = new Path
        {
            Data = Geometry.Parse("M 0,0 L 4,4 L 0,8"),
            StrokeThickness = 1.5,
            Width = 9,
            Height = 9,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(6, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = rotation,
        };
        chevron.Bind(Shape.StrokeProperty, name.GetObservable(TextBlock.ForegroundProperty));

        Grid headerRow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        headerRow.Children.Add(chevron);
        Grid.SetColumn(name, 1);
        headerRow.Children.Add(name);

        Border header = new Border { Child = headerRow, Padding = new Thickness(0, 3) }
            .WithThemeResource(Border.BackgroundProperty, "Frb.Brushes.Contrast01");
        header.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                body.IsVisible = !body.IsVisible;
                rotation.Angle = body.IsVisible ? 90 : 0;
            }
        };

        return new StackPanel { Children = { header, body } };
    }

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

    private static ColumnDefinitions Columns() => new ColumnDefinitions("3*,4*,Auto");

    private static Control BuildRow()
    {
        Grid row = new Grid { ColumnDefinitions = Columns(), RowDefinitions = new RowDefinitions("Auto,Auto"), Margin = new Thickness(0, 2) };

        // The default row names no font; every other row picks one from the fonts the project uses, or types one.
        TextBlock defaultLabel = new TextBlock { Text = "All other fonts", FontStyle = FontStyle.Italic, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        defaultLabel.Bind(Visual.IsVisibleProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)));
        row.Children.Add(defaultLabel);

        ComboBox family = new ComboBox { IsEditable = true, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 4, 0) };
        family.Bind(ComboBox.ItemsSourceProperty, new Binding($"{nameof(DataContext)}.{nameof(FontsViewModel.AvailableFonts)}")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(FontsView) },
        });
        family.Bind(ComboBox.TextProperty, new Binding(nameof(FontRangeEntryViewModel.FontFamily))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
        });
        family.Bind(Visual.IsVisibleProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)) { Converter = global::Avalonia.Data.Converters.BoolConverters.Not });
        CommitOnEnter(family);
        row.Children.Add(family);

        TextBox ranges = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch };
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
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(FontsView) },
        });
        remove.Bind(Button.CommandParameterProperty, new Binding());
        // The default row keeps an invisible button so its column lines up with the other rows.
        remove.Bind(Visual.OpacityProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)) { Converter = new FuncValueConverter<bool, double>(isDefault => isDefault ? 0 : 1) });
        remove.Bind(InputElement.IsHitTestVisibleProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)) { Converter = global::Avalonia.Data.Converters.BoolConverters.Not });
        remove.Bind(InputElement.FocusableProperty, new Binding(nameof(FontRangeEntryViewModel.IsDefault)) { Converter = global::Avalonia.Data.Converters.BoolConverters.Not });
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
