using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

    public SyntaxVersionDetectionService(ICodeGenLogger logger)
    {
        _logger = logger;
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
            codeProjectRoot = projectDirectory + codeProjectRoot;
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
        // Look for ProjectReference to MonoGameGum, RaylibGum, or SkiaGum
        string[] gumProjectNames = { "MonoGameGum", "RaylibGum", "SkiaGum", "KniGum", "FnaGum" };

        foreach (string projectName in gumProjectNames)
        {
            string? relativePath = ExtractProjectReferencePath(csprojContents, projectName);
            if (relativePath == null)
            {
                continue;
            }

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
        // Look for PackageReference to Gum runtime packages
        string[] gumPackageNames =
        {
            "FlatRedBall.MonoGameGum",
            "FlatRedBall.SkiaGum",
            "FlatRedBall.RaylibGum",
            // Also check non-FRB package names in case they're used
            "MonoGameGum",
            "SkiaGum",
            "RaylibGum"
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
        // Match: <PackageReference Include="FlatRedBall.MonoGameGum" Version="2026.4.1" />
        // or: <PackageReference Include="FlatRedBall.MonoGameGum" Version="2026.4.1">
        string pattern = $"<PackageReference\\s+Include=\"{Regex.Escape(packageName)}\"\\s+Version=\"([^\"]*)\"";
        Match match = Regex.Match(csprojContents, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? FindDllInNuGetCache(string packageName, string version)
    {
        string nugetCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");

        string packageDir = Path.Combine(nugetCache, packageName.ToLowerInvariant(), version, "lib");

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
            // Use MetadataLoadContext to avoid loading into the app domain
            var resolver = new PathAssemblyResolver(new[] { dllPath, typeof(object).Assembly.Location });
            using var context = new MetadataLoadContext(resolver);
            Assembly assembly = context.LoadFromAssemblyPath(dllPath);

            foreach (CustomAttributeData attr in assembly.GetCustomAttributesData())
            {
                if (attr.AttributeType.Name == nameof(Gum.DataTypes.GumSyntaxVersionAttribute))
                {
                    foreach (CustomAttributeNamedArgument namedArg in attr.NamedArguments)
                    {
                        if (namedArg.MemberName == "Version" && namedArg.TypedValue.Value is int version)
                        {
                            return version;
                        }
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
