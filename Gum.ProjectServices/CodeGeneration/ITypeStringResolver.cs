using System;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Resolves a type name string (e.g. "float", "bool", "Gum.DataTypes.DimensionUnitType") to a <see cref="Type"/>.
/// In the Gum tool, this delegates to TypeManager. In headless/CLI mode, a basic resolver is used.
/// </summary>
public interface ITypeStringResolver
{
    /// <summary>
    /// Returns the <see cref="Type"/> for the given type name, or null if not found.
    /// </summary>
    Type? GetTypeFromString(string typeAsString);
}
