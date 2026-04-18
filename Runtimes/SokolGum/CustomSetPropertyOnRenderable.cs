using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Renderables;
using ToolsUtilities;

namespace SokolGum;

/// <summary>
/// Property-routing helper invoked by Gum when a <c>.gumx</c> variable is
/// applied to a renderable. Two categories of handling live here:
///
/// 1. Name translation — <c>.gumx</c> uses Gum-level property names like
///    <c>"Text"</c>, <c>"SourceFile"</c>, <c>"Font"</c> that don't exist on
///    the underlying renderable (our <see cref="Text"/> has <c>RawText</c>,
///    <see cref="Sprite"/> has <c>Texture</c>, etc.).
///
/// 2. Content loading — <c>"SourceFile"</c> / <c>"CustomFontFile"</c> carry
///    file paths that must be routed through <see cref="IContentLoader"/>
///    to produce <see cref="Texture2D"/> / <see cref="Font"/> instances.
///
/// Anything this class doesn't special-case falls through to
/// <see cref="GraphicalUiElement.SetPropertyThroughReflection"/>, which
/// handles direct property mappings (Red, Green, Blue, Alpha, X, Y, Width,
/// Height, FontSize, Color, Visible, Rotation, etc.) via reflection.
///
/// The fontstash-based Text renderable doesn't need per-size bitmap font
/// caches the way <c>RaylibGum.Renderables.Text</c> does — <c>FontSize</c>
/// is applied at rasterization time, so changing it is a plain property set.
/// </summary>
public static class CustomSetPropertyOnRenderable
{
    /// <summary>
    /// Translates Gum <c>"Text"</c> assignments before they reach the
    /// renderable. Wire an actual implementation (e.g. <c>gettext</c>) by
    /// assigning this property at startup; null (the default) means identity.
    /// </summary>
    public static Func<string, string>? LocalizationTranslate { get; set; }

    /// <summary>
    /// Fallback hook for additional backends that layer on top of SokolGum
    /// (mirrors RaylibGum's pattern). Return true to indicate the property
    /// was handled and prevent reflection fallback.
    /// </summary>
    public static Func<IRenderableIpso, GraphicalUiElement, string, object, bool>? AdditionalPropertyOnRenderable;

    public static void SetPropertyOnRenderable(
        IRenderableIpso renderable,
        GraphicalUiElement element,
        string propertyName,
        object value)
    {
        bool handled = renderable switch
        {
            Text t      => TrySetOnText(t, element, propertyName, value),
            Sprite s    => TrySetOnSprite(s, element, propertyName, value),
            NineSlice n => TrySetOnNineSlice(n, element, propertyName, value),
            _           => false,
        };

        if (!handled && AdditionalPropertyOnRenderable is { } extra)
            handled = extra(renderable, element, propertyName, value);

        if (!handled)
            SetPropertyWithEnumConversion(renderable, element, propertyName, value);
    }

    /// <summary>
    /// Reflection fallback that closes two gaps in Gum's core
    /// <see cref="GraphicalUiElement.SetPropertyThroughReflection"/>: (1) int
    /// values can't be directly assigned to enum-typed properties, and (2)
    /// primitive values can't be directly assigned to <see cref="Nullable{T}"/>
    /// properties. <c>.gumx</c> stores enum variables as their underlying int
    /// and nullable floats as plain floats, so these two conversions come up
    /// constantly. Handling them here keeps alignment/overflow/units enums
    /// and nullable-float properties (like NineSlice's custom border width)
    /// working without special-casing each one.
    ///
    /// If the conversion throws (incompatible types, value out of range,
    /// undefined enum backing), we fall through to the core reflection
    /// helper rather than propagate — that matches the behaviour of the
    /// core path for most other mismatches, so a single bad variable
    /// doesn't tear down the entire screen load.
    /// </summary>
    private static void SetPropertyWithEnumConversion(
        IRenderableIpso renderable,
        GraphicalUiElement element,
        string propertyName,
        object value)
    {
        var prop = renderable.GetType().GetProperty(propertyName);
        if (prop is not null && prop.CanWrite && value is not null)
        {
            var targetType = prop.PropertyType;

            if (targetType.IsEnum)
            {
                if (value.GetType().IsPrimitive)
                {
                    try
                    {
                        prop.SetValue(renderable, Enum.ToObject(targetType, value));
                        return;
                    }
                    catch { /* fall through to base reflection helper */ }
                }
                else if (value is string enumName)
                {
                    // Hand-written .gumx or externally-produced project files
                    // sometimes store enum values by name ("Center") instead
                    // of int. Core Gum doesn't handle this conversion either,
                    // so covering it here closes that compat gap.
                    try
                    {
                        prop.SetValue(renderable, Enum.Parse(targetType, enumName, ignoreCase: true));
                        return;
                    }
                    catch { /* fall through — name not in enum or ambiguous */ }
                }
            }

            var underlyingNullable = Nullable.GetUnderlyingType(targetType);
            if (underlyingNullable is not null)
            {
                try
                {
                    object? converted;
                    if (underlyingNullable.IsEnum)
                    {
                        converted = value is string enumStr
                            ? Enum.Parse(underlyingNullable, enumStr, ignoreCase: true)
                            : Enum.ToObject(underlyingNullable, value);
                    }
                    else
                    {
                        converted = Convert.ChangeType(value, underlyingNullable);
                    }
                    prop.SetValue(renderable, converted);
                    return;
                }
                catch { /* fall through — incompatible primitive → Nullable<T> conversion */ }
            }
        }
        GraphicalUiElement.SetPropertyThroughReflection(renderable, element, propertyName, value);
    }

    /// <summary>
    /// Default layer-attachment: matches the reflection fallback's expected
    /// shape. Plugged into <see cref="GraphicalUiElement.AddRenderableToManagers"/>
    /// so Gum can hand renderables directly to our <see cref="Renderer"/>'s
    /// layer list when no layer is explicitly specified.
    /// </summary>
    public static void AddRenderableToManagers(
        IRenderableIpso renderable,
        ISystemManagers iSystemManagers,
        Layer? layer)
    {
        if (iSystemManagers is not SystemManagers managers) return;
        if (layer is null)
            managers.Renderer.Layers[0].Add(renderable);
        else
            layer.Add(renderable);
    }

    private static bool TrySetOnText(Text text, GraphicalUiElement element, string propertyName, object value)
    {
        var textRuntime = element as TextRuntime;

        switch (propertyName)
        {
            case "Text":
                {
                    var s = value as string ?? string.Empty;
                    text.RawText = LocalizationTranslate is { } tr ? tr(s) : s;
                    // If the text's size is driven by its children (glyphs), layout
                    // must re-run so the parent's measured size picks up the new text.
                    RequestLayoutIfSizedToChildren(element);
                    return true;
                }
            case "TextNoTranslate":
                text.RawText = value as string ?? string.Empty;
                RequestLayoutIfSizedToChildren(element);
                return true;

            case "Font":
                return AssignFont(text, value);

            case "CustomFontFile":
                if (textRuntime is not null && value is string path && !string.IsNullOrEmpty(path))
                {
                    var font = TryLoadFont(path);
                    if (font is not null) text.Font = font;
                }
                return true;

            // These exist in Gum's schema for bitmap-font backends (Raylib/MG/FRB)
            // that regenerate a per-size .fnt cache. Our fontstash-based text
            // applies size/style at rasterization time, so we just swallow
            // them rather than letting reflection spam errors about missing
            // properties. OutlineThickness / LineHeightMultiplier / alignment
            // fall through to reflection because Text now exposes them.
            case "UseCustomFont":
            case "UseFontSmoothing":
            case "IsItalic":
            case "IsBold":
                return true;

            default:
                return false;
        }
    }

    private static bool TrySetOnSprite(Sprite sprite, GraphicalUiElement element, string propertyName, object value)
    {
        switch (propertyName)
        {
            case "SourceFile":
                AssignSpriteSourceFile(sprite, value as string, element);
                element.UpdateLayout();
                return true;

            case "Texture" when value is Texture2D tex:
                sprite.Texture = tex;
                return true;

            case "AnimationChains" when value is AnimationChainList list:
                sprite.AnimationChains = list;
                return true;

            case "CurrentChainName":
                sprite.CurrentChainName = value as string;
                return true;

            case "Animate" when value is bool b:
                sprite.Animate = b;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Routes a Sprite <c>SourceFile</c> to the right content type: a path
    /// ending in <c>.achx</c> is loaded as an <see cref="AnimationChainList"/>
    /// and the first chain is auto-selected, mirroring the behaviour of the
    /// shared XNA-backed Sprite. Any other extension falls back to a
    /// straight texture load.
    /// </summary>
    private static void AssignSpriteSourceFile(Sprite sprite, string? path, GraphicalUiElement element)
    {
        if (string.IsNullOrEmpty(path))
        {
            sprite.Texture = null;
            sprite.AnimationChains = null;
            sprite.CurrentChainName = null;
            return;
        }

        var extension = FileManager.GetExtension(path);
        if (string.Equals(extension, "achx", StringComparison.OrdinalIgnoreCase))
        {
            var resolved = ResolveRelativePath(path);
            AnimationChainList? list = null;
            try
            {
                list = LoaderManager.Self.ContentLoader.LoadContent<AnimationChainList>(resolved);
            }
            catch
            {
                if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException) throw;
            }

            sprite.AnimationChains = list;
            // Pick the first chain as the initial state. Callers typically
            // override CurrentChainName afterwards (e.g. to "IdleLeft") via
            // a separate variable assignment.
            if (list is { Count: > 0 })
                sprite.CurrentChainName = list[0].Name;
        }
        else
        {
            AssignTextureFromPath(t => sprite.Texture = t, path, element);
        }
    }

    private static bool TrySetOnNineSlice(NineSlice nineSlice, GraphicalUiElement element, string propertyName, object value)
    {
        switch (propertyName)
        {
            case "SourceFile":
                AssignNineSliceSourceFile(nineSlice, value as string, element);
                return true;

            case "Texture" when value is Texture2D tex:
                nineSlice.Texture = tex;
                return true;

            case "AnimationChains" when value is AnimationChainList list:
                nineSlice.AnimationChains = list;
                return true;

            case "CurrentChainName":
                nineSlice.CurrentChainName = value as string;
                return true;

            case "Animate" when value is bool b:
                nineSlice.Animate = b;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Routes a NineSlice <c>SourceFile</c> to the right content type: a path
    /// ending in <c>.achx</c> loads an <see cref="AnimationChainList"/> and
    /// auto-selects the first chain, matching the shared XNA NineSlice's
    /// behaviour. Any other extension falls back to a straight texture load.
    /// </summary>
    private static void AssignNineSliceSourceFile(NineSlice nineSlice, string? path, GraphicalUiElement element)
    {
        if (string.IsNullOrEmpty(path))
        {
            nineSlice.Texture = null;
            nineSlice.AnimationChains = null;
            nineSlice.CurrentChainName = null;
            return;
        }

        var extension = FileManager.GetExtension(path);
        if (string.Equals(extension, "achx", StringComparison.OrdinalIgnoreCase))
        {
            var resolved = ResolveRelativePath(path);
            AnimationChainList? list = null;
            try
            {
                list = LoaderManager.Self.ContentLoader.LoadContent<AnimationChainList>(resolved);
            }
            catch
            {
                if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException) throw;
            }

            nineSlice.AnimationChains = list;
            if (list is { Count: > 0 })
                nineSlice.CurrentChainName = list[0].Name;
        }
        else
        {
            AssignTextureFromPath(t => nineSlice.Texture = t, path, element);
        }
    }

    private static bool AssignFont(Text text, object value)
    {
        switch (value)
        {
            case Font font:
                text.Font = font;
                return true;
            case string fontPath when !string.IsNullOrEmpty(fontPath):
                var loaded = TryLoadFont(fontPath);
                if (loaded is not null) text.Font = loaded;
                return true;
            default:
                return false;
        }
    }

    private static Font? TryLoadFont(string path)
    {
        path = ResolveRelativePath(path);
        try
        {
            return LoaderManager.Self.ContentLoader.LoadContent<Font>(path);
        }
        catch
        {
            if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException) throw;
            return null;
        }
    }

    private static void AssignTextureFromPath(
        Action<Texture2D?> apply,
        string? path,
        GraphicalUiElement element)
    {
        if (string.IsNullOrEmpty(path))
        {
            apply(null);
            return;
        }

        path = ResolveRelativePath(path);

        Texture2D? texture = null;
        try
        {
            texture = LoaderManager.Self.ContentLoader.LoadContent<Texture2D>(path);
        }
        catch (Exception ex)
        {
            if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
            {
                var msg = $"Error loading texture for {element.Tag ?? element.Name ?? "renderable"}: {path}\n"
                          + $"Relative directory: {FileManager.RelativeDirectory}";
                throw new FileNotFoundException(msg, ex);
            }
        }

        apply(texture);
    }

    private static string ResolveRelativePath(string path)
    {
        if (FileManager.IsRelative(path) && !FileManager.IsUrl(path))
        {
            path = FileManager.RelativeDirectory + path;
            path = FileManager.RemoveDotDotSlash(path);
        }
        return path;
    }

    private static void RequestLayoutIfSizedToChildren(GraphicalUiElement element)
    {
        if (element.WidthUnits == DimensionUnitType.RelativeToChildren
            || element.HeightUnits == DimensionUnitType.RelativeToChildren)
        {
            element.UpdateLayout();
        }
    }
}
