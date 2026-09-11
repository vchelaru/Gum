using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Gum.Avalonia.Converters;

/// <summary>
/// True when the bound value is not null (and, for a string, not empty); used to hide optional
/// rows. A type adapter only: the decision it encodes is "is there anything to show".
/// </summary>
public sealed class NotNullConverter : IValueConverter
{
    /// <summary>Shared instance.</summary>
    public static readonly NotNullConverter Instance = new NotNullConverter();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text ? !string.IsNullOrEmpty(text) : value != null;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
