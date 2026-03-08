using System;
using Gum.ProjectServices.CodeGeneration;
using Gum.Reflection;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Adapts the Gum tool's TypeManager to the headless ITypeStringResolver interface.
/// </summary>
internal class ToolTypeStringResolver : ITypeStringResolver
{
    /// <inheritdoc/>
    public Type? GetTypeFromString(string typeAsString)
    {
        return TypeManager.Self.GetTypeFromString(typeAsString);
    }
}
