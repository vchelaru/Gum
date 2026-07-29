using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Holds the result of syntax version detection, including the resolved version number
/// and how it was determined.
/// </summary>
public class SyntaxVersionResult
{
    /// <summary>
    /// The resolved syntax version number. 0 is the baseline (pre-unification).
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// How the version was determined.
    /// </summary>
    public SyntaxVersionSource Source { get; init; }

    /// <summary>
    /// A human-readable description of the detection result, suitable for display in the UI.
    /// </summary>
    public string Description { get; init; } = "";
}

/// <summary>
/// Indicates how the syntax version was determined.
/// </summary>
public enum SyntaxVersionSource
{
    /// <summary>The user set an explicit version in the .codsj file.</summary>
    ManualOverride,

    /// <summary>Auto-detected from a NuGet PackageReference.</summary>
    NuGetPackage,

    /// <summary>Auto-detected from a direct ProjectReference.</summary>
    ProjectReference,

    /// <summary>Auto-detection failed; fell back to default version 0.</summary>
    Fallback
}

/// <inheritdoc/>
public class SyntaxVersionDetectionService : ISyntaxVersionDetectionService
{
    private readonly ICodeGenLogger _logger;
    private readonly string _nuGetCacheRoot;

    public SyntaxVersionDetectionService(ICodeGenLogger logger)
        : this(logger, nuGetCacheRoot: null)
    {
    }

    /// <summary>
    /// Test seam allowing the NuGet global-packages cache root to be overridden, so the
    /// NuGet PackageReference detection path is testable without a populated ~/.nuget/packages.
    /// </summary>
    internal SyntaxVersionDetectionService(ICodeGenLogger logger, string? nuGetCacheRoot)
    {
        _logger = logger;
        _nuGetCacheRoot = nuGetCacheRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");
    }

    /// <inheritdoc/>
    public SyntaxVersionResult Detect(CodeOutputProjectSettings settings, string? projectDirectory)
    {
        string syntaxSetting = settings.SyntaxVersion ?? "*";

        if (syntaxSetting != "*" && int.TryParse(syntaxSetting, out int explicitVersion))
        {
            return new SyntaxVersionResult
            {
                Version = explicitVersion,
                Source = SyntaxVersionSource.ManualOverride,
                Description = $"Syntax Version: {explicitVersion} (manual override)"
            };
        }

        if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(settings.CodeProjectRoot))
        {
            return CreateFallback("No project directory or CodeProjectRoot configured.");
        }

        string codeProjectRoot = settings.CodeProjectRoot;
        if (FileManager.IsRelative(codeProjectRoot))
        {
            // Combine through the Path APIs rather than string concatenation: projectDirectory
            // may or may not end in a separator, and a raw concat like "dir" + "./" produces
            // "dir./" — a literal (nonexistent) directory name on macOS/Linux, though Windows
            // silently trims the trailing dot.
            codeProjectRoot = Path.GetFullPath(Path.Combine(projectDirectory, codeProjectRoot));
        }

        string? csprojPath = FindCsprojInDirectory(codeProjectRoot);
        if (csprojPath == null)
        {
            return CreateFallback($"No .csproj found in {codeProjectRoot}.");
        }

        string csprojContents;
        try
        {
            csprojContents = File.ReadAllText(csprojPath);
        }
        catch (Exception ex)
        {
            return CreateFallback($"Could not read {csprojPath}: {ex.Message}");
        }

        // Try ProjectReference first (direct source linking)
        SyntaxVersionResult? result = TryDetectFromProjectReference(csprojContents, csprojPath);
        if (result != null)
        {
            return result;
        }

        // Try NuGet PackageReference
        result = TryDetectFromNuGetPackage(csprojContents);
        if (result != null)
        {
            return result;
        }

        return CreateFallback("No Gum PackageReference or ProjectReference found in .csproj.");
    }

    private static string? FindCsprojInDirectory(string directory)
    {
        try
        {
            return Directory
                .EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private SyntaxVersionResult? TryDetectFromProjectReference(string csprojContents, string csprojPath)
    {
        // Look for a ProjectReference to a Gum runtime that stamps a GumSyntaxVersion.
        string[] gumProjectNames = { "MonoGameGum", "RaylibGum", "SkiaGum", "KniGum", "FnaGum", "SilkNetGum" };

        foreach (string projectName in gumProjectNames)
        {
            string? relativePath = ExtractProjectReferencePath(csprojContents, projectName);
            if (relativePath == null)
            {
                continue;
            }

            // csproj Include paths use backslash separators by MSBuild convention, even in
            // projects authored on macOS/Linux. Normalize to forward slashes so the Path
            // APIs below resolve the directory correctly on non-Windows platforms (where
            // '\' is not a separator and Path.GetDirectoryName would return an empty string).
            relativePath = relativePath.Replace('\\', '/');

            string csprojDir = Path.GetDirectoryName(csprojPath) ?? csprojPath;
            string referencedProjectDir;
            try
            {
                referencedProjectDir = Path.GetFullPath(Path.Combine(csprojDir, Path.GetDirectoryName(relativePath) ?? ""));
            }
            catch
            {
                continue;
            }

            string assemblyAttributesPath = Path.Combine(referencedProjectDir, "AssemblyAttributes.cs");
            if (!File.Exists(assemblyAttributesPath))
            {
                continue;
            }

            int? version = ParseVersionFromSourceFile(assemblyAttributesPath);
            if (version.HasValue)
            {
                return new SyntaxVersionResult
                {
                    Version = version.Value,
                    Source = SyntaxVersionSource.ProjectReference,
                    Description = $"Syntax Version: {version.Value} (auto-detected from ProjectReference to {projectName})"
                };
            }
        }

        return null;
    }

    private SyntaxVersionResult? TryDetectFromNuGetPackage(string csprojContents)
    {
        // Look for PackageReference to Gum runtime packages (published NuGet package IDs)
        string[] gumPackageNames =
        {
            "Gum.MonoGame",
            "Gum.KNI",
            "Gum.FNA",
            "Gum.SkiaSharp",
            "Gum.raylib",
            "Gum.sokol",
            "Gum.SilkNet"
        };

        foreach (string packageName in gumPackageNames)
        {
            string? packageVersion = ExtractPackageReferenceVersion(csprojContents, packageName);
            if (packageVersion == null)
            {
                continue;
            }

            string? dllPath = FindDllInNuGetCache(packageName, packageVersion);
            if (dllPath == null)
            {
                _logger.PrintOutput($"Could not locate {packageName} {packageVersion} in NuGet cache.");
                continue;
            }

            int? version = ReadVersionFromAssembly(dllPath);
            if (version.HasValue)
            {
                return new SyntaxVersionResult
                {
                    Version = version.Value,
                    Source = SyntaxVersionSource.NuGetPackage,
                    Description = $"Syntax Version: {version.Value} (auto-detected from NuGet package {packageName} {packageVersion})"
                };
            }
        }

        return null;
    }

    internal static string? ExtractProjectReferencePath(string csprojContents, string projectName)
    {
        // Match: <ProjectReference Include="..\..\MonoGameGum\MonoGameGum.csproj" />
        // or: <ProjectReference Include="path\to\MonoGameGum.csproj">
        string pattern = $"<ProjectReference\\s+Include=\"([^\"]*{Regex.Escape(projectName)}[^\"]*)\"";
        Match match = Regex.Match(csprojContents, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    internal static string? ExtractPackageReferenceVersion(string csprojContents, string packageName)
    {
        // Capture the whole element so Version can be found either as an attribute
        // (<PackageReference Include="X" Version="Y" />) or, for csproj files migrated
        // from packages.config, as a nested child element:
        // <PackageReference Include="X"><Version>Y</Version></PackageReference>
        string elementPattern =
            $"<PackageReference\\b(?=[^>]*\\bInclude=\"{Regex.Escape(packageName)}\")[^>]*?(?:/>|>(?<body>.*?)</PackageReference>)";
        Match elementMatch = Regex.Match(csprojContents, elementPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!elementMatch.Success)
        {
            return null;
        }

        Match versionAttribute = Regex.Match(elementMatch.Value, "\\bVersion=\"([^\"]*)\"", RegexOptions.IgnoreCase);
        if (versionAttribute.Success)
        {
            return versionAttribute.Groups[1].Value;
        }

        if (elementMatch.Groups["body"].Success)
        {
            Match versionElement = Regex.Match(elementMatch.Groups["body"].Value, "<Version>([^<]*)</Version>", RegexOptions.IgnoreCase);
            if (versionElement.Success)
            {
                return versionElement.Groups[1].Value;
            }
        }

        return null;
    }

    internal string? FindDllInNuGetCache(string packageName, string version)
    {
        string packageDir = Path.Combine(_nuGetCacheRoot, packageName.ToLowerInvariant(), version, "lib");

        if (!Directory.Exists(packageDir))
        {
            return null;
        }

        // Look through TFM folders in preference order
        string[] preferredTfms = { "net8.0", "net7.0", "net6.0", "netstandard2.1", "netstandard2.0" };

        foreach (string tfm in preferredTfms)
        {
            string tfmDir = Path.Combine(packageDir, tfm);
            if (Directory.Exists(tfmDir))
            {
                string? dll = Directory
                    .EnumerateFiles(tfmDir, "*.dll", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
                if (dll != null)
                {
                    return dll;
                }
            }
        }

        // Fallback: try any subfolder
        try
        {
            return Directory
                .EnumerateFiles(packageDir, "*.dll", SearchOption.AllDirectories)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private int? ReadVersionFromAssembly(string dllPath)
    {
        try
        {
            // Read the assembly-level custom attribute via raw ECMA-335 metadata rather than
            // MetadataLoadContext/reflection. A real Gum.MonoGame/etc. package's dll declares
            // [assembly: GumSyntaxVersion] using a type defined in GumCommon, which ships as its
            // own separate NuGet package -- reflection-based resolution would need every
            // transitively-referenced assembly (GumCommon, and whatever it references) available
            // to the resolver just to identify one attribute's name and read one int field.
            // Reading the metadata tables directly needs none of that: type references are just
            // names/strings in the tables, never resolved to their declaring assembly.
            using FileStream stream = File.OpenRead(dllPath);
            using var peReader = new PEReader(stream);
            MetadataReader reader = peReader.GetMetadataReader();
            AssemblyDefinition assemblyDefinition = reader.GetAssemblyDefinition();

            foreach (CustomAttributeHandle attributeHandle in assemblyDefinition.GetCustomAttributes())
            {
                CustomAttribute customAttribute = reader.GetCustomAttribute(attributeHandle);
                if (GetAttributeTypeName(reader, customAttribute) != nameof(Gum.DataTypes.GumSyntaxVersionAttribute))
                {
                    continue;
                }

                CustomAttributeValue<string> decoded = customAttribute.DecodeValue(NullCustomAttributeTypeProvider.Instance);
                foreach (CustomAttributeNamedArgument<string> namedArgument in decoded.NamedArguments)
                {
                    if (namedArgument.Name == "Version" && namedArgument.Value is int version)
                    {
                        return version;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.PrintError($"Error reading syntax version from {dllPath}: {ex.Message}");
        }

        return null;
    }

    private static string? GetAttributeTypeName(MetadataReader reader, CustomAttribute customAttribute)
    {
        StringHandle nameHandle;
        switch (customAttribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                MemberReference memberReference = reader.GetMemberReference((MemberReferenceHandle)customAttribute.Constructor);
                nameHandle = memberReference.Parent.Kind switch
                {
                    HandleKind.TypeReference => reader.GetTypeReference((TypeReferenceHandle)memberReference.Parent).Name,
                    HandleKind.TypeDefinition => reader.GetTypeDefinition((TypeDefinitionHandle)memberReference.Parent).Name,
                    _ => default
                };
                break;
            case HandleKind.MethodDefinition:
                MethodDefinition methodDefinition = reader.GetMethodDefinition((MethodDefinitionHandle)customAttribute.Constructor);
                nameHandle = reader.GetTypeDefinition(methodDefinition.GetDeclaringType()).Name;
                break;
            default:
                return null;
        }

        return nameHandle.IsNil ? null : reader.GetString(nameHandle);
    }

    /// <summary>
    /// Minimal <see cref="ICustomAttributeTypeProvider{TType}"/> for decoding
    /// <see cref="Gum.DataTypes.GumSyntaxVersionAttribute"/>'s single primitive named argument. Only the
    /// argument's runtime <c>Value</c> (decoded directly by the blob reader for primitives)
    /// is needed here, never the reported <c>TType</c>, so every type-lookup method is a stub.
    /// </summary>
    private sealed class NullCustomAttributeTypeProvider : ICustomAttributeTypeProvider<string>
    {
        public static readonly NullCustomAttributeTypeProvider Instance = new();

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        public string GetSystemType() => "Type";
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => "";
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => "";
        public string GetTypeFromSerializedName(string name) => name;
        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;
        public bool IsSystemType(string type) => false;
    }

    internal static int? ParseVersionFromSourceFile(string filePath)
    {
        try
        {
            string contents = File.ReadAllText(filePath);
            // Match: [assembly: GumSyntaxVersion(Version = 0)]
            Match match = Regex.Match(contents, @"GumSyntaxVersion\s*\(\s*Version\s*=\s*(\d+)\s*\)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int version))
            {
                return version;
            }
        }
        catch
        {
            // Fall through
        }

        return null;
    }

    private SyntaxVersionResult CreateFallback(string reason)
    {
        _logger.PrintOutput($"Syntax version auto-detection: {reason} Falling back to version 0.");
        return new SyntaxVersionResult
        {
            Version = 0,
            Source = SyntaxVersionSource.Fallback,
            Description = $"Syntax Version: 0 (fallback — {reason})"
        };
    }
}
