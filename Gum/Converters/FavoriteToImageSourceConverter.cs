using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gum.Converters;

/// <summary>
/// Converts the favorite-star bool on a recent-file item to the filled or outline star icon. Kept
/// in the WPF tool project (not the view model) so RecentItemViewModel can stay framework-neutral
/// (ADR-0004) and expose only the logical IsFavorite bool.
/// </summary>
public class FavoriteToImageSourceConverter : IValueConverter
{
    private static readonly ImageSource _filledStar = CreateImageSource("/Content/Icons/RecentFiles/StarFilled.png");
    private static readonly ImageSource _outlineStar = CreateImageSource("/Content/Icons/RecentFiles/StarOutline.png");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? _filledStar : _outlineStar;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static ImageSource CreateImageSource(string relativeUri)
    {
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.UriSource = new Uri(relativeUri, UriKind.Relative);
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        return bitmapImage;
    }
}
