using System;
using System.Collections.Concurrent;
using System.Reflection;
using Gum.Converters;

namespace Gum.Wireframe;

/// <summary>
/// Read-side companion to <see cref="GraphicalUiElement.SetProperty(string, object?)"/>: reads the
/// current value of a Gum variable by name. A curated switch (the "base set") handles names that do
/// not map 1:1 to a CLR property — notably the axis-aware unit enums "XUnits"/"YUnits", which the
/// runtime stores as <see cref="GeneralUnitType"/> but the tool stores as PositionUnitType. Every
/// other name falls through to reflection against the element's concrete runtime type, so visual
/// properties ("Red", "Text", "Font", ...) on TextRuntime/SpriteRuntime are read without per-type
/// code. The set of names actually requested is governed by StandardElementsManager (what Gum
/// understands), so reflection is only ever asked for genuine Gum variables. Kept as an extension
/// rather than a method on <see cref="GraphicalUiElement"/> to avoid adding to that already-large class.
/// </summary>
public static class GraphicalUiElementPropertyReadExtensions
{
    /// <summary>
    /// Attempts to read the current value of a Gum variable (e.g. "X", "Width", "WidthUnits", "Red",
    /// "Text") by name, trying the curated base-set switch first and reflection as a fallback.
    /// </summary>
    /// <param name="element">The element to read from.</param>
    /// <param name="propertyName">The Gum variable name.</param>
    /// <param name="value">The current value when found; otherwise null.</param>
    /// <returns>True if the property was recognized and read; otherwise false.</returns>
    public static bool TryGetProperty(this GraphicalUiElement element, string propertyName, out object? value)
    {
        value = null;
        switch (propertyName)
        {
            case "AutoGridHorizontalCells":
                value = element.AutoGridHorizontalCells;
                return true;
            case "AutoGridVerticalCells":
                value = element.AutoGridVerticalCells;
                return true;
            case "ChildrenLayout":
            case "Children Layout":
                value = element.ChildrenLayout;
                return true;
            case "ClipsChildren":
            case "Clips Children":
                value = element.ClipsChildren;
                return true;
#if !FRB && NET6_0_OR_GREATER
            case "ExposeChildrenEvents":
                if (element is InteractiveGue exposeChildrenEventsGue)
                {
                    value = exposeChildrenEventsGue.ExposeChildrenEvents;
                    return true;
                }
                return false;
#endif
            case "FlipHorizontal":
                value = element.FlipHorizontal;
                return true;
#if !FRB && NET6_0_OR_GREATER
            case "HasEvents":
                if (element is InteractiveGue hasEventsGue)
                {
                    value = hasEventsGue.HasEvents;
                    return true;
                }
                return false;
#endif
            case "Height":
                value = element.Height;
                return true;
            case "HeightUnits":
            case "Height Units":
                value = element.HeightUnits;
                return true;
            case nameof(GraphicalUiElement.IgnoredByParentSize):
                value = element.IgnoredByParentSize;
                return true;
            case nameof(GraphicalUiElement.MaxHeight):
                value = element.MaxHeight;
                return true;
            case nameof(GraphicalUiElement.MaxWidth):
                value = element.MaxWidth;
                return true;
            case nameof(GraphicalUiElement.MinHeight):
                value = element.MinHeight;
                return true;
            case nameof(GraphicalUiElement.MinWidth):
                value = element.MinWidth;
                return true;
            case "Parent":
                // "Parent" is intentionally not returned as a scalar. Parent/child structure is captured
                // structurally by the snapshot serializer (via the qualified "Parent" variable); reflecting
                // GraphicalUiElement.Parent would return the parent object, not a Gum parent name.
                return false;
            case "Rotation":
                value = element.Rotation;
                return true;
            case "SourceFile":
            case "Source File":
                // The Gum "SourceFile" variable is a file path. The runtime exposes the texture object
                // (Sprite/NineSlice ".Texture"), not the path -- but the content loader stamps the source
                // path onto Texture2D.Name, so recover it there. Reflection keeps this reader cross-platform
                // (no Texture2D reference here). Returns false when there is no texture, so callers skip it
                // rather than emit a Texture2D the snapshot serializer cannot write.
                value = TryReadTextureName(element);
                return value != null;
            case "StackSpacing":
                value = element.StackSpacing;
                return true;
            case "TextureLeft":
            case "Texture Left":
                value = element.TextureLeft;
                return true;
            case "TextureTop":
            case "Texture Top":
                value = element.TextureTop;
                return true;
            case "TextureWidth":
            case "Texture Width":
                value = element.TextureWidth;
                return true;
            case "TextureHeight":
            case "Texture Height":
                value = element.TextureHeight;
                return true;
            case "TextureWidthScale":
            case "Texture Width Scale":
                value = element.TextureWidthScale;
                return true;
            case "TextureHeightScale":
            case "Texture Height Scale":
                value = element.TextureHeightScale;
                return true;
            case "TextureAddress":
            case "Texture Address":
                value = element.TextureAddress;
                return true;
            case "Visible":
                value = element.Visible;
                return true;
            case "Width":
                value = element.Width;
                return true;
            case "WidthUnits":
            case "Width Units":
                value = element.WidthUnits;
                return true;
            case "X":
                value = element.X;
                return true;
            case "XOrigin":
            case "X Origin":
                value = element.XOrigin;
                return true;
            case "XUnits":
            case "X Units":
                value = UnitConverter.ConvertToPositionUnit(element.XUnits, isXAxis: true);
                return true;
            case "Y":
                value = element.Y;
                return true;
            case "YOrigin":
            case "Y Origin":
                value = element.YOrigin;
                return true;
            case "YUnits":
            case "Y Units":
                value = UnitConverter.ConvertToPositionUnit(element.YUnits, isXAxis: false);
                return true;
            case "Wrap":
                value = element.Wrap;
                return true;
            case "WrapsChildren":
            case "Wraps Children":
                value = element.WrapsChildren;
                return true;
        }

        // Not in the base set: fall through to reflection on the concrete runtime type. The caller
        // only requests names from the StandardElementsManager catalog, so this only ever resolves
        // genuine Gum variables (e.g. "Red", "Text", "Font" on TextRuntime/SpriteRuntime).
        PropertyInfo? property = _propertyCache.GetOrAdd(
            (element.GetType(), propertyName),
            static key => ResolveReflectedProperty(key.Item1, key.Item2));

        if (property != null)
        {
            try
            {
                value = property.GetValue(element);
                return true;
            }
            catch (TargetInvocationException)
            {
                // A property getter threw; treat as unreadable rather than failing the whole read.
            }
        }

        return false;
    }

    // Memoizes the public, readable, non-indexer property per (runtime type, Gum variable name).
    // A null entry caches "no reflectable property" so repeated misses stay cheap.
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = new();

    // Recovers a Sprite/NineSlice source path from its texture's Name (stamped by the content loader on
    // load). Done by reflection so this shared reader needs no reference to the backend texture type.
    private static string? TryReadTextureName(GraphicalUiElement element)
    {
        PropertyInfo? textureProperty = ResolveReflectedProperty(element.GetType(), "Texture");
        object? texture = textureProperty?.GetValue(element);
        if (texture == null)
        {
            return null;
        }

        PropertyInfo? nameProperty = ResolveReflectedProperty(texture.GetType(), "Name");
        string? name = nameProperty?.GetValue(texture) as string;
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private static PropertyInfo? ResolveReflectedProperty(Type type, string propertyName)
    {
        try
        {
            PropertyInfo? property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                return property;
            }
        }
        catch (AmbiguousMatchException)
        {
            // Overloaded properties (e.g. SpriteRuntime.SourceFile has Texture2D? and string? variants)
            // cannot be resolved by name alone; such names must be added to the base-set switch.
        }
        return null;
    }
}
