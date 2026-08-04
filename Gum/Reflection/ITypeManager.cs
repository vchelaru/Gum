using System;
using System.Diagnostics.CodeAnalysis;

namespace Gum.Reflection;

public interface ITypeManager
{
    void AddType(Type type);

    [RequiresDynamicCode("Resolving a nullable-enum type name calls Type.MakeGenericType, which requires generating new code at runtime.")]
    Type GetTypeFromString(string typeAsString);

    [RequiresUnreferencedCode("Scans all types in the executing, RenderingLibrary, and GumCommon-linked assemblies via Assembly.GetTypes(), which can be incomplete or throw after trimming.")]
    void Initialize();
}
