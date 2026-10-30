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
    private const double PreviewGap = 20;

    /// <summary>Builds the view; set its DataContext to a <see cref="ThemeSelectionViewModel"/>.</summary>
    /// <param name="showRequirements">False leaves out the list of project changes the import will make: it describes an existing project, so New Project hides it (the import still applies them).</param>
    /// <param name="previewBeside">Puts the preview image to the right of a narrow theme column, wrapping below it when there is no room (New Project).</param>
    /// <param name="labelContent">Replaces the "Theme" text label, such as with a label that carries a tip.</param>
    /// <param name="labelAbove">Puts the "Theme" label above a full-width drop-down instead of beside it (New Project).</param>
    public ThemeSelectionView(bool labelAbove = false, Control? labelContent = null, bool previewBeside = false, bool showRequirements = true)
    {
        Orientation = Orientation.Vertical;

        StackPanel themeRow = new StackPanel
        {
            Orientation = labelAbove ? Orientation.Vertical : Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8),
            Spacing = labelAbove ? 3 : 8,
        };
        themeRow.Children.Add(labelContent ?? new TextBlock { Text = "Theme", MinWidth = labelAbove ? 0 : 56, VerticalAlignment = VerticalAlignment.Center });
        ComboBox themes = new ComboBox
        {
            MinWidth = previewBeside ? 0 : 160,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = labelAbove ? HorizontalAlignment.Stretch : HorizontalAlignment.Left,
        };
        themes.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ThemeSelectionViewModel.AvailableThemes)));
        themes.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(ThemeSelectionViewModel.SelectedTheme)) { Mode = BindingMode.TwoWay });
        themeRow.Children.Add(themes);
        Grid? besideRow = null;
        if (previewBeside)
        {
            // The drop-down fills what the preview leaves; the preview is a fixed-size block at the right edge.
            // The label sits alone in row 0; the drop-down and the preview share row 1, so their tops are flush.
            besideRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                Margin = new Thickness(0, 0, 0, 8),
            };
            // 20 px at the default size, scaling with the tool zoom like the text around it; the preview column hugs the image.
            besideRow.Bind(Grid.ColumnSpacingProperty, besideRow.GetResourceObservable("ControlContentThemeFontSize", size => (object)(PreviewGap * (size is double baseSize ? baseSize : 12d) / 12d)));
            Control label = themeRow.Children[0];
            themeRow.Children.Clear();
            label.Margin = new Thickness(0, 0, 0, 3);
            besideRow.Children.Add(label);
            Grid.SetRow(themes, 1);
            themes.VerticalAlignment = VerticalAlignment.Top;
            besideRow.Children.Add(themes);
            Children.Add(besideRow);
        }
        else
        {
            Children.Add(themeRow);
        }

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
        if (besideRow != null)
        {
            preview.MaxHeight = 112;
            preview.MaxWidth = 200;
            preview.Margin = new Thickness(0);
            preview.HorizontalAlignment = HorizontalAlignment.Right;
            preview.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(preview, 1);
            Grid.SetRow(preview, 1);
            besideRow.Children.Add(preview);
        }
        else
        {
            Children.Add(preview);
        }

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
        if (showRequirements)
        {
            Children.Add(requirementsPanel);
        }
    }
}
