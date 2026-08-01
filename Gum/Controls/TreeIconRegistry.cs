using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Gum.Managers;
using static Gum.Managers.TreeNodeImageIndices;

namespace Gum.Controls;

/// <summary>
/// Maps a tree node's icon index to the artwork and theme color that draw it, and builds the
/// element that renders the pair.
/// </summary>
/// <remarks>
/// <para>
/// The source PNGs are authored white-on-transparent, with alpha carrying the shading, so an icon
/// is colored by multiplying it with a theme color. In WPF that is a shape filled with the theme
/// brush and masked by the artwork - no pixel work and no per-theme bitmap rebuild, which is what
/// the GDI+ <c>ColorMatrix</c> pass over a whole <c>ImageList</c> used to do on every theme or
/// font-size change.
/// </para>
/// <para>
/// Indices are the shared <see cref="TreeNodeImageIndices"/> constants that the headless
/// <see cref="TreeNodeImageLogic"/> already produces. Unlike the old <c>ImageList</c>, the numbering
/// is no longer coupled to the order images happen to load in - adding an icon is a constant plus an
/// entry here, in any position.
/// </para>
/// </remarks>
public static class TreeIconRegistry
{
    private const string IconRoot = "pack://application:,,,/Gum;component/Content/Icons/";
    private const string TreeIconRoot = IconRoot + "UpdatedTreeViewIcons/";

    // Theme resource keys, matching what the WinForms icon tinting read.
    private const string Manilla = "Frb.Colors.Icon.Manilla";
    private const string Green = "Frb.Colors.Icon.Green";
    private const string Blue = "Frb.Colors.Icon.Blue";
    private const string Red = "Frb.Colors.Icon.Red";
    private const string Purple = "Frb.Colors.Icon.Purple";
    private const string Fallback = "Frb.Colors.Primary";

    /// <param name="Key">
    /// The icon's file-name key. Retained because callers that are not tree rows - the Standards
    /// chip palette and the context menu - address icons by name rather than by index.
    /// </param>
    private sealed record IconDefinition(string Key, string Uri, string ColorResourceKey);

    private static readonly Dictionary<int, IconDefinition> Icons = new()
    {
        [TransparentImageIndex] = new("transparent.png", IconRoot + "transparent.png", Fallback),
        [FolderImageIndex] = new("Folder.png", TreeIconRoot + "folder.png", Manilla),
        [ComponentImageIndex] = new("Component.png", TreeIconRoot + "Component.png", Green),
        [InstanceImageIndex] = new("Instance.png", TreeIconRoot + "Instance.png", Blue),
        [ScreenImageIndex] = new("Screen.png", TreeIconRoot + "screen.png", Red),
        [StandardElementImageIndex] = new("StandardElement.png", TreeIconRoot + "StandardElement.png", Purple),
        [ExclamationIndex] = new("redExclamation.png", IconRoot + "redExclamation.png", Red),
        [StateImageIndex] = new("state.png", IconRoot + "state.png", Blue),
        [BehaviorImageIndex] = new("behavior.png", TreeIconRoot + "behavior.png", Manilla),
        [DerivedInstanceImageIndex] = new("InheritedInstance.png", IconRoot + "InheritedInstance.png", Fallback),
        [LockedInstanceImageIndex] = new("instance_locked.png", TreeIconRoot + "instance_locked.png", Blue),

        [ContainerImageIndex] = new("Container.png", TreeIconRoot + "Container.png", Purple),
        [SpriteImageIndex] = new("Sprite.png", TreeIconRoot + "Sprite.png", Purple),
        [NineSliceImageIndex] = new("NineSlice.png", TreeIconRoot + "NineSlice.png", Purple),
        [TextImageIndex] = new("Text.png", TreeIconRoot + "Text.png", Purple),
        [RectangleImageIndex] = new("Rectangle.png", TreeIconRoot + "Rectangle.png", Purple),
        [ColoredRectangleImageIndex] = new("ColoredRectangle.png", TreeIconRoot + "ColoredRectangle.png", Purple),
        [CircleImageIndex] = new("Circle.png", TreeIconRoot + "Circle.png", Purple),
        [ColoredCircleImageIndex] = new("ColoredCircle.png", TreeIconRoot + "ColoredCircle.png", Purple),
        [RoundedRectangleImageIndex] = new("RoundedRectangle.png", TreeIconRoot + "RoundedRectangle.png", Purple),
        [PolygonImageIndex] = new("Polygon.png", TreeIconRoot + "Polygon.png", Purple),
        [ArcImageIndex] = new("Arc.png", TreeIconRoot + "Arc.png", Purple),
        [LineImageIndex] = new("Line.png", TreeIconRoot + "Line.png", Purple),
        [CanvasImageIndex] = new("Canvas.png", TreeIconRoot + "Canvas.png", Purple),
        [LottieAnimationImageIndex] = new("LottieAnimation.png", TreeIconRoot + "LottieAnimation.png", Purple),
        [SvgImageIndex] = new("Svg.png", TreeIconRoot + "Svg.png", Purple),

        // Same artwork as the standard-element rows above, tinted blue instead of purple. The old
        // ImageList had to cache these as separate images to hold the second tint; here only the
        // brush differs.
        [ContainerInstanceImageIndex] = new("Container_Instance.png", TreeIconRoot + "Container.png", Blue),
        [SpriteInstanceImageIndex] = new("Sprite_Instance.png", TreeIconRoot + "Sprite.png", Blue),
        [NineSliceInstanceImageIndex] = new("NineSlice_Instance.png", TreeIconRoot + "NineSlice.png", Blue),
        [TextInstanceImageIndex] = new("Text_Instance.png", TreeIconRoot + "Text.png", Blue),
        [RectangleInstanceImageIndex] = new("Rectangle_Instance.png", TreeIconRoot + "Rectangle.png", Blue),
        [ColoredRectangleInstanceImageIndex] = new("ColoredRectangle_Instance.png", TreeIconRoot + "ColoredRectangle.png", Blue),
        [CircleInstanceImageIndex] = new("Circle_Instance.png", TreeIconRoot + "Circle.png", Blue),
        [ColoredCircleInstanceImageIndex] = new("ColoredCircle_Instance.png", TreeIconRoot + "ColoredCircle.png", Blue),
        [RoundedRectangleInstanceImageIndex] = new("RoundedRectangle_Instance.png", TreeIconRoot + "RoundedRectangle.png", Blue),
        [PolygonInstanceImageIndex] = new("Polygon_Instance.png", TreeIconRoot + "Polygon.png", Blue),
        [ArcInstanceImageIndex] = new("Arc_Instance.png", TreeIconRoot + "Arc.png", Blue),
        [LineInstanceImageIndex] = new("Line_Instance.png", TreeIconRoot + "Line.png", Blue),
        [CanvasInstanceImageIndex] = new("Canvas_Instance.png", TreeIconRoot + "Canvas.png", Blue),
        [LottieAnimationInstanceImageIndex] = new("LottieAnimation_Instance.png", TreeIconRoot + "LottieAnimation.png", Blue),
        [SvgInstanceImageIndex] = new("Svg_Instance.png", TreeIconRoot + "Svg.png", Blue),
    };

    private static readonly Dictionary<string, int> IndicesByKey = BuildKeyIndex();

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
        Icons.TryGetValue(imageIndex, out IconDefinition? definition) ? LoadSource(definition.Uri) : null;

    /// <summary>
    /// The brush an icon index is tinted with, resolved from the active theme. Falls back to the
    /// primary color when the index has no entry or the theme is missing the resource.
    /// </summary>
    public static Brush GetBrush(int imageIndex)
    {
        string key = Icons.TryGetValue(imageIndex, out IconDefinition? definition)
            ? definition.ColorResourceKey
            : Fallback;

        if (BrushCache.TryGetValue(key, out Brush? cached))
        {
            return cached;
        }

        Brush brush = ResolveBrush(key) ?? ResolveBrush(Fallback) ?? Brushes.White;
        BrushCache[key] = brush;
        return brush;
    }

    /// <summary>
    /// A themed, correctly-tinted element for the icon with the given file-name key (e.g.
    /// "Container_Instance.png"), or null if there is no such icon. For callers outside the tree -
    /// context-menu items and the Standards chip palette - that place an icon into their own layout.
    /// </summary>
    public static FrameworkElement? CreateIcon(string key, double size) =>
        IndicesByKey.TryGetValue(key, out int index) ? CreateIcon(index, size) : null;

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

    private static Dictionary<string, int> BuildKeyIndex()
    {
        Dictionary<string, int> byKey = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<int, IconDefinition> entry in Icons)
        {
            byKey[entry.Value.Key] = entry.Key;
        }

        return byKey;
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
