using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Gum.Avalonia.Themes;
using Gum.SettingsWindow;

namespace Gum.Avalonia.Settings;

/// <summary>The Application > General page: auto save, the Standards palette, and the settings folder.</summary>
public sealed class GeneralSettingsView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public GeneralSettingsView()
    {
        Spacing = 12;
        Children.Add(CreateOption(
            "Auto save",
            "Save the project whenever something changes.",
            nameof(GeneralSettingsViewModel.AutoSave)));
        Children.Add(CreateOption(
            "Standards palette",
            "Show the standard types as a palette at the bottom of the Project panel instead of a Standard folder.",
            nameof(GeneralSettingsViewModel.UseStandardsPalette)));

        Button openFolder = new Button { Content = "Open settings folder", HorizontalAlignment = HorizontalAlignment.Left };
        openFolder.Bind(Button.CommandProperty, new Binding(nameof(GeneralSettingsViewModel.OpenSettingsFolderCommand)));
        ToolTip.SetTip(openFolder, "Open the folder containing Gum's global settings files");
        Children.Add(openFolder);
    }

    private static Control CreateOption(string label, string description, string property)
    {
        CheckBox box = new CheckBox { Content = label };
        box.Bind(ToggleButton.IsCheckedProperty, new Binding(property) { Mode = BindingMode.TwoWay });
        TextBlock hint = new TextBlock { Text = description, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(26, 0, 0, 0) }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Subtle");
        return new StackPanel { Children = { box, hint } };
    }
}
