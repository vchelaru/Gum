using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;

namespace Gum.Avalonia.Converters;

/// <summary>
/// Error-colored text bound to a view-model bool. A style does it, not a converter that returns
/// "unset" for the normal case: that binding leaves the property at Black instead of inheriting the
/// theme's text color, so the label vanished on the dark theme.
/// </summary>
internal static class ErrorText
{
    private const string FlaggedClass = ":flagged";

    /// <summary>Turns <paramref name="block"/> OrangeRed (and italic when asked) while <paramref name="flagPath"/> is true.</summary>
    public static void Bind(TextBlock block, string flagPath, bool italic)
    {
        Style style = new Style(x => x.OfType<TextBlock>().Class(FlaggedClass));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.OrangeRed));
        if (italic)
        {
            style.Setters.Add(new Setter(TextBlock.FontStyleProperty, FontStyle.Italic));
        }
        block.Styles.Add(style);
        block.Bind(FlagProperty, new Binding(flagPath));
    }

    private static readonly AttachedProperty<bool> FlagProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, TextBlock, bool>("Flag");

    static ErrorText()
    {
        FlagProperty.Changed.AddClassHandler<TextBlock>((block, e) =>
            ((IPseudoClasses)block.Classes).Set(FlaggedClass, e.GetNewValue<bool>()));
    }
}
