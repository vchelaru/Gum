using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;
using Gum.RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.ProjectServices;

/// <summary>
/// Default type resolver that recognizes common .NET and Gum types.
/// Used as a fallback when the full Gum tool type manager is not available.
/// </summary>
public class DefaultTypeResolver : ITypeResolver
{
    private readonly Dictionary<string, Type> _typesByName;

    public DefaultTypeResolver()
    {
        _typesByName = new Dictionary<string, Type>();
        RegisterCommonTypes();
    }

    /// <inheritdoc/>
    public Type? GetTypeFromString(string typeAsString)
    {
        if (_typesByName.TryGetValue(typeAsString, out var type))
        {
            return type;
        }

        // Try standard .NET type resolution
        type = Type.GetType(typeAsString);
        if (type != null)
        {
            return type;
        }

        return FindInGumCommon(typeAsString);
    }

    // The tool's TypeManager resolves any type of GumCommon by name (GradientType, ColorOperation, GeneralUnitType,
    // Orientation, ...), so a project that names one must resolve here too.
    private Type? FindInGumCommon(string typeAsString)
    {
        _gumCommonTypesByName ??= BuildGumCommonTypes();
        Type? found = null;
        if (_gumCommonTypesByName.TryGetValue(typeAsString, out found))
        {
            return found;
        }

        // "Orientation?" names the nullable enum.
        if (typeAsString.Length > 1 && typeAsString[typeAsString.Length - 1] == '?'
            && _gumCommonTypesByName.TryGetValue(typeAsString.Substring(0, typeAsString.Length - 1), out found)
            && found.IsEnum)
        {
            return typeof(Nullable<>).MakeGenericType(found);
        }
        return null;
    }

    private static Dictionary<string, Type> BuildGumCommonTypes()
    {
        Dictionary<string, Type> types = new Dictionary<string, Type>();
        foreach (Type exported in typeof(DimensionUnitType).Assembly.GetExportedTypes())
        {
            if (exported.FullName != null)
            {
                types.TryAdd(exported.FullName, exported);
            }
            types.TryAdd(exported.Name, exported);
        }
        return types;
    }

    private Dictionary<string, Type>? _gumCommonTypesByName;

    /// <summary>
    /// Registers an additional type for resolution by both its short name and its fully-qualified name.
    /// </summary>
    public void RegisterType(Type type)
    {
        _typesByName[type.Name] = type;
        if (type.FullName != null)
        {
            _typesByName[type.FullName] = type;
        }
    }

    private void RegisterCommonTypes()
    {
        // Register types commonly used in Gum variable definitions
        RegisterType(typeof(bool));
        RegisterType(typeof(int));
        RegisterType(typeof(float));
        RegisterType(typeof(double));
        RegisterType(typeof(string));
        RegisterType(typeof(decimal));
        RegisterType(typeof(byte));
        RegisterType(typeof(long));
        RegisterType(typeof(char));

        // Gum enum types used in standard elements
        RegisterType(typeof(HorizontalAlignment));
        RegisterType(typeof(VerticalAlignment));
        RegisterType(typeof(PositionUnitType));
        RegisterType(typeof(DimensionUnitType));
        RegisterType(typeof(Blend));
        RegisterType(typeof(TextureAddress));
        RegisterType(typeof(ChildrenLayout));
        RegisterType(typeof(TextOverflowHorizontalMode));
        RegisterType(typeof(TextOverflowVerticalMode));
    }
}
