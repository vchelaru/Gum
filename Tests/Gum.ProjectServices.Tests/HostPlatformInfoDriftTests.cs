using System.Xml.Linq;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The per-runtime facts (assembly name, package id) are kept in Gum.ProjectServices, not discovered from
/// the csprojs. A runtime added to the repo and not to those lists fails here with the line to add.
/// </summary>
public class HostPlatformInfoDriftTests
{
    // Add-on packages (Gum.Expressions, Gum.Shapes.*) share the "Gum." prefix but are not a runtime whose
    // version the syntax-version detection reads, so they stay out of the package-id list on purpose.
    private static readonly string[] AddOnPackageIdPrefixes = { "Gum.Expressions", "Gum.Shapes." };

    [Fact]
    public void EveryRuntimeAssemblyName_IsReservedAsAProjectName()
    {
        IReadOnlyCollection<string> reserved = GetReservedProjectNames();

        List<string> problems = ReadRuntimeProjects()
            .Where(project => !reserved.Contains(project.AssemblyName, StringComparer.OrdinalIgnoreCase))
            .Select(project => $"add \"{project.AssemblyName}\" ({project.CsprojPath}) to the Gum runtime assembly names")
            .ToList();

        problems.ShouldBeEmpty(Numbered(problems));
    }

    [Fact]
    public void EveryRuntimePackageId_IsAKnownGumPackageName()
    {
        IReadOnlyCollection<string> known = GetKnownPackageIds();

        List<string> problems = ReadRuntimeProjects()
            .Where(project => project.PackageId != null
                && project.PackageId.StartsWith("Gum.", StringComparison.Ordinal)
                && !AddOnPackageIdPrefixes.Any(prefix => project.PackageId.StartsWith(prefix, StringComparison.Ordinal))
                && !known.Contains(project.PackageId))
            .Select(project => $"add \"{project.PackageId}\" ({project.CsprojPath}) to the Gum runtime package ids")
            .ToList();

        problems.ShouldBeEmpty(Numbered(problems));
    }

    private static string Numbered(IReadOnlyList<string> problems) =>
        string.Join(Environment.NewLine, problems.Select((problem, index) => $"{index + 1}. {problem}"));

    private static IReadOnlyCollection<string> GetReservedProjectNames() => HostPlatformInfo.ReservedAssemblyNames;

    private static IReadOnlyCollection<string> GetKnownPackageIds() => HostPlatformInfo.PackageIds;

    [Fact]
    public void EveryTableEntry_PointsAtAnExistingRuntimeProject()
    {
        List<string> problems = HostPlatformInfo.All
            .Where(info => !File.Exists(Path.Combine(RepoPaths.Root, info.ProjectPath)))
            .Select(info => $"fix the project path of \"{info.AssemblyName}\": {info.ProjectPath} does not exist")
            .ToList();

        problems.ShouldBeEmpty(Numbered(problems));
    }

    [Fact]
    public void EveryHostPlatform_HasARuntimeEntry()
    {
        List<string> problems = Enum.GetValues<HostPlatform>()
            .Where(platform => HostPlatformInfo.All.Count(info => info.Platform == platform) != 1)
            .Select(platform => $"give HostPlatform.{platform} exactly one entry in HostPlatformInfo.All")
            .ToList();

        problems.ShouldBeEmpty(Numbered(problems));
    }

    private record RuntimeProject(string CsprojPath, string AssemblyName, string? PackageId);

    // A runtime is a csproj that ships as a NuGet package (a PackageId, IsPackable not false) or that is a
    // <Engine>Gum project directly under Runtimes/ (UnityGum is copied into a Unity project, not packed).
    private static IEnumerable<RuntimeProject> ReadRuntimeProjects()
    {
        string[] roots = { "Runtimes", "MonoGameGum", "GumCommon" };
        foreach (string root in roots)
        {
            string rootPath = Path.Combine(RepoPaths.Root, root);
            foreach (string path in Directory.EnumerateFiles(rootPath, "*.csproj", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(RepoPaths.Root, path).Replace('\\', '/');
                if (relative.Contains("/obj/") || relative.Contains("/bin/") || relative.Contains("Tests"))
                {
                    continue;
                }

                XElement project = XDocument.Load(path).Root!;
                string? packageId = project.Descendants("PackageId").FirstOrDefault()?.Value;
                string? isPackable = project.Descendants("IsPackable").FirstOrDefault()?.Value;
                string assemblyName = project.Descendants("AssemblyName").FirstOrDefault()?.Value
                    ?? Path.GetFileNameWithoutExtension(path);

                bool packed = packageId != null && !string.Equals(isPackable, "false", StringComparison.OrdinalIgnoreCase);
                bool engineRuntime = root == "Runtimes" && assemblyName.EndsWith("Gum", StringComparison.Ordinal)
                    && Path.GetFileName(Path.GetDirectoryName(path)) == assemblyName;
                if (packed || engineRuntime)
                {
                    yield return new RuntimeProject(relative, assemblyName, packed ? packageId : null);
                }
            }
        }
    }
}
