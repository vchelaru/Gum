using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Gum.Plugins.InternalPlugins.Undos;

namespace Gum.Avalonia.Converters;

/// <summary>
/// Small converters that turn a view model's neutral state into a look. Each is the Avalonia twin of
/// a WPF <c>DataTrigger</c> that sets a themed brush or font style from a VM bool; the decision
/// itself stays on the VM. Brushes use Fluent-friendly fixed colors until phase 90 brings themes.
/// </summary>
public static class ViewConverters
{
    /// <summary>Green when an error count is zero, red otherwise (the Errors tab header dot).</summary>
    public static readonly IValueConverter ErrorCountBrush =
        new FuncValueConverter<int, IBrush>(count => count == 0 ? Brushes.MediumSeaGreen : Brushes.OrangeRed);

    /// <summary>Dims history entries that would be redone rather than undone.</summary>
    public static readonly IValueConverter RedoOpacity =
        new FuncValueConverter<UndoOrRedo, double>(kind => kind == UndoOrRedo.Redo ? 0.5 : 1.0);

    /// <summary>Error-colored text for a missing (orphaned) behavior; unset otherwise so the theme applies.</summary>
    public static readonly IValueConverter OrphanForeground =
        new FuncValueConverter<bool, object>(isOrphaned => isOrphaned ? Brushes.OrangeRed : AvaloniaProperty.UnsetValue);

    /// <summary>Italic text for a missing (orphaned) behavior.</summary>
    public static readonly IValueConverter OrphanFontStyle =
        new FuncValueConverter<bool, FontStyle>(isOrphaned => isOrphaned ? FontStyle.Italic : FontStyle.Normal);

    /// <summary>A solid brush for a neutral <see cref="System.Drawing.Color"/> (ADR-0004).</summary>
    public static readonly IValueConverter DrawingColorBrush =
        new FuncValueConverter<System.Drawing.Color, IBrush>(color => new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B)));
}
