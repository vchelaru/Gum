using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GumTestSupport;

/// <summary>
/// Checks a shipped program against THIRD-PARTY-NOTICES.txt. Finds the NuGet packages in a shipped program's <c>.deps.json</c> that THIRD-PARTY-NOTICES.txt does
/// not name on a "NuGet:" line (#5385). Microsoft.*, System.* and runtime.* packages are covered by
/// the notices file's .NET entry and are skipped. Compiled into the Avalonia head and CLI test
/// projects, each of which has its program's deps file in its output folder.
/// </summary>
internal static class ThirdPartyNoticesCoverage
{
    private static readonly string[] _coveredByDotNetEntry = { "Microsoft.", "System.", "runtime." };

    public static IReadOnlyList<string> FindUnlistedPackages(string depsJsonPath, string noticesText)
    {
        HashSet<string> listed = ReadListedIds(noticesText, "NuGet");
        string[] prefixes = listed
            .Where(id => id.EndsWith(".*", StringComparison.Ordinal))
            .Select(id => id.Substring(0, id.Length - 1))
            .ToArray();

        using JsonDocument deps = JsonDocument.Parse(File.ReadAllText(depsJsonPath));
        List<string> unlisted = new List<string>();
        foreach (JsonProperty library in deps.RootElement.GetProperty("libraries").EnumerateObject())
        {
            if (library.Value.GetProperty("type").GetString() != "package")
            {
                continue;
            }
            string id = library.Name.Substring(0, library.Name.IndexOf('/'));
            bool covered = listed.Contains(id)
                || prefixes.Any(prefix => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                || _coveredByDotNetEntry.Any(prefix => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (!covered)
            {
                unlisted.Add(id);
            }
        }
        return unlisted.Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Finds the native programs and libraries (.exe, .dll, .so, .dylib) embedded as manifest resources in
    /// the shipped program's own project assemblies that THIRD-PARTY-NOTICES.txt does not name on an
    /// "Embedded:" line (#5447). These are vendored binaries (bmfont.exe in Gum.ProjectServices) that no
    /// NuGet package declares, so <see cref="FindUnlistedPackages"/> cannot see them.
    /// </summary>
    public static IReadOnlyList<string> FindUnlistedEmbeddedBinaries(string depsJsonPath, string noticesText)
    {
        HashSet<string> listed = ReadListedIds(noticesText, "Embedded");
        string directory = Path.GetDirectoryName(depsJsonPath)!;

        using JsonDocument deps = JsonDocument.Parse(File.ReadAllText(depsJsonPath));
        JsonElement libraries = deps.RootElement.GetProperty("libraries");
        List<string> unlisted = new List<string>();
        foreach (JsonProperty target in deps.RootElement.GetProperty("targets").EnumerateObject())
        {
            foreach (JsonProperty library in target.Value.EnumerateObject())
            {
                if (libraries.GetProperty(library.Name).GetProperty("type").GetString() != "project"
                    || !library.Value.TryGetProperty("runtime", out JsonElement runtime))
                {
                    continue;
                }
                foreach (JsonProperty assemblyFile in runtime.EnumerateObject())
                {
                    string assemblyPath = Path.Combine(directory, assemblyFile.Name);
                    foreach (string resource in ReadEmbeddedBinaryNames(assemblyPath))
                    {
                        bool covered = listed.Any(name => resource.Equals(name, StringComparison.OrdinalIgnoreCase)
                            || resource.EndsWith("." + name, StringComparison.OrdinalIgnoreCase));
                        if (!covered)
                        {
                            unlisted.Add($"{resource} (in {assemblyFile.Name})");
                        }
                    }
                }
            }
        }
        return unlisted.Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    private static readonly string[] _binaryExtensions = { ".exe", ".dll", ".so", ".dylib" };

    private static IEnumerable<string> ReadEmbeddedBinaryNames(string assemblyPath)
    {
        using FileStream stream = File.OpenRead(assemblyPath);
        using PEReader peReader = new PEReader(stream);
        MetadataReader metadata = peReader.GetMetadataReader();
        List<string> names = new List<string>();
        foreach (ManifestResourceHandle handle in metadata.ManifestResources)
        {
            string name = metadata.GetString(metadata.GetManifestResource(handle).Name);
            if (_binaryExtensions.Any(extension => name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                names.Add(name);
            }
        }
        return names;
    }

    /// <summary>
    /// Reads every "<label>:" line (e.g. "NuGet:"), including its indented continuation lines, into a
    /// case-insensitive set of ids (package ids and "Prefix.*" patterns for NuGet).
    /// </summary>
    private static HashSet<string> ReadListedIds(string noticesText, string label)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] lines = noticesText.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            Match match = Regex.Match(lines[i], @"^(\s+)" + label + @":\s*(.*)$");
            if (!match.Success)
            {
                continue;
            }
            int indent = match.Groups[1].Length;
            string text = match.Groups[2].Value;
            // A list that wraps continues on lines indented deeper than the "NuGet:" line.
            while (i + 1 < lines.Length && text.TrimEnd().EndsWith(",", StringComparison.Ordinal)
                && lines[i + 1].Length - lines[i + 1].TrimStart().Length > indent)
            {
                i++;
                text += " " + lines[i].Trim();
            }
            foreach (string id in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                ids.Add(id);
            }
        }
        return ids;
    }
}
