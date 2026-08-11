using Gum.Services.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace Gum.Extensions;

/// <summary>
/// Converts a framework-neutral <see cref="DeleteOptionCheckboxViewModel"/> (ADR-0005) into a real
/// WPF <see cref="CheckBox"/> for the DeleteOptionsWindow's plugin-extension area. Mirrors
/// <see cref="ContextMenuItemViewModelExtensions"/>'s neutral-data-to-WPF-control pattern.
/// </summary>
public static class DeleteOptionCheckboxExtensions
{
    public static CheckBox ToCheckBox(this DeleteOptionCheckboxViewModel viewModel)
    {
        return new CheckBox
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
            IsChecked = viewModel.IsChecked
        };
    }
}
