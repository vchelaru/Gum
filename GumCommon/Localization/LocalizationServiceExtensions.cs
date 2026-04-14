using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Gum.Localization;

public static class LocalizationServiceExtensions
{
    public static void AddCsvDatabase(this ILocalizationService service, Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();
        var columnCount = csv.ColumnCount;

        Dictionary<string, string[]> entryDictionary = new Dictionary<string, string[]>();
        List<string> headerList = csv.HeaderRecord.Skip(1).ToList();

        while (csv.Read())
        {
            var stringId = csv.GetField(0);

            string[] translatedStrings = new string[columnCount];

            translatedStrings[0] = stringId;

            for (int i = 1; i < columnCount; i++)
            {
                translatedStrings[i] = csv.GetField(i);
            }

            entryDictionary[stringId] = translatedStrings;
        }

        service.AddDatabase(entryDictionary, headerList);
    }

    /// <summary>
    /// Loads localization data from a base .resx file and any satellite .resx files
    /// discovered by convention in the same directory (e.g., Strings.resx, Strings.es.resx,
    /// Strings.fr.resx).
    /// </summary>
    /// <param name="service">The localization service to populate.</param>
    /// <param name="baseResxFilePath">Path to the base (default language) .resx file.</param>
    public static void AddResxDatabase(this ILocalizationService service, string baseResxFilePath)
    {
        AddResxDatabase(service, new[] { baseResxFilePath }, onWarning: null);
    }

    /// <summary>
    /// Loads localization data from multiple base .resx files, each with their own satellite
    /// files discovered by convention (e.g., Strings.resx + Strings.es.resx, Buttons.resx +
    /// Buttons.es.resx). Languages are unioned across all files; missing translations fall
    /// back to the string ID. String IDs are merged across files with last-write-wins on
    /// collision.
    /// </summary>
    /// <param name="service">The localization service to populate.</param>
    /// <param name="baseResxFilePaths">Paths to the base (default language) .resx files.</param>
    /// <param name="onWarning">Optional callback invoked with a descriptive message when
    /// a string ID collision occurs across files. Not invoked for other events. Does not
    /// write to Debug or Console by default because this runtime ships in games.</param>
    public static void AddResxDatabase(this ILocalizationService service,
        IEnumerable<string> baseResxFilePaths,
        Action<string>? onWarning = null)
    {
        var fileGroups = new List<FileGroup>();

        foreach (var baseResxFilePath in baseResxFilePaths)
        {
            var directory = Path.GetDirectoryName(baseResxFilePath)!;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseResxFilePath);

            var filesForGroup = new List<(string languageName, string filePath)>();
            filesForGroup.Add(("Default", baseResxFilePath));

            // Note: this pattern will match any file of the shape {BaseName}.*.resx, including
            // unintended ones like Strings.backup.resx. Callers are responsible for keeping the
            // directory clean of non-localization files matching this shape.
            var searchPattern = fileNameWithoutExtension + ".*.resx";
            foreach (var satelliteFile in Directory.GetFiles(directory, searchPattern).OrderBy(f => f))
            {
                var satelliteFileName = Path.GetFileNameWithoutExtension(satelliteFile);
                var cultureName = satelliteFileName.Substring(fileNameWithoutExtension.Length + 1);
                filesForGroup.Add((cultureName, satelliteFile));
            }

            var group = new FileGroup
            {
                DisplayName = Path.GetFileName(baseResxFilePath),
                LanguageEntries = new List<(string languageName, Dictionary<string, string> entries)>()
            };

            foreach (var (languageName, filePath) in filesForGroup)
            {
                using var stream = File.OpenRead(filePath);
                group.LanguageEntries.Add((languageName, ReadResxStream(stream)));
            }

            fileGroups.Add(group);
        }

        BuildAndAddDatabase(service, fileGroups, onWarning);
    }

    /// <summary>
    /// Loads localization data from multiple .resx streams, one per language. Each stream
    /// should contain standard .resx XML. The first stream is treated as the default language.
    /// </summary>
    /// <param name="service">The localization service to populate.</param>
    /// <param name="resxStreams">A collection of (languageName, stream) pairs, one per language.</param>
    public static void AddResxDatabase(this ILocalizationService service,
        IEnumerable<(string languageName, Stream stream)> resxStreams)
    {
        AddResxDatabase(service,
            new[] { (groupName: (string?)null, streams: resxStreams) },
            onWarning: null);
    }

    /// <summary>
    /// Loads localization data from multiple groups of .resx streams. Each group represents
    /// one base-file worth of per-language streams (e.g., one group for Strings.resx + its
    /// satellites, another for Buttons.resx + its satellites). Languages are unioned across
    /// all groups; missing translations fall back to the string ID. String IDs are merged
    /// across groups with last-write-wins on collision. Use this on mobile/web where
    /// Directory.GetFiles is unavailable.
    /// </summary>
    /// <param name="service">The localization service to populate.</param>
    /// <param name="fileGroups">Groups of (languageName, stream) pairs, one group per file.</param>
    /// <param name="onWarning">Optional callback invoked with a descriptive message when
    /// a string ID collision occurs across groups.</param>
    public static void AddResxDatabase(this ILocalizationService service,
        IEnumerable<IEnumerable<(string languageName, Stream stream)>> fileGroups,
        Action<string>? onWarning = null)
    {
        AddResxDatabase(service,
            fileGroups.Select(g => (groupName: (string?)null, streams: g)),
            onWarning);
    }

    /// <summary>
    /// Loads localization data from multiple named groups of .resx streams. The group name
    /// is used in collision warning messages; pass null to fall back to "Group {index}".
    /// Behaves identically to the unnamed multi-group overload otherwise.
    /// </summary>
    /// <param name="service">The localization service to populate.</param>
    /// <param name="fileGroups">Groups of (groupName, streams) pairs. groupName may be null.</param>
    /// <param name="onWarning">Optional callback invoked with a descriptive message when
    /// a string ID collision occurs across groups.</param>
    public static void AddResxDatabase(this ILocalizationService service,
        IEnumerable<(string? groupName, IEnumerable<(string languageName, Stream stream)> streams)> fileGroups,
        Action<string>? onWarning = null)
    {
        var parsedGroups = new List<FileGroup>();

        var groupIndex = 0;
        foreach (var (groupName, group) in fileGroups)
        {
            var parsed = new FileGroup
            {
                DisplayName = groupName ?? ("Group " + groupIndex),
                LanguageEntries = new List<(string languageName, Dictionary<string, string> entries)>()
            };

            foreach (var (languageName, stream) in group)
            {
                parsed.LanguageEntries.Add((languageName, ReadResxStream(stream)));
            }

            parsedGroups.Add(parsed);
            groupIndex++;
        }

        BuildAndAddDatabase(service, parsedGroups, onWarning);
    }

    private class FileGroup
    {
        public string DisplayName { get; set; } = "";
        public List<(string languageName, Dictionary<string, string> entries)> LanguageEntries { get; set; }
            = new List<(string, Dictionary<string, string>)>();
    }

    private static void BuildAndAddDatabase(ILocalizationService service,
        List<FileGroup> fileGroups,
        Action<string>? onWarning)
    {
        // Union language names across groups, preserving first-seen order.
        var headerList = new List<string>();
        var seenLanguages = new HashSet<string>();
        foreach (var group in fileGroups)
        {
            foreach (var (languageName, _) in group.LanguageEntries)
            {
                if (seenLanguages.Add(languageName))
                {
                    headerList.Add(languageName);
                }
            }
        }

        var totalColumns = headerList.Count + 1;
        var entryDictionary = new Dictionary<string, string[]>();

        // Track all prior file-groups that provided each stringId for collision reporting.
        var stringIdToSourceGroups = new Dictionary<string, List<string>>();

        foreach (var group in fileGroups)
        {
            // Build per-language lookup for this group keyed by language name.
            var languageMap = new Dictionary<string, Dictionary<string, string>>();
            foreach (var (languageName, entries) in group.LanguageEntries)
            {
                languageMap[languageName] = entries;
            }

            // Collect all string IDs present anywhere in this group.
            var stringIdsInGroup = new HashSet<string>();
            foreach (var (_, entries) in group.LanguageEntries)
            {
                foreach (var key in entries.Keys)
                {
                    stringIdsInGroup.Add(key);
                }
            }

            foreach (var stringId in stringIdsInGroup)
            {
                if (stringIdToSourceGroups.TryGetValue(stringId, out var previousSources))
                {
                    var priorList = "[" + string.Join(", ", previousSources.Select(s => $"'{s}'")) + "]";
                    onWarning?.Invoke(
                        $"Key '{stringId}' collision: overwriting value from {priorList} with value from '{group.DisplayName}' (last-write-wins).");
                    previousSources.Add(group.DisplayName);
                }
                else
                {
                    stringIdToSourceGroups[stringId] = new List<string> { group.DisplayName };
                }

                var translatedStrings = new string[totalColumns];
                translatedStrings[0] = stringId;

                for (var i = 0; i < headerList.Count; i++)
                {
                    var languageName = headerList[i];
                    if (languageMap.TryGetValue(languageName, out var entries)
                        && entries.TryGetValue(stringId, out var value))
                    {
                        translatedStrings[i + 1] = value;
                    }
                    else
                    {
                        translatedStrings[i + 1] = stringId;
                    }
                }

                entryDictionary[stringId] = translatedStrings;
            }
        }

        service.AddDatabase(entryDictionary, headerList);
    }

    private static Dictionary<string, string> ReadResxStream(Stream stream)
    {
        // Entries with a null name attribute or null value element are silently skipped
        // (pre-existing behavior). Malformed entries do not raise exceptions.
        var result = new Dictionary<string, string>();
        var doc = XDocument.Load(stream);

        foreach (var dataElement in doc.Descendants("data"))
        {
            var name = dataElement.Attribute("name")?.Value;
            var value = dataElement.Element("value")?.Value;

            if (name != null && value != null)
            {
                result[name] = value;
            }
        }

        return result;
    }
}
