using System;
using Gum.ProjectServices;
using Gum.Reflection;

namespace Gum.Managers;

/// <summary>
/// Adapts the tool's <see cref="ITypeManager"/> to the headless
/// <see cref="ITypeResolver"/> interface used by <see cref="HeadlessErrorChecker"/>.
/// </summary>
public class TypeManagerTypeResolverAdapter : ITypeResolver
{
    private readonly ITypeManager _typeManager;

    public TypeManagerTypeResolverAdapter(ITypeManager typeManager)
    {
        _typeManager = typeManager;
    }

    /// <inheritdoc/>
    public Type? GetTypeFromString(string typeAsString)
    {
        return _typeManager.GetTypeFromString(typeAsString);
    }
}
