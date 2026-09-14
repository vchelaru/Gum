using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Gum.Avalonia.Converters;

/// <summary>
/// True when the bound number is not zero; binds a collection's <c>Count</c> to <c>IsVisible</c>.
/// Twin of the WPF head's <c>CountToVisibilityConverter</c>.
/// </summary>
public sealed class NonZeroConverter : IValueConverter
{
    /// <summary>Shared instance.</summary>
    public static readonly NonZeroConverter Instance = new NonZeroConverter();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count != 0;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
