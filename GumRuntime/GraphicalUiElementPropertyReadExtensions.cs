using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gum.Converters;
using Gum.Managers;

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
    /// Re-evaluates the same-component variable references that read the named variables, using each
    /// variable's current value. Call it after changing a variable through a typed property, such as
    /// <c>element.X = 5</c>, when a reference reads that variable. Animations and state application
    /// already do this. See <see cref="GraphicalUiElement.NotifyVariablesChanged"/>.
    /// </summary>
    /// <param name="element">The component, screen or instance whose references to refresh.</param>
    /// <param name="variableNames">The variables that changed, relative to <paramref name="element"/>, such as
    /// <c>X</c> or <c>Instance.Width</c>. A name that cannot be read is skipped.</param>
    public static void RefreshReferences(this GraphicalUiElement element, params string[] variableNames)
    {
        List<Gum.DataTypes.Variables.VariableSave> changed = new List<Gum.DataTypes.Variables.VariableSave>();
        foreach (string name in variableNames)
        {
            GraphicalUiElement owner = element;
            string propertyName = name;
            int dot = name.IndexOf('.');
            if (dot >= 0)
            {
                GraphicalUiElement? instance = element.GetGraphicalUiElementByName(name.Substring(0, dot));
                if (instance == null)
                {
                    continue;
                }
                owner = instance;
                propertyName = name.Substring(dot + 1);
            }

            if (owner.TryGetPropertyExact(propertyName, out object? value))
            {
                changed.Add(new Gum.DataTypes.Variables.VariableSave { Name = name, Value = value, SetsValue = true });
            }
        }

        element.NotifyVariablesChanged(changed);
    }

    /// <summary>
    /// Attempts to read the current value of a Gum variable (e.g. "X", "Width", "WidthUnits", "Red",
    /// "Text") by name, trying the curated base-set switch first and reflection as a fallback.
    /// </summary>
    /// <param name="element">The element to read from.</param>
    /// <param name="propertyName">The Gum variable name.</param>
    /// <param name="value">The current value when found; otherwise null.</param>
    /// <returns>True if the property was recognized and read; otherwise false.</returns>
    public static bool TryGetProperty(this GraphicalUiElement element, string propertyName, out object? value) =>
        TryGetProperty(element, propertyName, exact: false, out value);

    /// <summary>
    /// Reads a variable for a same-component reference, which must carry the value unchanged. Differs from
    /// <see cref="TryGetProperty(GraphicalUiElement, string, out object?)"/>, whose values are what a saved project
    /// can hold, in two cases: an XUnits or YUnits with no <see cref="PositionUnitType"/> counterpart (set in code,
    /// such as <see cref="GeneralUnitType.PercentageOfFile"/>) comes back as the <see cref="GeneralUnitType"/>, and
    /// a fractional FontSize stays a float instead of rounding to the int the saved variable is.
    /// <see cref="GraphicalUiElement.SetProperty(string, object?)"/> accepts both.
    /// </summary>
    internal static bool TryGetPropertyExact(this GraphicalUiElement element, string propertyName, out object? value) =>
        TryGetProperty(element, propertyName, exact: true, out value);

    private static bool TryGetProperty(GraphicalUiElement element, string propertyName, bool exact, out object? value)
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
#if !FRB && (NET6_0_OR_GREATER || NETSTANDARD2_1)
            case "ExposeChildrenEvents":
                if (element is InteractiveGue exposeChildrenEventsGue)
                {
                    value = exposeChildrenEventsGue.ExposeChildrenEvents;
                    return true;
                }
                return false;
#endif
#if !FRB && (NET6_0_OR_GREATER || NETSTANDARD2_1)
            case "RaiseChildrenEventsOutsideOfBounds":
                if (element is InteractiveGue raiseOutsideGue)
                {
                    value = raiseOutsideGue.RaiseChildrenEventsOutsideOfBounds;
                    return true;
                }
                return false;
#endif
            case "FlipHorizontal":
                value = element.FlipHorizontal;
                return true;
#if !FRB && (NET6_0_OR_GREATER || NETSTANDARD2_1)
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
                value = GetPositionUnitValue(element.XUnits, isXAxis: true, exact);
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
                value = GetPositionUnitValue(element.YUnits, isXAxis: false, exact);
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

                // The FontSize variable is an int, but TextRuntime.FontSize is a float. A float here
                // would fail the (int) cast when the value is written to another element's FontSize.
                // An exact read keeps only a fractional one as a float; a whole one is the int the variable is, as
                // at creation, so it can select an enum value or be cast like any int (#6446). One the int cannot
                // hold (infinity, NaN, beyond the int range) stays a float (#6452).
                if (value is float fontSize && propertyName == "FontSize" &&
                    (!exact || (fontSize == Math.Floor(fontSize) && GumRuntime.ElementSaveExtensions.IsInIntRange(fontSize))))
                {
                    value = (int)Math.Round(fontSize);
                }
                return true;
            }
            catch (TargetInvocationException)
            {
                // A property getter threw; treat as unreadable rather than failing the whole read.
            }
        }

        return false;
    }

    // The saved variable is a PositionUnitType, which has no value for the dimension-only units or, on X, the
    // obsolete inverted center. An exact read keeps those as the GeneralUnitType rather than reading them as pixels.
    private static object GetPositionUnitValue(GeneralUnitType unit, bool isXAxis, bool exact)
    {
        PositionUnitType positionUnit = UnitConverter.ConvertToPositionUnit(unit, isXAxis);
        if (exact && UnitConverter.ConvertToGeneralUnit(positionUnit) != unit)
        {
            return unit;
        }
        return positionUnit;
    }

    // Memoizes the public, readable, non-indexer property per (runtime type, Gum variable name).
    // A null entry caches "no reflectable property" so repeated misses stay cheap.
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = new();

    /// <summary>
    /// Reads the live texture object (a Sprite/NineSlice ".Texture") for this element, if any, without
    /// taking a dependency on the backend Texture2D type. The snapshot serializer uses this to detect a
    /// texture that has no resolvable file path (an embedded/runtime-generated texture whose Name is unset)
    /// so a platform layer can extract it to a file.
    /// </summary>
    /// <param name="element">The element to read from.</param>
    /// <param name="texture">The live texture object when present; otherwise null.</param>
    /// <returns>True when the element exposes a non-null texture; otherwise false.</returns>
    public static bool TryGetTexture(this GraphicalUiElement element, out object? texture)
    {
        // Standard runtimes (SpriteRuntime/NineSliceRuntime) expose Texture directly on the runtime type.
        PropertyInfo? textureProperty = ResolveReflectedProperty(element.GetType(), "Texture");
        texture = textureProperty?.GetValue(element);
        if (texture != null)
        {
            return true;
        }

        // A bare InteractiveGue (e.g. a Forms Image's visual) has no Texture property of its own -- the
        // texture lives on its Sprite/NineSlice renderable. Read it from there so the snapshot still finds it.
        object? renderable = element.RenderableComponent;
        if (renderable != null)
        {
            PropertyInfo? renderableTextureProperty = ResolveReflectedProperty(renderable.GetType(), "Texture");
            texture = renderableTextureProperty?.GetValue(renderable);
        }
        return texture != null;
    }

    // Recovers a Sprite/NineSlice source path from its texture's Name (stamped by the content loader on
    // load). Done by reflection so this shared reader needs no reference to the backend texture type.
    private static string? TryReadTextureName(GraphicalUiElement element)
    {
        if (!element.TryGetTexture(out object? texture) || texture == null)
        {
            return null;
        }

        PropertyInfo? nameProperty = ResolveReflectedProperty(texture.GetType(), "Name");
        string? name = nameProperty?.GetValue(texture) as string;
        return string.IsNullOrEmpty(name) ? null : name;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Best-effort read of a backend renderable's or texture's property. A trimmed " +
            "property resolves to null, which every caller already treats as 'not available'.")]
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
