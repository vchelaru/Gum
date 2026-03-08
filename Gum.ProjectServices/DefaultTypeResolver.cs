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
        return type;
    }

    /// <summary>
    /// Registers an additional type for resolution.
    /// </summary>
    public void RegisterType(Type type)
    {
        _typesByName[type.Name] = type;
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
