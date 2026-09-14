using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// Every project a checked-in .sln references must exist on disk. A deleted csproj whose
/// solution entry survives fails restore for the whole solution (MSB3202), and not every
/// root solution is built by CI, so this pins it instead. See issue #4695.
/// </summary>
public class SolutionProjectReferenceTests
{
    // Project("{type-guid}") = "Name", "relative\path.csproj", "{guid}"
    private static readonly Regex ProjectLine = new(
        @"^Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*"",\s*""(?<path>[^""]+\.[a-z]+proj)""",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

    // path = fna   (inside a [submodule "..."] section of .gitmodules)
    private static readonly Regex SubmodulePathLine = new(
        @"^\s*path\s*=\s*(?<path>\S+)", RegexOptions.Multiline);

    [Fact]
    public void EveryCheckedInSolution_ReferencesOnlyProjectsThatExist()
    {
        // Submodules are not initialized in every checkout (CI skips fna), so a project
        // inside one is only checked when the submodule is actually present.
        List<string> submoduleRoots = ReadSubmoduleRoots(RepoPaths.RepoRoot)
            .Where(root => !Directory.EnumerateFileSystemEntries(root).Any())
            .ToList();
        List<string> missing = new();

        foreach (string sln in EnumerateSolutions(RepoPaths.RepoRoot))
        {
            string slnDir = Path.GetDirectoryName(sln)!;
            foreach (Match match in ProjectLine.Matches(File.ReadAllText(sln)))
            {
                string relative = match.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar);
                string full = Path.GetFullPath(Path.Combine(slnDir, relative));
                bool inUninitializedSubmodule = submoduleRoots.Any(root =>
                    full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
                if (!inUninitializedSubmodule && !File.Exists(full))
                {
                    missing.Add($"{Path.GetRelativePath(RepoPaths.RepoRoot, sln)} -> {relative}");
                }
            }
        }

        missing.ShouldBeEmpty();
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
        return Directory.EnumerateFiles(root, "*.sln", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj" or "node_modules" or ".git"));
    }
}
