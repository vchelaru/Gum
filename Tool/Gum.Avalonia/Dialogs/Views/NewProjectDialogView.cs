using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Gum.Avalonia.Controls;
using Gum.Dialogs;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>
/// File &gt; New Project options: whether to import Forms controls, which theme, and whether to add
/// the theme's demo screen. Twin of the WPF <c>NewProjectDialogView</c>, bound to
/// <see cref="NewProjectDialogViewModel"/>.
/// </summary>
public sealed class NewProjectDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public NewProjectDialogView()
    {
        MaxWidth = 420;
        DialogWindow.SetDialogTitle(this, "New Project");

        CheckBox includeForms = new CheckBox { Content = "Include Forms controls" };
        includeForms.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.IsIncludeFormsControls)) { Mode = BindingMode.TwoWay });
        Children.Add(includeForms);

        Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 4, 0, 8),
            Opacity = 0.7,
            Text = "Adds buttons, text boxes, list boxes and other standard controls to the project.",
            TextWrapping = TextWrapping.Wrap,
        });

        StackPanel formsOptions = new StackPanel();
        formsOptions.Bind(IsEnabledProperty, new Binding(nameof(NewProjectDialogViewModel.IsIncludeFormsControls)));

        ThemeSelectionView themeSelection = new ThemeSelectionView();
        themeSelection.Bind(DataContextProperty, new Binding(nameof(NewProjectDialogViewModel.ThemeSelection)));
        formsOptions.Children.Add(themeSelection);

        CheckBox includeDemo = new CheckBox { Content = "Include DemoScreenGum", Margin = new Thickness(0, 8, 0, 0) };
        includeDemo.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(NewProjectDialogViewModel.IsIncludeDemoScreenGum)) { Mode = BindingMode.TwoWay });
        formsOptions.Children.Add(includeDemo);

        Children.Add(formsOptions);
    }
}
