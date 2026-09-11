using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Converters;
using GumFormsPlugin.ViewModels;

namespace Gum.Avalonia.Controls;

/// <summary>
/// Theme picker bound to a <see cref="ThemeSelectionViewModel"/>: the theme combo box, the selected
/// theme's preview image, and the list of project changes importing it will make. Twin of the WPF
/// <c>ThemeSelectionControl</c>, shared by New Project and Add Forms. Avalonia's size-to-content
/// windows grow with the requirements panel on their own, so the WPF control's window re-fit is not
/// needed here.
/// </summary>
public sealed class ThemeSelectionView : StackPanel
{
    /// <summary>Builds the view; set its DataContext to a <see cref="ThemeSelectionViewModel"/>.</summary>
    public ThemeSelectionView()
    {
        Orientation = Orientation.Vertical;

        StackPanel themeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8), Spacing = 8 };
        themeRow.Children.Add(new TextBlock { Text = "Theme", MinWidth = 56, VerticalAlignment = VerticalAlignment.Center });
        ComboBox themes = new ComboBox { MinWidth = 160, VerticalAlignment = VerticalAlignment.Center };
        themes.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ThemeSelectionViewModel.AvailableThemes)));
        themes.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(ThemeSelectionViewModel.SelectedTheme)) { Mode = BindingMode.TwoWay });
        themeRow.Children.Add(themes);
        Children.Add(themeRow);

        Image preview = new Image
        {
            MaxWidth = 280,
            MaxHeight = 140,
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            Stretch = Stretch.Uniform,
        };
        preview.Bind(Image.SourceProperty, new Binding(nameof(ThemeSelectionViewModel.PreviewImagePath)) { Converter = FilePathToBitmapConverter.Instance });
        preview.Bind(IsVisibleProperty, new Binding(nameof(ThemeSelectionViewModel.HasPreviewImage)));
        Children.Add(preview);

        StackPanel requirementsText = new StackPanel();
        requirementsText.Children.Add(new TextBlock
        {
            Text = "Clicking OK will also apply these project changes:",
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        TextBlock requirements = new TextBlock { Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap };
        requirements.Bind(TextBlock.TextProperty, new Binding(nameof(ThemeSelectionViewModel.RequirementsDescription)));
        requirementsText.Children.Add(requirements);

        Border requirementsPanel = new Border
        {
            Margin = new Thickness(0, 4, 0, 0),
            Padding = new Thickness(8),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = requirementsText,
        };
        requirementsPanel.Bind(IsVisibleProperty, new Binding(nameof(ThemeSelectionViewModel.HasRequirements)));
        Children.Add(requirementsPanel);
    }
}
