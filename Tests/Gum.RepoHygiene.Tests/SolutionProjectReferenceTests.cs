using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// Every project a checked-in .sln or .slnx references must exist on disk. A deleted csproj whose
/// solution entry survives fails restore for the whole solution (MSB3202), and not every
/// root solution is built by CI, so this pins it instead. See issue #4695.
/// </summary>
public class SolutionProjectReferenceTests
{
    // Project("{type-guid}") = "Name", "relative\path.csproj", "{guid}"
    private static readonly Regex ProjectLine = new(
        @"^Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*"",\s*""(?<path>[^""]+\.[a-z]+proj)""",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

    // <Project Path="relative/path.csproj" />   (.slnx)
    private static readonly Regex SlnxProjectElement = new(
        @"<Project\s+Path\s*=\s*""(?<path>[^""]+\.[a-z]+proj)""", RegexOptions.IgnoreCase);

    // path = fna   (inside a [submodule "..."] section of .gitmodules)
    private static readonly Regex SubmodulePathLine = new(
        @"^\s*path\s*=\s*(?<path>\S+)", RegexOptions.Multiline);

    [Fact]
    public void EveryCheckedInSolution_ReferencesOnlyProjectsThatExist()
    {
        // Submodules are third-party trees: their own .sln files are not ours to police
        // (fna's reference sibling repos that are never checked out here), and they are not
        // initialized in every checkout, so a Gum .sln pointing into one is only checked
        // when the submodule is present.
        List<string> submoduleRoots = ReadSubmoduleRoots(RepoPaths.RepoRoot).ToList();
        List<string> uninitializedSubmoduleRoots = submoduleRoots
            .Where(root => !Directory.Exists(root) || !Directory.EnumerateFileSystemEntries(root).Any())
            .ToList();
        List<string> missing = new();

        foreach (string sln in EnumerateSolutions(RepoPaths.RepoRoot))
        {
            if (IsUnder(sln, submoduleRoots))
            {
                continue;
            }
            string slnDir = Path.GetDirectoryName(sln)!;
            Regex projectReference = sln.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) ? SlnxProjectElement : ProjectLine;
            foreach (Match match in projectReference.Matches(File.ReadAllText(sln)))
            {
                string relative = match.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar);
                string full = Path.GetFullPath(Path.Combine(slnDir, relative));
                if (!IsUnder(full, uninitializedSubmoduleRoots) && !File.Exists(full))
                {
                    missing.Add($"{Path.GetRelativePath(RepoPaths.RepoRoot, sln)} -> {relative}");
                }
            }
        }

        missing.ShouldBeEmpty();
    }

    private static bool IsUnder(string fullPath, IEnumerable<string> roots)
    {
        return roots.Any(root =>
            fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SubmodulePathLine_ReadsPathFromGitmodulesSection()
    {
        string gitmodules = "[submodule \"fna\"]\n\tpath = fna\n\turl = https://example.invalid/fna\n";

        MatchCollection matches = SubmodulePathLine.Matches(gitmodules);

        matches.Select(m => m.Groups["path"].Value).ShouldBe(new[] { "fna" });
    }

    [Fact]
    public void ProjectLine_MatchesSlnEntriesWithBackslashPaths()
    {
        string line = @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Gum"", ""Gum\Gum.csproj"", ""{4A5B6C7D-0000-0000-0000-000000000000}""";

        Match match = ProjectLine.Match(line);

        match.Success.ShouldBeTrue();
        match.Groups["path"].Value.ShouldBe(@"Gum\Gum.csproj");
    }

    [Fact]
    public void SlnxProjectElement_MatchesProjectElementsWithForwardSlashPaths()
    {
        string element = @"  <Project Path=""Tool/Gum.Avalonia/Gum.Avalonia.csproj"" />";

        Match match = SlnxProjectElement.Match(element);

        match.Success.ShouldBeTrue();
        match.Groups["path"].Value.ShouldBe("Tool/Gum.Avalonia/Gum.Avalonia.csproj");
    }

    private static IEnumerable<string> ReadSubmoduleRoots(string repoRoot)
    {
        string gitmodules = Path.Combine(repoRoot, ".gitmodules");
        if (!File.Exists(gitmodules))
        {
            return Enumerable.Empty<string>();
        }
        return SubmodulePathLine.Matches(File.ReadAllText(gitmodules))
            .Select(m => Path.GetFullPath(Path.Combine(repoRoot, m.Groups["path"].Value)));
    }

    private static IEnumerable<string> EnumerateSolutions(string root)
    {
        // Sokol.NET is a hand-cloned, gitignored tree (Runtimes/SokolGum/README.md), not ours.
        return Directory.EnumerateFiles(root, "*.sln", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "*.slnx", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj" or "node_modules" or ".git" or "Sokol.NET"));
    }
}
