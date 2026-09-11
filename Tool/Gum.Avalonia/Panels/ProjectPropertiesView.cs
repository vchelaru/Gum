using Gum.DataTypes;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Plugins.PropertiesWindowPlugin;
using Gum.Services.Fonts;

namespace Gum.Avalonia.Panels;

/// <summary>
/// The Project Properties tab: every project setting of <see cref="ProjectPropertiesViewModel"/>,
/// grouped the way the WPF tool's property grid groups them (general, guides, single pixel texture,
/// font generation), with a Close button. Twin of the WPF <c>ProjectPropertiesControl</c>, whose
/// view is a property grid. The localization file list is shown read-only until phase 70 brings
/// the file editors.
/// </summary>
public sealed class ProjectPropertiesView : DockPanel
{
    private StackPanel _section = null!;
    private Grid _rows = null!;

    /// <summary>Builds the view.</summary>
    public ProjectPropertiesView()
    {
        Button close = new Button { Content = "Close", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(4) };
        close.Click += (_, _) => (DataContext as ProjectPropertiesViewModel)?.RequestClose();
        SetDock(close, Dock.Bottom);
        Children.Add(close);

        StackPanel sections = new StackPanel { Margin = new Thickness(6), Spacing = 10 };
        Children.Add(new ScrollViewer { Content = sections });

        StartSection(sections, "General");
        AddCheckBox("Auto Save", nameof(ProjectPropertiesViewModel.AutoSave));
        AddComboBox("Texture Filter", nameof(ProjectPropertiesViewModel.TextureFilter), new object[] { TextureFilter.Point, TextureFilter.Linear });
        AddCheckBox("Restrict To Unit Values", nameof(ProjectPropertiesViewModel.RestrictToUnitValues));
        AddTextBox("Canvas Width", nameof(ProjectPropertiesViewModel.CanvasWidth));
        AddTextBox("Canvas Height", nameof(ProjectPropertiesViewModel.CanvasHeight));
        AddCheckBox("Restrict File Names For Android", nameof(ProjectPropertiesViewModel.RestrictFileNamesForAndroid));
        AddCheckBox("Render Text Character By Character", nameof(ProjectPropertiesViewModel.RenderTextCharacterByCharacter));
        AddCheckBox("Show Localization", nameof(ProjectPropertiesViewModel.ShowLocalization));
        AddLanguage();
        AddLocalizationFiles();

        StartSection(sections, "Guides");
        AddCheckBox("Show Outlines", nameof(ProjectPropertiesViewModel.ShowOutlines));
        AddCheckBox("Show Canvas Outline", nameof(ProjectPropertiesViewModel.ShowCanvasOutline));
        AddCheckBox("Show Checker Background", nameof(ProjectPropertiesViewModel.ShowCheckerBackground));

        StartSection(sections, "Single Pixel Texture");
        AddTextBox("Single Pixel Texture File", nameof(ProjectPropertiesViewModel.SinglePixelTextureFile));
        AddTextBox("Single Pixel Texture Left", nameof(ProjectPropertiesViewModel.SinglePixelTextureLeft));
        AddTextBox("Single Pixel Texture Top", nameof(ProjectPropertiesViewModel.SinglePixelTextureTop));
        AddTextBox("Single Pixel Texture Right", nameof(ProjectPropertiesViewModel.SinglePixelTextureRight));
        AddTextBox("Single Pixel Texture Bottom", nameof(ProjectPropertiesViewModel.SinglePixelTextureBottom));

        StartSection(sections, "Font Generation");
        TextBox fontRanges = AddTextBox("Font Ranges", nameof(ProjectPropertiesViewModel.FontRanges));
        fontRanges.Bind(TextBox.IsReadOnlyProperty, new Binding(nameof(ProjectPropertiesViewModel.IsFontRangesReadOnly)));
        AddCheckBox("Use Font Character File (.gumfcs)", nameof(ProjectPropertiesViewModel.UseFontCharacterFile));
        AddTextBox("Font Spacing Horizontal", nameof(ProjectPropertiesViewModel.FontSpacingHorizontal));
        AddTextBox("Font Spacing Vertical", nameof(ProjectPropertiesViewModel.FontSpacingVertical));
        AddCheckBox("Auto-Size Font Outputs", nameof(ProjectPropertiesViewModel.AutoSizeFontOutputs), "Fewer PNGs, but font generation can be much slower");
        AddComboBox("Font Generator", nameof(ProjectPropertiesViewModel.FontGenerator), new object[] { FontGeneratorType.BmFont, FontGeneratorType.KernSmith });
    }

    private void StartSection(StackPanel sections, string title)
    {
        _section = new StackPanel { Spacing = 2 };
        _section.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 2) });
        _rows = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        _section.Children.Add(_rows);
        sections.Children.Add(_section);
    }

    private int AddRow(string label, Control editor, string? detail = null)
    {
        int row = _rows.RowDefinitions.Count;
        _rows.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        TextBlock name = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 2, 12, 2) };
        if (detail != null)
        {
            ToolTip.SetTip(name, detail);
            ToolTip.SetTip(editor, detail);
        }
        Grid.SetRow(name, row);
        _rows.Children.Add(name);

        editor.Margin = new Thickness(0, 2);
        Grid.SetRow(editor, row);
        Grid.SetColumn(editor, 1);
        _rows.Children.Add(editor);
        return row;
    }

    private void AddCheckBox(string label, string property, string? detail = null)
    {
        CheckBox checkBox = new CheckBox();
        checkBox.Bind(ToggleButton.IsCheckedProperty, new Binding(property) { Mode = BindingMode.TwoWay });
        AddRow(label, checkBox, detail);
    }

    private TextBox AddTextBox(string label, string property)
    {
        TextBox textBox = new TextBox { MinWidth = 160 };
        textBox.Bind(TextBox.TextProperty, new Binding(property) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
        AddRow(label, textBox);
        return textBox;
    }

    private void AddComboBox(string label, string property, object[] options)
    {
        ComboBox comboBox = new ComboBox { ItemsSource = options, MinWidth = 160 };
        comboBox.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(property) { Mode = BindingMode.TwoWay });
        AddRow(label, comboBox);
    }

    private void AddLanguage()
    {
        ComboBox languages = new ComboBox { MinWidth = 160 };
        languages.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ProjectPropertiesViewModel.AvailableLanguages)));
        languages.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(ProjectPropertiesViewModel.LanguageName)) { Mode = BindingMode.TwoWay });
        int row = AddRow("Language", languages);

        // The WPF grid drops the row when no localization is loaded; hide it here.
        IValueConverter hasLanguages = new FuncValueConverter<System.Collections.Generic.IReadOnlyList<string>?, bool>(list => list is { Count: > 0 });
        foreach (Control control in _rows.Children)
        {
            if (Grid.GetRow(control) == row)
            {
                control.Bind(IsVisibleProperty, new Binding(nameof(ProjectPropertiesViewModel.AvailableLanguages)) { Converter = hasLanguages });
            }
        }
    }

    private void AddLocalizationFiles()
    {
        ItemsControl files = new ItemsControl();
        files.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ProjectPropertiesViewModel.LocalizationFiles)));
        StackPanel box = new StackPanel { Spacing = 2 };
        box.Children.Add(files);
        box.Children.Add(new TextBlock
        {
            Text = "Add or remove localization files in the WPF tool for now.",
            Opacity = 0.6,
            FontStyle = FontStyle.Italic,
            TextWrapping = TextWrapping.Wrap,
        });
        AddRow("Localization Files", box);
    }
}
