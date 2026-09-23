using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using ConvertToJsonPlugin;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Plugins.PluginDialogs;

/// <summary>
/// The Convert to JSON confirmation: what conversion does, the opt-in check box to move the
/// original XML files to the OS trash, and a warning shown while it is checked.
/// </summary>
public sealed class ConvertToJsonView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public ConvertToJsonView()
    {
        Spacing = 8;
        MaxWidth = 520;

        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(ConvertToJsonDialogViewModel.Message)));
        Children.Add(message);

        TextBlock recycleLabel = new TextBlock { TextWrapping = TextWrapping.Wrap };
        recycleLabel.Bind(TextBlock.TextProperty, new Binding(nameof(ConvertToJsonDialogViewModel.RecycleCheckBoxText)));
        CheckBox recycle = new CheckBox { Content = recycleLabel, Margin = new Thickness(0, 8, 0, 0) };
        recycle.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ConvertToJsonDialogViewModel.ShouldRecycleXmlFiles)) { Mode = BindingMode.TwoWay });
        Children.Add(recycle);

        TextBlock warning = new TextBlock { TextWrapping = TextWrapping.Wrap }
            .WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Error");
        warning.Bind(TextBlock.TextProperty, new Binding(nameof(ConvertToJsonDialogViewModel.RecycleWarning)));
        warning.Bind(IsVisibleProperty, new Binding(nameof(ConvertToJsonDialogViewModel.ShouldRecycleXmlFiles)));
        Children.Add(warning);
    }
}
