using System;

namespace Gum.ProjectServices;

/// <summary>
/// Resolves type names to .NET types for variable type validation.
/// </summary>
public interface ITypeResolver
{
    Type? GetTypeFromString(string typeAsString);
}
