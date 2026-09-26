using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace PluginHostFixture;

/// <summary>
/// Writes minimal plugin-shaped assemblies to disk for plugin-host tests: one public class whose
/// base type lives in another assembly, so a test can stand in for a plugin built against some
/// other build of the tool without compiling one.
/// </summary>
public static class PluginAssemblyWriter
{
    private static readonly Version FixtureVersion = new Version(1, 0, 0, 0);

    /// <summary>
    /// Writes <paramref name="assemblyName"/>.dll into <paramref name="directory"/>, holding the
    /// public class <c>{assemblyName}.Entry</c> deriving from <paramref name="baseTypeNamespace"/>.
    /// <paramref name="baseTypeName"/> in the assembly named <paramref name="baseAssemblyName"/>.
    /// <paramref name="otherReferences"/> are extra assembly references with nothing used from them,
    /// the way a real plugin references Gum.Presentation. Returns the file's path.
    /// </summary>
    public static string WriteDerivingFrom(string directory, string assemblyName,
        string baseAssemblyName, string baseTypeNamespace, string baseTypeName, params string[] otherReferences)
    {
        MetadataBuilder metadata = StartAssembly(assemblyName);
        foreach (string reference in otherReferences)
        {
            metadata.AddAssemblyReference(metadata.GetOrAddString(reference), new Version(0, 0, 0, 0),
                default, default, default, default);
        }
        AssemblyReferenceHandle baseAssembly = metadata.AddAssemblyReference(metadata.GetOrAddString(baseAssemblyName),
            FixtureVersion, default, default, default, default);
        TypeReferenceHandle baseType = metadata.AddTypeReference(baseAssembly,
            metadata.GetOrAddString(baseTypeNamespace), metadata.GetOrAddString(baseTypeName));
        metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Class,
            metadata.GetOrAddString(assemblyName), metadata.GetOrAddString("Entry"), baseType,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        return Write(directory, assemblyName, metadata);
    }

    /// <summary>Writes <paramref name="assemblyName"/>.dll into <paramref name="directory"/> with no types; returns its path.</summary>
    public static string WriteEmpty(string directory, string assemblyName)
    {
        return Write(directory, assemblyName, StartAssembly(assemblyName));
    }

    private static MetadataBuilder StartAssembly(string assemblyName)
    {
        MetadataBuilder metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString(assemblyName + ".dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString(assemblyName), FixtureVersion, default, default, default, AssemblyHashAlgorithm.Sha1);
        metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        return metadata;
    }

    private static string Write(string directory, string assemblyName, MetadataBuilder metadata)
    {
        ManagedPEBuilder peBuilder = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(),
            new MetadataRootBuilder(metadata), new BlobBuilder());
        BlobBuilder image = new BlobBuilder();
        peBuilder.Serialize(image);

        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, assemblyName + ".dll");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }
}
