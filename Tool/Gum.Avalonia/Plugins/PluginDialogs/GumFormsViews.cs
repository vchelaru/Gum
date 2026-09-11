using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GumFormsPlugin.ViewModels;

namespace Gum.Avalonia.Plugins.PluginDialogs;

/// <summary>
/// The Forms theme picker: the theme drop-down, its preview image, and the project changes that
/// applying it implies. Twin of the WPF <c>ThemeSelectionControl</c>; any dialog over a
/// <see cref="ThemeSelectionViewModel"/> can host it.
/// </summary>
public sealed class ThemeSelectionView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public ThemeSelectionView()
    {
        StackPanel themeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
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

        StackPanel requirements = new StackPanel();
        requirements.Children.Add(new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            Text = "Clicking OK will also apply these project changes:",
            TextWrapping = TextWrapping.Wrap,
        });
        TextBlock description = new TextBlock { Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap };
        description.Bind(TextBlock.TextProperty, new Binding(nameof(ThemeSelectionViewModel.RequirementsDescription)));
        requirements.Children.Add(description);
        Border requirementsBorder = new Border
        {
            Margin = new Thickness(0, 4, 0, 0),
            Padding = new Thickness(8),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new CornerRadius(4),
            Child = requirements,
        };
        requirementsBorder.Bind(IsVisibleProperty, new Binding(nameof(ThemeSelectionViewModel.HasRequirements)));
        Children.Add(requirementsBorder);
    }
}

/// <summary>The Add Forms dialog: the theme picker and whether to include the demo screen. Twin of the WPF <c>AddFormsWindow</c>.</summary>
public sealed class AddFormsView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public AddFormsView()
    {
        MaxWidth = 420;

        ThemeSelectionView themeSelection = new ThemeSelectionView { Margin = new Thickness(0, 0, 0, 8) };
        themeSelection.Bind(DataContextProperty, new Binding(nameof(AddFormsViewModel.ThemeSelection)));
        Children.Add(themeSelection);

        CheckBox includeDemo = new CheckBox { Content = "Include DemoScreenGum" };
        includeDemo.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(AddFormsViewModel.IsIncludeDemoScreenGum)) { Mode = BindingMode.TwoWay });
        Children.Add(includeDemo);
    }
}

/// <summary>An image file path as a bitmap; null when the path is empty, missing, or not an image.</summary>
public sealed class FilePathToBitmapConverter : IValueConverter
{
    /// <summary>Shared instance.</summary>
    public static readonly FilePathToBitmapConverter Instance = new FilePathToBitmapConverter();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return new Bitmap(path);
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or NotSupportedException or InvalidOperationException)
        {
            // A preview is optional; an unreadable file just shows none.
            return null;
        }
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
