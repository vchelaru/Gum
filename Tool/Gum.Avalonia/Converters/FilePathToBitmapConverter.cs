using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Gum.Avalonia.Converters;

/// <summary>
/// Loads the image file at the bound absolute path, or yields null when the path is empty, missing,
/// or not a readable image. Twin of the WPF head's <c>FilePathToImageSourceConverter</c>.
/// </summary>
public sealed class FilePathToBitmapConverter : IValueConverter
{
    /// <summary>Shared instance.</summary>
    public static readonly FilePathToBitmapConverter Instance = new FilePathToBitmapConverter();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return new Bitmap(path);
        }
        catch (Exception)
        {
            // A corrupt or unsupported preview image is not worth failing the dialog over.
            return null;
        }
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
