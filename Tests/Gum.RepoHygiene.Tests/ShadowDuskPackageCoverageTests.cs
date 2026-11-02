using System.Xml.Linq;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// Every <c>*.ShadowDusk</c> project under <c>Runtimes/</c> is an opt-in NuGet package. The publish
/// workflow packs <c>AllLibraries.sln</c>, so a project missing from it, or one that is not packable,
/// builds and tests fine but never reaches nuget.org (#7273).
/// </summary>
public class ShadowDuskPackageCoverageTests
{
    [Fact]
    public void ShadowDuskProjects_AreFound()
    {
        // Guards the other tests from passing vacuously if the folder layout changes.
        EnumerateShadowDuskProjects().Count.ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void EveryShadowDuskProject_IsInAllLibrariesSolution()
    {
        string solution = File.ReadAllText(Path.Combine(RepoPaths.RepoRoot, "AllLibraries.sln"))
            .Replace('/', '\\');
        List<string> missing = new();

        foreach (string project in EnumerateShadowDuskProjects())
        {
            string relative = Path.GetRelativePath(RepoPaths.RepoRoot, project).Replace('/', '\\');
            if (!solution.Contains("\"" + relative + "\"", StringComparison.OrdinalIgnoreCase))
            {
                missing.Add(relative);
            }
        }

        missing.ShouldBeEmpty("dotnet-nuget.yaml packs AllLibraries.sln, so these projects would never be published");
    }

    [Fact]
    public void EveryShadowDuskProject_IsPackable()
    {
        List<string> offenders = new();

        foreach (string project in EnumerateShadowDuskProjects())
        {
            XDocument document = XDocument.Load(project);
            string relative = Path.GetRelativePath(RepoPaths.RepoRoot, project).Replace('/', '\\');
            if (!document.Descendants("PackageId").Any())
            {
                offenders.Add(relative + ": no PackageId");
            }
            if (document.Descendants("IsPackable")
                .Any(element => string.Equals(element.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase)))
            {
                offenders.Add(relative + ": IsPackable is false");
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void PublishFilter_DoesNotHoldBackShadowDuskPackages()
    {
        string filter = File.ReadAllText(Path.Combine(RepoPaths.RepoRoot, "Tools", "Get-PublishablePackages.ps1"));

        filter.ShouldNotContain("ShadowDusk", Case.Insensitive);
    }

    private static List<string> EnumerateShadowDuskProjects()
    {
        string runtimes = Path.Combine(RepoPaths.RepoRoot, "Runtimes");
        string[] excludedSegments = { "bin", "obj" };

        return Directory.EnumerateFiles(runtimes, "*.ShadowDusk.csproj", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(runtimes, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => excludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)))
            .ToList();
    }
}
