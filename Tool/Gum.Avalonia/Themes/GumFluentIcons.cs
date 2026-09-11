using Avalonia.Data;
using Avalonia.Layout;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace Gum.Avalonia.Themes;

/// <summary>
/// Creates Fluent System Icons (FluentIcons.Avalonia, the icon set the WPF head uses) at a set size.
/// The icon control measures to whatever height it is offered, so an unsized one stretches to fill a
/// docked or stacked slot (a tool button row took the Project panel's whole height); these always
/// size the icon.
/// </summary>
public static class GumFluentIcons
{
    /// <summary>An icon <paramref name="size"/> units square.</summary>
    public static FluentIcon Create(Icon icon, double size) =>
        new FluentIcon { Icon = icon, FontSize = size, Width = size, Height = size };

    /// <summary>An icon whose size follows <paramref name="size"/> (a font-size binding, say).</summary>
    public static FluentIcon Create(Icon icon, IBinding size)
    {
        FluentIcon result = new FluentIcon { Icon = icon };
        result.Bind(FluentIcon.FontSizeProperty, size);
        result.Bind(Layoutable.WidthProperty, size);
        result.Bind(Layoutable.HeightProperty, size);
        return result;
    }
}
