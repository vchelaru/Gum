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
            // mid-word. MaxWidth is what makes the wrap engage: the themed CheckBox template lays
            // the content out in an Auto column, which measures at the label's desired width and so
            // never supplies a constraint of its own. Sized to the delete dialog's fixed width less
            // its padding and the check box glyph.
            Content = new TextBlock
            {
                Text = viewModel.Label,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 290
            },
            HorizontalAlignment = HorizontalAlignment.Left,
            IsChecked = viewModel.IsChecked
        };
    }
}
