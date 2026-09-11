using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// Builds the WPF elements that draw tree icons: the artwork for an icon index, tinted with that
/// icon's theme color. Which artwork and color belong to each index is the shared
/// <see cref="TreeIconCatalog"/>.
/// </summary>
/// <remarks>
/// The source PNGs are authored white-on-transparent, with alpha carrying the shading, so an icon
/// is colored by multiplying it with a theme color. In WPF that is a shape filled with the theme
/// brush and masked by the artwork - no pixel work and no per-theme bitmap rebuild, which is what
/// the GDI+ <c>ColorMatrix</c> pass over a whole <c>ImageList</c> used to do on every theme or
/// font-size change.
/// </remarks>
public static class TreeIconRegistry
{
    private const string PackRoot = "pack://application:,,,/Gum;component/";

    private static readonly Dictionary<string, ImageSource> SourceCache = new();

    /// <summary>Tint brushes by theme resource key. Cleared whenever the theme changes.</summary>
    private static readonly Dictionary<string, Brush> BrushCache = new();

    /// <summary>
    /// Raised when the active theme changes, so already-drawn icons re-resolve their tint.
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// Announces that theme resources have changed. Artwork is theme-independent and stays cached;
    /// only the tint brushes are dropped so they re-resolve against the new theme.
    /// </summary>
    public static void NotifyThemeChanged()
    {
        BrushCache.Clear();
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// The artwork for an icon index, or null if the index has no entry. Decoded once and frozen, so
    /// every row showing the same icon shares one image.
    /// </summary>
    public static ImageSource? GetSource(int imageIndex) =>
        TreeIconCatalog.Icons.TryGetValue(imageIndex, out TreeIconDefinition? definition)
            ? LoadSource(PackRoot + definition.RelativePath)
            : null;

    /// <summary>
    /// The brush an icon index is tinted with, resolved from the active theme. Falls back to the
    /// primary color when the index has no entry or the theme is missing the resource.
    /// </summary>
    public static Brush GetBrush(int imageIndex)
    {
        string key = TreeIconCatalog.GetColorResourceKey(imageIndex);

        if (BrushCache.TryGetValue(key, out Brush? cached))
        {
            return cached;
        }

        Brush brush = ResolveBrush(key) ?? ResolveBrush(TreeIconCatalog.Fallback) ?? Brushes.White;
        BrushCache[key] = brush;
        return brush;
    }

    /// <summary>
    /// A themed, correctly-tinted element for the icon with the given file-name key (e.g.
    /// "Container_Instance.png"), or null if there is no such icon. For callers outside the tree -
    /// context-menu items and the Standards chip palette - that place an icon into their own layout.
    /// </summary>
    public static FrameworkElement? CreateIcon(string key, double size) =>
        TreeIconCatalog.TryGetIndex(key, out int index) ? CreateIcon(index, size) : null;

    /// <summary>
    /// A themed, correctly-tinted element for an icon index, or null if there is no such icon.
    /// </summary>
    public static FrameworkElement? CreateIcon(int imageIndex, double size)
    {
        if (GetSource(imageIndex) is not { } source)
        {
            return null;
        }

        return new Rectangle
        {
            Width = size,
            Height = size,
            Fill = GetBrush(imageIndex),
            OpacityMask = new ImageBrush(source),
        };
    }

    private static ImageSource LoadSource(string uri)
    {
        if (SourceCache.TryGetValue(uri, out ImageSource? cached))
        {
            return cached;
        }

        BitmapImage image = new();
        image.BeginInit();
        image.UriSource = new Uri(uri, UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();

        SourceCache[uri] = image;
        return image;
    }

    private static Brush? ResolveBrush(string resourceKey)
    {
        if (Application.Current?.TryFindResource(resourceKey) is not Color color)
        {
            return null;
        }

        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}
