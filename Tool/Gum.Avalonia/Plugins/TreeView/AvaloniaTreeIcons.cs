using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Gum.Managers;

namespace Gum.Avalonia.Plugins.TreeView;

/// <summary>
/// Draws tree icons in the Avalonia head: the artwork for an icon index from the shared
/// <see cref="TreeIconCatalog"/>, filled with the icon's theme color. The counterpart of the WPF
/// <c>TreeIconRegistry</c>; the artwork is the same PNGs, linked into this assembly as resources.
/// </summary>
public static class AvaloniaTreeIcons
{
    private const string ResourceRoot = "avares://Gum.Avalonia/";

    // The tints come from the application's resources once the FRB theme dictionaries are ported;
    // until then, and for any key a theme lacks, the dark palette's values.
    private static readonly Dictionary<string, Color> FallbackColors = new()
    {
        [TreeIconCatalog.Manilla] = Color.Parse("#ffdd95"),
        [TreeIconCatalog.Green] = Color.Parse("#7ef37a"),
        [TreeIconCatalog.Blue] = Color.Parse("#51b3ff"),
        [TreeIconCatalog.Red] = Color.Parse("#d74554"),
        [TreeIconCatalog.Purple] = Color.Parse("#a47ad5"),
        [TreeIconCatalog.Fallback] = Color.Parse("#3e9ece"),
    };

    private static readonly Dictionary<string, Bitmap?> BitmapCache = new();
    private static readonly Dictionary<string, IBrush> BrushCache = new();

    /// <summary>Raised when the theme changes, so drawn icons re-resolve their tint.</summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>Drops the cached tints and tells drawn icons to re-resolve them.</summary>
    public static void NotifyThemeChanged()
    {
        BrushCache.Clear();
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>The resource URI of an icon's artwork.</summary>
    public static Uri GetResourceUri(TreeIconDefinition definition) => new Uri(ResourceRoot + definition.RelativePath);

    /// <summary>The artwork for an icon index, or null when the index has none. Decoded once and shared.</summary>
    public static Bitmap? GetBitmap(int imageIndex) =>
        TreeIconCatalog.Icons.TryGetValue(imageIndex, out TreeIconDefinition? definition) ? LoadBitmap(definition) : null;

    /// <summary>The brush an icon index is tinted with.</summary>
    public static IBrush GetBrush(int imageIndex)
    {
        string key = TreeIconCatalog.GetColorResourceKey(imageIndex);
        if (BrushCache.TryGetValue(key, out IBrush? cached))
        {
            return cached;
        }

        IBrush brush = new SolidColorBrush(ResolveColor(key));
        BrushCache[key] = brush;
        return brush;
    }

    /// <summary>A tinted icon for a file-name key (e.g. "Sprite_Instance.png"), or null if there is none.</summary>
    public static Control? CreateIcon(string key, double size) =>
        TreeIconCatalog.TryGetIndex(key, out int index) ? CreateIcon(index, size) : null;

    /// <summary>A tinted icon for an icon index, or null if there is none.</summary>
    public static Control? CreateIcon(int imageIndex, double size)
    {
        if (GetBitmap(imageIndex) is not { } bitmap)
        {
            return null;
        }

        return new Rectangle
        {
            Width = size,
            Height = size,
            Fill = GetBrush(imageIndex),
            OpacityMask = new ImageBrush(bitmap),
        };
    }

    private static Bitmap? LoadBitmap(TreeIconDefinition definition)
    {
        if (BitmapCache.TryGetValue(definition.RelativePath, out Bitmap? cached))
        {
            return cached;
        }

        Bitmap? bitmap = null;
        Uri uri = GetResourceUri(definition);
        if (AssetLoader.Exists(uri))
        {
            using System.IO.Stream stream = AssetLoader.Open(uri);
            bitmap = new Bitmap(stream);
        }

        BitmapCache[definition.RelativePath] = bitmap;
        return bitmap;
    }

    private static Color ResolveColor(string key)
    {
        if (Application.Current is { } app &&
            app.TryFindResource(key, app.ActualThemeVariant, out object? value) &&
            value is Color color)
        {
            return color;
        }

        return FallbackColors.TryGetValue(key, out Color fallback) ? fallback : Colors.White;
    }
}
