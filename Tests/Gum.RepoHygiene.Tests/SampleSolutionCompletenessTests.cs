using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// A sample's .sln lists every project its csproj files reference, including Debug-only ones such as
/// Gum.Automation. Samples are opened and run from the .sln in Visual Studio, and a referenced project
/// the .sln does not list is not built there, so the sample fails to compile in Debug while CI's
/// Release build stays green.
/// </summary>
public class SampleSolutionCompletenessTests
{
    private static readonly Regex ProjectPath = new(
        @"^Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*"",\s*""(?<path>[^""]+\.[a-z]+proj)""",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

    [Fact]
    public void EverySampleSolution_ListsEveryProjectItsProjectsReference()
    {
        string samplesRoot = Path.Combine(RepoPaths.RepoRoot, "Samples");
        List<string> unlisted = new();

        foreach (string sln in Directory.EnumerateFiles(samplesRoot, "*.sln", SearchOption.AllDirectories)
            .Where(path => !IsExcluded(path)))
        {
            string slnDir = Path.GetDirectoryName(sln)!;
            HashSet<string> listed = ProjectPath.Matches(File.ReadAllText(sln))
                .Select(m => Resolve(slnDir, m.Groups["path"].Value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string project in listed.Where(p => File.Exists(p) && IsUnder(p, slnDir)))
            {
                foreach (XElement reference in XDocument.Load(project).Descendants("ProjectReference"))
                {
                    string target = Resolve(Path.GetDirectoryName(project)!, reference.Attribute("Include")!.Value);
                    if (!listed.Contains(target))
                    {
                        unlisted.Add($"{Path.GetRelativePath(RepoPaths.RepoRoot, sln)}: {Path.GetFileName(project)} -> {Path.GetRelativePath(RepoPaths.RepoRoot, target)}");
                    }
                }
            }
        }

        unlisted.ShouldBeEmpty();
    }

    private static string Resolve(string baseDir, string relative)
    {
        return Path.GetFullPath(Path.Combine(baseDir, relative.Replace('\\', Path.DirectorySeparatorChar)));
    }

    private static bool IsUnder(string path, string dir)
    {
        return path.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExcluded(string path)
    {
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj" or "Sokol.NET");
    }
}
