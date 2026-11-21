using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Themes;
using Gum.Diagnostics;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>
/// Shows <see cref="CreateDiagnosticsViewModel"/>: an optional description, what is always included,
/// two opt-in check boxes, the file name and, while saving or after a failure, progress or the error.
/// </summary>
public sealed class CreateDiagnosticsView : StackPanel
{
    /// <summary>Automation and test name of the description box.</summary>
    public const string DescriptionName = "PART_Description";

    /// <summary>Automation and test name of the Gum project check box.</summary>
    public const string GumProjectCheckBoxName = "PART_IncludeGumProject";

    /// <summary>Automation and test name of the C# project check box.</summary>
    public const string CSharpProjectCheckBoxName = "PART_IncludeCSharpProject";

    private const double ContentWidth = 420;

    /// <summary>Builds the view.</summary>
    public CreateDiagnosticsView()
    {
        Spacing = 4;
        Width = ContentWidth;

        Children.Add(new TextBlock { Text = "What went wrong? (optional)" });

        TextBox description = new TextBox
        {
            Name = DescriptionName,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 84,
            Watermark = "Describe the problem and what you were doing when it happened.",
        };
        description.Bind(TextBox.TextProperty, new Binding(nameof(CreateDiagnosticsViewModel.Description)) { Mode = BindingMode.TwoWay });
        Children.Add(description);

        StackPanel included = new StackPanel { Spacing = 3, Margin = new Thickness(0, 10, 0, 0) };
        included.Children.Add(IncludedLine("Gum version, system and graphics info", withCheckmark: true));
        included.Children.Add(IncludedLine("Recent logs, errors, plugins and settings", withCheckmark: true));
        TextBlock privacy = IncludedLine("Your user name and folder paths are removed.", withCheckmark: false);
        privacy.Margin = new Thickness(18, 0, 0, 0);
        included.Children.Add(privacy);
        Children.Add(included);

        StackPanel options = new StackPanel { Spacing = 6, Margin = new Thickness(0, 10, 0, 0) };
        options.Children.Add(OptionalCheckBox(
            GumProjectCheckBoxName, "Gum project",
            "The .gumx file and its screens, components and standards.",
            nameof(CreateDiagnosticsViewModel.IncludeGumProject),
            nameof(CreateDiagnosticsViewModel.CanIncludeGumProject),
            nameof(CreateDiagnosticsViewModel.GumProjectNote)));
        options.Children.Add(OptionalCheckBox(
            CSharpProjectCheckBoxName, "C# project",
            "The whole game project, without build output.",
            nameof(CreateDiagnosticsViewModel.IncludeCSharpProject),
            nameof(CreateDiagnosticsViewModel.CanIncludeCSharpProject),
            nameof(CreateDiagnosticsViewModel.CSharpProjectNote)));
        Children.Add(options);

        TextBlock fileName = new TextBlock { Margin = new Thickness(0, 10, 0, 0) };
        fileName.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        fileName.Bind(TextBlock.TextProperty, new Binding(nameof(CreateDiagnosticsViewModel.FileName)));
        Children.Add(fileName);

        ProgressBar progress = new ProgressBar { IsIndeterminate = true, Height = 4, Margin = new Thickness(0, 6, 0, 0) };
        progress.Bind(IsVisibleProperty, new Binding(nameof(CreateDiagnosticsViewModel.IsSaving)));
        Children.Add(progress);

        TextBlock error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
        error.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Error");
        error.Bind(TextBlock.TextProperty, new Binding(nameof(CreateDiagnosticsViewModel.ErrorMessage)));
        error.Bind(IsVisibleProperty, new Binding(nameof(CreateDiagnosticsViewModel.HasError)));
        Children.Add(error);
    }

    private static TextBlock IncludedLine(string text, bool withCheckmark)
    {
        TextBlock line = new TextBlock { Text = withCheckmark ? "✓  " + text : text, TextWrapping = TextWrapping.Wrap };
        line.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        return line;
    }

    private static readonly global::Avalonia.Data.Converters.IValueConverter DimWhenDisabled =
        new global::Avalonia.Data.Converters.FuncValueConverter<bool, double>(isEnabled => isEnabled ? 1.0 : 0.5);

    // The box sits on the title line and the help and note line up under the title, so the box reads as the title's.
    private static Control OptionalCheckBox(string name, string label, string help, string isChecked, string isEnabled, string note)
    {
        Grid row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
        };

        CheckBox box = new CheckBox { Name = name, VerticalAlignment = VerticalAlignment.Center, MinWidth = 0, Margin = new Thickness(0, 0, 6, 0) };
        global::Avalonia.Automation.AutomationProperties.SetName(box, label);
        box.Bind(ToggleButton.IsCheckedProperty, new Binding(isChecked) { Mode = BindingMode.TwoWay });
        box.Bind(IsEnabledProperty, new Binding(isEnabled));
        Grid.SetRowSpan(box, 1);
        row.Children.Add(box);

        TextBlock title = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        title.Bind(OpacityProperty, new Binding(isEnabled) { Converter = DimWhenDisabled });
        title.PointerPressed += (_, _) =>
        {
            if (box.IsEnabled)
            {
                box.IsChecked = box.IsChecked != true;
            }
        };
        Grid.SetColumn(title, 1);
        row.Children.Add(title);

        TextBlock helpText = new TextBlock { Text = help, TextWrapping = TextWrapping.Wrap };
        helpText.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        helpText.Bind(OpacityProperty, new Binding(isEnabled) { Converter = DimWhenDisabled });
        Grid.SetColumn(helpText, 1);
        Grid.SetRow(helpText, 1);
        row.Children.Add(helpText);

        TextBlock noteText = new TextBlock { TextWrapping = TextWrapping.Wrap };
        noteText.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Icon.Manilla");
        noteText.Bind(TextBlock.TextProperty, new Binding(note));
        noteText.Bind(IsVisibleProperty, new Binding(note) { Converter = global::Avalonia.Data.Converters.StringConverters.IsNotNullOrEmpty });
        Grid.SetColumn(noteText, 1);
        Grid.SetRow(noteText, 2);
        row.Children.Add(noteText);

        return row;
    }
}
