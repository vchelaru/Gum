using System;
using System.Collections.Generic;
using static Gum.Managers.TreeNodeImageIndices;

namespace Gum.Managers;

/// <summary>
/// One tree icon: its file-name key, its artwork, and the theme color that tints it.
/// </summary>
/// <param name="Key">
/// The icon's file-name key (e.g. "Container_Instance.png"). Callers that are not tree rows - the
/// Standards chip palette and context menus - address icons by this key rather than by index.
/// </param>
/// <param name="RelativePath">The artwork's path under the tool's content root, forward-slash separated.</param>
/// <param name="ColorResourceKey">The theme color resource the artwork is tinted with.</param>
public sealed record TreeIconDefinition(string Key, string RelativePath, string ColorResourceKey);

/// <summary>
/// The artwork and tint behind every tree icon index, shared by both heads' icon registries. Each
/// head prefixes <see cref="TreeIconDefinition.RelativePath"/> with its own resource scheme.
/// </summary>
/// <remarks>
/// The source PNGs are authored white-on-transparent, with alpha carrying the shading, so an icon is
/// drawn by filling a shape with the theme color and masking it with the artwork. Indices are the
/// shared <see cref="TreeNodeImageIndices"/> constants that <see cref="TreeNodeImageLogic"/>
/// produces; adding an icon is a constant plus an entry here, in any position.
/// </remarks>
public static class TreeIconCatalog
{
    /// <summary>Folder of the tool's general icons, relative to the content root.</summary>
    public const string IconFolder = "Content/Icons/";

    /// <summary>Folder of the element tree's row icons, relative to the content root.</summary>
    public const string TreeIconFolder = IconFolder + "UpdatedTreeViewIcons/";

    /// <summary>Theme color keys the icons are tinted with.</summary>
    public const string Manilla = "Frb.Colors.Icon.Manilla";
    public const string Green = "Frb.Colors.Icon.Green";
    public const string Blue = "Frb.Colors.Icon.Blue";
    public const string Red = "Frb.Colors.Icon.Red";
    public const string Purple = "Frb.Colors.Icon.Purple";
    public const string Fallback = "Frb.Colors.Primary";

    private static readonly Dictionary<int, TreeIconDefinition> IconsByIndex = new()
    {
        [TransparentImageIndex] = new("transparent.png", IconFolder + "transparent.png", Fallback),
        [FolderImageIndex] = new("Folder.png", TreeIconFolder + "folder.png", Manilla),
        [ComponentImageIndex] = new("Component.png", TreeIconFolder + "Component.png", Green),
        [InstanceImageIndex] = new("Instance.png", TreeIconFolder + "Instance.png", Blue),
        [ScreenImageIndex] = new("Screen.png", TreeIconFolder + "screen.png", Red),
        [StandardElementImageIndex] = new("StandardElement.png", TreeIconFolder + "StandardElement.png", Purple),
        [ExclamationIndex] = new("redExclamation.png", IconFolder + "redExclamation.png", Red),
        [StateImageIndex] = new("state.png", IconFolder + "state.png", Blue),
        [BehaviorImageIndex] = new("behavior.png", TreeIconFolder + "behavior.png", Manilla),
        [DerivedInstanceImageIndex] = new("InheritedInstance.png", IconFolder + "InheritedInstance.png", Fallback),
        [LockedInstanceImageIndex] = new("instance_locked.png", TreeIconFolder + "instance_locked.png", Blue),

        [ContainerImageIndex] = new("Container.png", TreeIconFolder + "Container.png", Purple),
        [SpriteImageIndex] = new("Sprite.png", TreeIconFolder + "Sprite.png", Purple),
        [NineSliceImageIndex] = new("NineSlice.png", TreeIconFolder + "NineSlice.png", Purple),
        [TextImageIndex] = new("Text.png", TreeIconFolder + "Text.png", Purple),
        [RectangleImageIndex] = new("Rectangle.png", TreeIconFolder + "Rectangle.png", Purple),
        [ColoredRectangleImageIndex] = new("ColoredRectangle.png", TreeIconFolder + "ColoredRectangle.png", Purple),
        [CircleImageIndex] = new("Circle.png", TreeIconFolder + "Circle.png", Purple),
        [ColoredCircleImageIndex] = new("ColoredCircle.png", TreeIconFolder + "ColoredCircle.png", Purple),
        [RoundedRectangleImageIndex] = new("RoundedRectangle.png", TreeIconFolder + "RoundedRectangle.png", Purple),
        [PolygonImageIndex] = new("Polygon.png", TreeIconFolder + "Polygon.png", Purple),
        [ArcImageIndex] = new("Arc.png", TreeIconFolder + "Arc.png", Purple),
        [LineImageIndex] = new("Line.png", TreeIconFolder + "Line.png", Purple),
        [CanvasImageIndex] = new("Canvas.png", TreeIconFolder + "Canvas.png", Purple),
        [LottieAnimationImageIndex] = new("LottieAnimation.png", TreeIconFolder + "LottieAnimation.png", Purple),
        [SvgImageIndex] = new("Svg.png", TreeIconFolder + "Svg.png", Purple),

        // Same artwork as the standard-element rows above, tinted blue instead of purple.
        [ContainerInstanceImageIndex] = new("Container_Instance.png", TreeIconFolder + "Container.png", Blue),
        [SpriteInstanceImageIndex] = new("Sprite_Instance.png", TreeIconFolder + "Sprite.png", Blue),
        [NineSliceInstanceImageIndex] = new("NineSlice_Instance.png", TreeIconFolder + "NineSlice.png", Blue),
        [TextInstanceImageIndex] = new("Text_Instance.png", TreeIconFolder + "Text.png", Blue),
        [RectangleInstanceImageIndex] = new("Rectangle_Instance.png", TreeIconFolder + "Rectangle.png", Blue),
        [ColoredRectangleInstanceImageIndex] = new("ColoredRectangle_Instance.png", TreeIconFolder + "ColoredRectangle.png", Blue),
        [CircleInstanceImageIndex] = new("Circle_Instance.png", TreeIconFolder + "Circle.png", Blue),
        [ColoredCircleInstanceImageIndex] = new("ColoredCircle_Instance.png", TreeIconFolder + "ColoredCircle.png", Blue),
        [RoundedRectangleInstanceImageIndex] = new("RoundedRectangle_Instance.png", TreeIconFolder + "RoundedRectangle.png", Blue),
        [PolygonInstanceImageIndex] = new("Polygon_Instance.png", TreeIconFolder + "Polygon.png", Blue),
        [ArcInstanceImageIndex] = new("Arc_Instance.png", TreeIconFolder + "Arc.png", Blue),
        [LineInstanceImageIndex] = new("Line_Instance.png", TreeIconFolder + "Line.png", Blue),
        [CanvasInstanceImageIndex] = new("Canvas_Instance.png", TreeIconFolder + "Canvas.png", Blue),
        [LottieAnimationInstanceImageIndex] = new("LottieAnimation_Instance.png", TreeIconFolder + "LottieAnimation.png", Blue),
        [SvgInstanceImageIndex] = new("Svg_Instance.png", TreeIconFolder + "Svg.png", Blue),
    };

    private static readonly Dictionary<string, int> IndicesByKey = BuildKeyIndex();

    /// <summary>Every icon, by index.</summary>
    public static IReadOnlyDictionary<int, TreeIconDefinition> Icons => IconsByIndex;

    /// <summary>Finds the index of the icon with file-name key <paramref name="key"/> (case-insensitive).</summary>
    public static bool TryGetIndex(string key, out int index) => IndicesByKey.TryGetValue(key, out index);

    /// <summary>The theme color key an icon index is tinted with, or <see cref="Fallback"/> for an unknown index.</summary>
    public static string GetColorResourceKey(int imageIndex) =>
        IconsByIndex.TryGetValue(imageIndex, out TreeIconDefinition? definition) ? definition.ColorResourceKey : Fallback;

    private static Dictionary<string, int> BuildKeyIndex()
    {
        Dictionary<string, int> byKey = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<int, TreeIconDefinition> entry in IconsByIndex)
        {
            byKey[entry.Value.Key] = entry.Key;
        }

        return byKey;
    }
}
