using Gum.Services.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Gum.Extensions;

/// <summary>
/// Converts the framework-neutral delete options (ADR-0005) into real WPF controls for the
/// DeleteOptionsWindow's plugin-extension area. Each control binds two-way to its option, so the
/// option carries the user's choice back to the plugin. Mirrors
/// <see cref="ContextMenuItemViewModelExtensions"/>'s neutral-data-to-WPF-control pattern.
/// </summary>
public static class DeleteOptionCheckboxExtensions
{
    public static CheckBox ToCheckBox(this DeleteOptionCheckboxViewModel viewModel)
    {
        CheckBox checkBox = new CheckBox
        {
            // A TextBlock rather than the raw string so a long label wraps instead of being clipped
            // mid-word; the themed CheckBox template's content column now stretches to the dialog's
            // width, giving the wrap something to wrap against.
            Content = new TextBlock
            {
                Text = viewModel.Label,
                TextWrapping = TextWrapping.Wrap
            },
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        BindIsChecked(checkBox, viewModel);
        return checkBox;
    }

    /// <summary>A captioned group of radio buttons, one per option, for a pick-one delete option.</summary>
    public static GroupBox ToGroupBox(this DeleteOptionChoiceViewModel viewModel)
    {
        StackPanel stackPanel = new StackPanel();
        string groupName = "DeleteChoice" + System.Guid.NewGuid().ToString("N");
        foreach (DeleteOptionCheckboxViewModel option in viewModel.Options)
        {
            RadioButton radioButton = new RadioButton
            {
                Content = option.Label,
                GroupName = groupName,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            BindIsChecked(radioButton, option);
            stackPanel.Children.Add(radioButton);
        }

        return new GroupBox
        {
            Header = viewModel.Header,
            Content = stackPanel,
        };
    }

    private static void BindIsChecked(ToggleButton toggle, DeleteOptionCheckboxViewModel option) =>
        toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(DeleteOptionCheckboxViewModel.IsChecked))
        {
            Source = option,
            Mode = BindingMode.TwoWay,
        });
}
