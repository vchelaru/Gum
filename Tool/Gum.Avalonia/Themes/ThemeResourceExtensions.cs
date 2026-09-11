using Avalonia;
using Avalonia.Controls;

namespace Gum.Avalonia.Themes;

/// <summary>
/// Binds control properties to theme resources (the <c>Frb.*</c> keys from
/// <see cref="FrbThemeResources"/>) so they repaint on a light/dark or accent switch, the code-built
/// equivalent of WPF's <c>{DynamicResource}</c>.
/// </summary>
public static class ThemeResourceExtensions
{
    /// <summary>Binds <paramref name="property"/> on <paramref name="control"/> to the resource <paramref name="key"/>.</summary>
    public static T WithThemeResource<T>(this T control, AvaloniaProperty property, string key) where T : Control
    {
        control.Bind(property, control.GetResourceObservable(key));
        return control;
    }
}
