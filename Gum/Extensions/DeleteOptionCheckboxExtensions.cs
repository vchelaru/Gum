using StateAnimationPlugin.Managers;
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
            Content = viewModel.Label,
            IsChecked = viewModel.IsChecked,
            Width = 220
        };
    }
}
