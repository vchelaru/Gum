using System.Text.Json;
using System.Xml.Linq;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// A component or screen that sets <c>Foo.Bar</c> must have an instance named Foo (its own or one
/// inherited from a base component); otherwise the value is silently ignored (#7206, unblocks the
/// <c>gumcli check</c> rule in #7119). Scans every committed project, including the shipped
/// Forms template and themes.
/// </summary>
public class ElementGhostVariableTests
{
    private static readonly string[] _roots = { "Samples", "Tests", "Tools", "Gum", "Docs", "docs" };
    private static readonly string[] _skipped = { "bin", "obj", "node_modules", ".git", "fna" };
    private static readonly string[] _elementExtensions = { ".gucx", ".gusx", ".gucj", ".gusj" };

    [Fact]
    public void CommittedProjects_ElementVariables_ShouldOnlyNameInstancesTheElementHas()
    {
        List<string> ghosts = new();

        foreach (string projectFolder in FindProjectFolders())
        {
            Dictionary<string, ElementInfo> elementsByName = new(StringComparer.Ordinal);
            foreach (string file in EnumerateFiles(projectFolder).Where(f => _elementExtensions.Contains(Path.GetExtension(f))))
            {
                ElementInfo info = Load(file);
                elementsByName[info.Name] = info;
            }

            foreach (ElementInfo element in elementsByName.Values)
            {
                HashSet<string> instances = GetInstanceNamesIncludingBases(element, elementsByName);
                foreach (string variable in element.VariableNames)
                {
                    int dot = variable.IndexOf('.');
                    if (dot > 0 && !instances.Contains(variable.Substring(0, dot)))
                    {
                        ghosts.Add(Path.GetRelativePath(RepoPaths.RepoRoot, element.File) + " : " + variable);
                    }
                }
            }
        }

        ghosts.ShouldBeEmpty(string.Join(Environment.NewLine, ghosts));
    }

    private static HashSet<string> GetInstanceNamesIncludingBases(ElementInfo element, Dictionary<string, ElementInfo> all)
    {
        HashSet<string> result = new();
        HashSet<string> visited = new();
        ElementInfo? current = element;
        while (current != null && visited.Add(current.Name))
        {
            result.UnionWith(current.InstanceNames);
            current = current.BaseType != null && all.TryGetValue(current.BaseType, out ElementInfo? b) ? b : null;
        }
        return result;
    }

    private static List<string> FindProjectFolders()
    {
        List<string> folders = new();
        foreach (string root in _roots)
        {
            string path = Path.Combine(RepoPaths.RepoRoot, root);
            if (!Directory.Exists(path)) continue;
            folders.AddRange(EnumerateFiles(path)
                .Where(f => f.EndsWith(".gumx") || f.EndsWith(".gumj"))
                .Select(f => Path.GetDirectoryName(f)!));
        }
        return folders.Distinct().ToList();
    }

    private static IEnumerable<string> EnumerateFiles(string folder)
    {
        Stack<string> pending = new();
        pending.Push(folder);
        while (pending.Count > 0)
        {
            string current = pending.Pop();
            foreach (string file in Directory.EnumerateFiles(current)) yield return file;
            foreach (string sub in Directory.EnumerateDirectories(current))
            {
                if (!_skipped.Contains(Path.GetFileName(sub))) pending.Push(sub);
            }
        }
    }

    private static ElementInfo Load(string file)
    {
        if (file.EndsWith("x"))
        {
            XElement root = XDocument.Load(file).Root!;
            return new ElementInfo(
                file,
                root.Element("Name")?.Value ?? "",
                root.Element("BaseType")?.Value,
                root.Elements("Instance").Select(i => i.Element("Name")?.Value ?? "").ToList(),
                root.Descendants("State").SelectMany(s => s.Elements("Variable"))
                    .Select(v => v.Element("Name")?.Value ?? "").Distinct().ToList());
        }

        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(file), new JsonDocumentOptions { AllowTrailingCommas = true });
        JsonElement r = json.RootElement;
        List<JsonElement> states = new();
        if (r.TryGetProperty("States", out JsonElement s)) states.AddRange(s.EnumerateArray());
        if (r.TryGetProperty("Categories", out JsonElement cats))
        {
            foreach (JsonElement c in cats.EnumerateArray())
            {
                if (c.TryGetProperty("States", out JsonElement cs)) states.AddRange(cs.EnumerateArray());
            }
        }
        List<string> variables = states
            .Where(st => st.TryGetProperty("Variables", out _))
            .SelectMany(st => st.GetProperty("Variables").EnumerateArray())
            .Select(v => v.GetProperty("Name").GetString() ?? "").Distinct().ToList();
        List<string> instances = r.TryGetProperty("Instances", out JsonElement ins)
            ? ins.EnumerateArray().Select(i => i.GetProperty("Name").GetString() ?? "").ToList()
            : new List<string>();
        return new ElementInfo(
            file,
            r.TryGetProperty("Name", out JsonElement n) ? n.GetString() ?? "" : "",
            r.TryGetProperty("BaseType", out JsonElement b) ? b.GetString() : null,
            instances,
            variables);
    }

    private sealed record ElementInfo(string File, string Name, string? BaseType, List<string> InstanceNames, List<string> VariableNames);
}
