using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// The publish workflow packs <c>AllLibraries.sln</c>. A packed project's ProjectReference becomes a
/// NuGet dependency on the referenced project's package, so a referenced project that is outside the
/// solution (or not packable) is never published and the consumer's restore fails with NU1101 (#7682).
/// </summary>
public class PackedProjectDependencyTests
{
    private static readonly Regex SolutionProject = new(
        @"^Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*"",\s*""(?<path>[^""]+\.csproj)""",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

    [Fact]
    public void PackedProjects_AreFound()
    {
        // Guards the other test from passing vacuously if the solution layout changes.
        EnumerateSolutionProjects().Count(IsPackable).ShouldBeGreaterThan(20);
    }

    [Fact]
    public void EveryProjectReferencedByAPackedProject_IsAlsoPacked()
    {
        HashSet<string> inSolution = EnumerateSolutionProjects().ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> offenders = new();

        foreach (string project in inSolution.Where(p => File.Exists(p) && IsPackable(p)))
        {
            foreach (string referenced in EnumerateReferencedProjects(project))
            {
                if (!inSolution.Contains(referenced) || !IsPackable(referenced))
                {
                    offenders.Add(Relative(project) + " -> " + Relative(referenced));
                }
            }
        }

        offenders.ShouldBeEmpty("a referenced project that dotnet pack AllLibraries.sln does not pack is a dependency nobody can restore");
    }

    private static IEnumerable<string> EnumerateSolutionProjects()
    {
        string solution = Path.Combine(RepoPaths.RepoRoot, "AllLibraries.sln");
        foreach (Match match in SolutionProject.Matches(File.ReadAllText(solution)))
        {
            string relative = match.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(RepoPaths.RepoRoot, relative));
            yield return full;
        }
    }

    private static bool IsPackable(string project)
    {
        // Submodule projects (fna) are not always checked out; the solution still lists them.
        if (!File.Exists(project))
        {
            return true;
        }
        string name = Path.GetFileNameWithoutExtension(project);
        if (name.Contains("Tests") || name.Contains("Benchmarks"))
        {
            return false;
        }
        XDocument document = XDocument.Load(project);
        return !document.Descendants("IsPackable")
            .Any(e => e.Value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> EnumerateReferencedProjects(string project)
    {
        string directory = Path.GetDirectoryName(project)!;
        foreach (XElement reference in XDocument.Load(project).Descendants("ProjectReference"))
        {
            // A reference that does not flow to the package is not a dependency.
            bool notAFlowingDependency =
                reference.Elements("ReferenceOutputAssembly").Any(e => e.Value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                || reference.Elements("PrivateAssets").Any(e => e.Value.Trim().Equals("all", StringComparison.OrdinalIgnoreCase));
            string? include = (string?)reference.Attribute("Include");
            if (notAFlowingDependency || include is null)
            {
                continue;
            }
            yield return Path.GetFullPath(Path.Combine(directory, include.Replace('\\', Path.DirectorySeparatorChar)));
        }
    }

    private static string Relative(string path) => Path.GetRelativePath(RepoPaths.RepoRoot, path);
}
