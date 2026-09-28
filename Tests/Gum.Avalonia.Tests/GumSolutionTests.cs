using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Visual Studio and Rider restore only the projects listed in the loaded solution, so a
/// ProjectReference to a project missing from Gum.slnx fails there with NU1105 even though
/// <c>dotnet build Gum.slnx</c> restores it transitively and passes.
/// </summary>
public class GumSolutionTests
{
    [Fact]
    public void Slnx_ListsEveryProjectItsProjectsReference()
    {
        string root = FindRepositoryRoot();
        string slnx = File.ReadAllText(Path.Combine(root, "Gum.slnx"));
        HashSet<string> listed = Regex.Matches(slnx, "Path=\"([^\"]+\\.\\w+proj)\"")
            .Select(match => Normalize(Path.Combine(root, match.Groups[1].Value)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> missing = new List<string>();
        HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Stack<string> pending = new Stack<string>(listed);
        while (pending.Count > 0)
        {
            string project = pending.Pop();
            if (!visited.Add(project))
            {
                continue;
            }
            string directory = Path.GetDirectoryName(project)!;
            foreach (Match reference in Regex.Matches(File.ReadAllText(project), "<ProjectReference\\s+Include=\"([^\"]+)\""))
            {
                string referenced = Normalize(Path.Combine(directory, reference.Groups[1].Value));
                if (!listed.Contains(referenced))
                {
                    missing.Add($"{Path.GetRelativePath(root, referenced)} (referenced by {Path.GetRelativePath(root, project)})");
                }
                pending.Push(referenced);
            }
        }

        missing.Distinct().ShouldBeEmpty();
    }

    private static string Normalize(string path) =>
        Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar));

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gum.slnx")))
            {
                return directory.FullName;
            }
        }
        throw new InvalidOperationException("The test is not running inside the Gum repository.");
    }
}
