using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Gum.Plugins;

/// <summary>
/// Finds a type a plugin file uses from one of the tool's own assemblies that the running tool does
/// not have. That is how a plugin built against another build of the tool shows up when the assembly
/// names match: the WPF head's assembly is named Gum like the Avalonia head's, so the reference binds
/// and only the missing type tells them apart. Reads the file's metadata, so nothing of the plugin
/// is loaded or run.
/// </summary>
public static class ToolTypeReferenceChecker
{
    /// <summary>
    /// Returns the first type the plugin at <paramref name="pluginPath"/> references from an
    /// assembly named like one of <paramref name="toolAssemblies"/> that the loaded assembly does not
    /// define or forward, or null when there is none or the file's metadata cannot be read.
    /// </summary>
    public static MissingToolType? FindMissingToolType(string pluginPath, IEnumerable<Assembly> toolAssemblies)
    {
        Dictionary<string, Assembly> toolAssembliesByName = toolAssemblies
            .GroupBy(assembly => assembly.GetName().Name ?? "", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        try
        {
            using FileStream stream = File.OpenRead(pluginPath);
            using PEReader peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
            {
                return null;
            }
            MetadataReader metadata = peReader.GetMetadataReader();

            foreach (TypeReferenceHandle handle in metadata.TypeReferences)
            {
                string typeName = GetFullName(metadata, metadata.GetTypeReference(handle), out EntityHandle rootScope);
                if (rootScope.Kind != HandleKind.AssemblyReference)
                {
                    continue;
                }
                AssemblyReference reference = metadata.GetAssemblyReference((AssemblyReferenceHandle)rootScope);
                string assemblyName = metadata.GetString(reference.Name);
                if (toolAssembliesByName.TryGetValue(assemblyName, out Assembly? toolAssembly) &&
                    toolAssembly.GetType(typeName, throwOnError: false) == null)
                {
                    return new MissingToolType(typeName, assemblyName);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException)
        {
            // The loader reports an unreadable file in its own terms.
        }

        return null;
    }

    /// <summary>The reflection name of the type (nested types joined with '+'), and the scope that roots it.</summary>
    private static string GetFullName(MetadataReader metadata, TypeReference type, out EntityHandle rootScope)
    {
        string name = metadata.GetString(type.Name);
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            TypeReference declaringType = metadata.GetTypeReference((TypeReferenceHandle)type.ResolutionScope);
            return GetFullName(metadata, declaringType, out rootScope) + "+" + name;
        }

        rootScope = type.ResolutionScope;
        string typeNamespace = metadata.GetString(type.Namespace);
        return typeNamespace.Length == 0 ? name : typeNamespace + "." + name;
    }
}

/// <summary>A type a plugin uses that the named tool assembly does not have.</summary>
public sealed record MissingToolType(string TypeName, string AssemblyName);
