using System.Text;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The bytes of every file in a project folder at one moment, so a scenario can prove that undoing
/// its edits, or saving what it reloaded, leaves the project exactly as it was.
/// </summary>
internal sealed class ProjectFileSnapshot
{
    // Folders under the project that hold per-user or generated files rather than project data.
    // EventExport is the Event Output plugin's journal: it appends every edit, an undo included.
    private static readonly string[] IgnoredFolders = ["UserData", "FontCache", "EventExport"];

    private readonly SortedDictionary<string, byte[]> _files;

    private ProjectFileSnapshot(SortedDictionary<string, byte[]> files)
    {
        _files = files;
    }

    /// <summary>The project-relative paths captured, with forward slashes.</summary>
    public IReadOnlyCollection<string> Paths => _files.Keys;

    /// <summary>Reads every project file under <paramref name="projectFolder"/>.</summary>
    public static ProjectFileSnapshot Take(string projectFolder)
    {
        SortedDictionary<string, byte[]> files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(projectFolder, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(projectFolder, path).Replace('\\', '/');
            if (IgnoredFolders.Any(folder => relative.StartsWith(folder + "/", StringComparison.Ordinal)))
            {
                continue;
            }
            files[relative] = File.ReadAllBytes(path);
        }
        return new ProjectFileSnapshot(files);
    }

    /// <summary>
    /// Throws when the two snapshots differ, naming each added, removed or changed file and the
    /// first line that differs in a changed one.
    /// </summary>
    public void ShouldMatch(ProjectFileSnapshot expected, string because)
    {
        StringBuilder differences = new StringBuilder();
        foreach (string path in expected._files.Keys.Except(_files.Keys))
        {
            differences.AppendLine($"  missing: {path}");
        }
        foreach (string path in _files.Keys.Except(expected._files.Keys))
        {
            differences.AppendLine($"  added: {path}");
        }
        foreach (string path in _files.Keys.Intersect(expected._files.Keys))
        {
            if (!_files[path].AsSpan().SequenceEqual(expected._files[path]))
            {
                differences.AppendLine($"  changed: {path}: {FirstDifferentLine(expected._files[path], _files[path])}");
            }
        }
        if (differences.Length > 0)
        {
            string saved = SaveBothSides(expected);
            throw new Shouldly.ShouldAssertException($"Project files differ; {because}:{Environment.NewLine}{differences}Both versions are in {saved}");
        }
    }

    // Writes the expected and actual files to a temp folder, so a failure can be diffed after the
    // test has deleted its project.
    private string SaveBothSides(ProjectFileSnapshot expected)
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumEndToEnd", "diffs", Guid.NewGuid().ToString("N"));
        foreach ((string side, ProjectFileSnapshot snapshot) in new[] { ("expected", expected), ("actual", this) })
        {
            foreach ((string path, byte[] bytes) in snapshot._files)
            {
                string target = Path.Combine(folder, side, path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllBytes(target, bytes);
            }
        }
        return folder;
    }

    private static string FirstDifferentLine(byte[] expected, byte[] actual)
    {
        string[] expectedLines = Encoding.UTF8.GetString(expected).Split('\n');
        string[] actualLines = Encoding.UTF8.GetString(actual).Split('\n');
        for (int i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            string expectedLine = i < expectedLines.Length ? expectedLines[i].TrimEnd('\r') : "<end of file>";
            string actualLine = i < actualLines.Length ? actualLines[i].TrimEnd('\r') : "<end of file>";
            if (expectedLine != actualLine)
            {
                return $"line {i + 1} was \"{expectedLine.Trim()}\", now \"{actualLine.Trim()}\"";
            }
        }
        return "line endings or encoding differ";
    }
}
