using System;
using Gum.ProjectServices.CodeGeneration;
using Gum.Reflection;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Adapts the Gum tool's TypeManager to the headless ITypeStringResolver interface.
/// </summary>
public class ToolTypeStringResolver : ITypeStringResolver
{
    private readonly ITypeManager _typeManager;

    public ToolTypeStringResolver(ITypeManager typeManager)
    {
        _typeManager = typeManager;
    }

    /// <inheritdoc/>
    public Type? GetTypeFromString(string typeAsString)
    {
        return _typeManager.GetTypeFromString(typeAsString);
    }
}
