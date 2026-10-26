using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Gum.Cli.Commands;

/// <summary>
/// Reads the element names in an assembly's <c>[assembly: GumElementRoot("...")]</c> attributes from
/// its metadata, without loading the assembly (it may target another runtime or platform).
/// </summary>
internal static class GumElementRootAssemblyReader
{
    private const string AttributeNamespace = "Gum.DataTypes";
    private const string AttributeName = "GumElementRootAttribute";

    internal static List<string> Read(string assemblyPath)
    {
        List<string> names = new List<string>();
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new PEReader(stream);
        MetadataReader metadata = peReader.GetMetadataReader();

        foreach (CustomAttributeHandle handle in metadata.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = metadata.GetCustomAttribute(handle);
            if (!IsGumElementRoot(metadata, attribute))
            {
                continue;
            }

            // Blob: 0x0001 prolog, then the single string constructor argument.
            BlobReader blob = metadata.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() == 1 && blob.ReadSerializedString() is string name && name.Length > 0)
            {
                names.Add(name);
            }
        }
        return names;
    }

    private static bool IsGumElementRoot(MetadataReader metadata, CustomAttribute attribute)
    {
        EntityHandle type;
        if (attribute.Constructor.Kind == HandleKind.MemberReference)
        {
            type = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
        }
        else if (attribute.Constructor.Kind == HandleKind.MethodDefinition)
        {
            type = metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
        }
        else
        {
            return false;
        }

        return type.Kind switch
        {
            HandleKind.TypeReference => Matches(metadata, metadata.GetTypeReference((TypeReferenceHandle)type).Namespace,
                metadata.GetTypeReference((TypeReferenceHandle)type).Name),
            HandleKind.TypeDefinition => Matches(metadata, metadata.GetTypeDefinition((TypeDefinitionHandle)type).Namespace,
                metadata.GetTypeDefinition((TypeDefinitionHandle)type).Name),
            _ => false,
        };
    }

    private static bool Matches(MetadataReader metadata, StringHandle typeNamespace, StringHandle name) =>
        metadata.StringComparer.Equals(typeNamespace, AttributeNamespace)
        && metadata.StringComparer.Equals(name, AttributeName);
}
