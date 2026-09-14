using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Gum.Avalonia.Converters;

/// <summary>
/// Adapts a view model's neutral <see cref="System.Drawing.Color"/> (ADR-0004) to Avalonia's
/// <see cref="Color"/> and back. A null color shows as transparent. Twin of the WPF head's
/// <c>DrawingColorToMediaColorConverter</c>.
/// </summary>
public sealed class DrawingColorToAvaloniaColorConverter : IValueConverter
{
    /// <summary>Shared instance.</summary>
    public static readonly DrawingColorToAvaloniaColorConverter Instance = new DrawingColorToAvaloniaColorConverter();

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is System.Drawing.Color color ? Color.FromArgb(color.A, color.R, color.G, color.B) : Colors.Transparent;

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Color color ? System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B) : null;
}
