using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gum.Avalonia.Themes;

/// <summary>
/// Looks up an FRB brush for the current theme variant, for code that picks a brush by state
/// (selected, hovered, dragging) rather than binding one property for good. Callers re-read on
/// <see cref="Plugins.TreeView.AvaloniaTreeIcons.ThemeChanged"/> or a theme-variant change.
/// </summary>
public static class ThemeBrushes
{
    /// <summary>The brush for <paramref name="key"/> as seen from <paramref name="scope"/>, or <paramref name="fallback"/>.</summary>
    public static IBrush Get(StyledElement? scope, string key, IBrush fallback)
    {
        if (scope != null && scope.TryFindResource(key, scope.ActualThemeVariant, out object? scoped) && scoped is IBrush scopedBrush)
        {
            return scopedBrush;
        }

        if (Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out object? value) && value is IBrush brush)
        {
            return brush;
        }

        return fallback;
    }
}
