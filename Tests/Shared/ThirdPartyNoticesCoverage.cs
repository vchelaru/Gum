using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GumTestSupport;

/// <summary>
/// Finds the NuGet packages in a shipped program's <c>.deps.json</c> that THIRD-PARTY-NOTICES.txt does
/// not name on a "NuGet:" line (#5385). Microsoft.*, System.* and runtime.* packages are covered by
/// the notices file's .NET entry and are skipped. Compiled into the Avalonia head and CLI test
/// projects, each of which has its program's deps file in its output folder.
/// </summary>
internal static class ThirdPartyNoticesCoverage
{
    private static readonly string[] _coveredByDotNetEntry = { "Microsoft.", "System.", "runtime." };

    public static IReadOnlyList<string> FindUnlistedPackages(string depsJsonPath, string noticesText)
    {
        HashSet<string> listed = ReadListedPackageIds(noticesText);
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
    /// Reads every "NuGet:" line, including its indented continuation lines, into a case-insensitive set
    /// of package ids and "Prefix.*" patterns.
    /// </summary>
    private static HashSet<string> ReadListedPackageIds(string noticesText)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] lines = noticesText.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            Match match = Regex.Match(lines[i], @"^(\s+)NuGet:\s*(.*)$");
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
