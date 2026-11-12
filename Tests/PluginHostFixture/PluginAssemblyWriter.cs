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
        return WriteDerivingFrom(directory, assemblyName, baseAssemblyName, FixtureVersion,
            baseTypeNamespace, baseTypeName, otherReferences);
    }

    /// <summary>As the overload without a version, with the base assembly referenced at <paramref name="baseAssemblyVersion"/>.</summary>
    public static string WriteDerivingFrom(string directory, string assemblyName,
        string baseAssemblyName, Version baseAssemblyVersion, string baseTypeNamespace, string baseTypeName,
        params string[] otherReferences)
    {
        MetadataBuilder metadata = StartAssembly(assemblyName);
        foreach (string reference in otherReferences)
        {
            metadata.AddAssemblyReference(metadata.GetOrAddString(reference), new Version(0, 0, 0, 0),
                default, default, default, default);
        }
        AssemblyReferenceHandle baseAssembly = metadata.AddAssemblyReference(metadata.GetOrAddString(baseAssemblyName),
            baseAssemblyVersion, default, default, default, default);
        TypeReferenceHandle baseType = metadata.AddTypeReference(baseAssembly,
            metadata.GetOrAddString(baseTypeNamespace), metadata.GetOrAddString(baseTypeName));
        metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Class,
            metadata.GetOrAddString(assemblyName), metadata.GetOrAddString("Entry"), baseType,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        return Write(directory, assemblyName, metadata);
    }

    /// <summary>
    /// Writes a plugin-shaped assembly whose class <c>{assemblyName}.Entry</c> derives from
    /// <c>System.Object</c> and has a static <c>Touch()</c> that only constructs the public type
    /// <paramref name="typeNamespace"/>.<paramref name="typeName"/> of <paramref name="referencedAssemblyName"/>
    /// (referenced at <paramref name="referencedVersion"/>). The reference binds when <c>Touch</c> is first
    /// compiled, not when the assembly or its types load, like a plugin using a type only inside a method body.
    /// </summary>
    public static string WriteTouchingInMethodBody(string directory, string assemblyName,
        string referencedAssemblyName, Version referencedVersion, string typeNamespace, string typeName)
    {
        MetadataBuilder metadata = StartAssembly(assemblyName);
        AssemblyReferenceHandle runtime = metadata.AddAssemblyReference(metadata.GetOrAddString("System.Runtime"),
            new Version(10, 0, 0, 0), default,
            metadata.GetOrAddBlob(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }), default, default);
        TypeReferenceHandle objectType = metadata.AddTypeReference(runtime,
            metadata.GetOrAddString("System"), metadata.GetOrAddString("Object"));
        AssemblyReferenceHandle referenced = metadata.AddAssemblyReference(metadata.GetOrAddString(referencedAssemblyName),
            referencedVersion, default, default, default, default);
        TypeReferenceHandle touched = metadata.AddTypeReference(referenced,
            metadata.GetOrAddString(typeNamespace), metadata.GetOrAddString(typeName));

        BlobBuilder ctorSignature = new BlobBuilder();
        new BlobEncoder(ctorSignature).MethodSignature(isInstanceMethod: true).Parameters(0, returnType => returnType.Void(), parameters => { });
        MemberReferenceHandle constructor = metadata.AddMemberReference(touched, metadata.GetOrAddString(".ctor"),
            metadata.GetOrAddBlob(ctorSignature));

        BlobBuilder code = new BlobBuilder();
        InstructionEncoder instructions = new InstructionEncoder(code);
        instructions.OpCode(ILOpCode.Newobj);
        instructions.Token(constructor);
        instructions.OpCode(ILOpCode.Pop);
        instructions.OpCode(ILOpCode.Ret);
        BlobBuilder ilStream = new BlobBuilder();
        int bodyOffset = new MethodBodyStreamEncoder(ilStream).AddMethodBody(instructions);

        BlobBuilder methodSignature = new BlobBuilder();
        new BlobEncoder(methodSignature).MethodSignature().Parameters(0, returnType => returnType.Void(), parameters => { });
        metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Class,
            metadata.GetOrAddString(assemblyName), metadata.GetOrAddString("Entry"), objectType,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
            metadata.GetOrAddString("Touch"), metadata.GetOrAddBlob(methodSignature), bodyOffset, default);

        return Write(directory, assemblyName, metadata, ilStream);
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

    private static string Write(string directory, string assemblyName, MetadataBuilder metadata, BlobBuilder? ilStream = null)
    {
        ManagedPEBuilder peBuilder = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(),
            new MetadataRootBuilder(metadata), ilStream ?? new BlobBuilder());
        BlobBuilder image = new BlobBuilder();
        peBuilder.Serialize(image);

        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, assemblyName + ".dll");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }
}
