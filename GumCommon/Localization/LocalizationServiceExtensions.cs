using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToolsUtilities;

namespace Gum.Localization;

public static class LocalizationServiceExtensions
{
    /// <inheritdoc cref="AddCsvDatabase(ILocalizationService, Stream, Action{string}?)"/>
    public static void AddCsvDatabase(this ILocalizationService service, Stream stream) =>
        AddCsvDatabase(service, stream, onWarning: null);

    /// <summary>
    /// Loads a CSV localization database. The first row holds the headers: the first column is the
    /// string ID and every other column is a language. Each later row is one string ID and its
    /// translations.
    /// </summary>
    /// <remarks>
    /// Cells and headers are trimmed of spaces and tabs outside quotes. A quote inside an unquoted
    /// cell is kept as text, but a quoted cell with unescaped inner quotes throws
    /// <see cref="BadDataException"/>. Rows whose ID is blank or starts with <c>//</c> are skipped.
    /// A cell missing from a short row falls back to the ID; cells past the header are ignored. When
    /// an ID repeats, the later row wins and <paramref name="onWarning"/> is told.
    /// </remarks>
    /// <param name="service">The service to populate.</param>
    /// <param name="stream">The CSV contents.</param>
    /// <param name="onWarning">Receives a message for each repeated string ID.</param>
    public static void AddCsvDatabase(this ILocalizationService service, Stream stream,
        Action<string>? onWarning)
    {
        using var reader = new StreamReader(stream);
        CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            WhiteSpaceChars = new[] { ' ', '\t' },
            BadDataFound = args =>
            {
                if (args.Field.TrimStart(' ', '\t').StartsWith("\"", StringComparison.Ordinal))
                {
                    throw new BadDataException(args.Field, args.RawRecord, args.Context,
                        $"Malformed quoted CSV cell: {args.Field}");
                }
            },
        };
        using var csv = new CsvReader(reader, configuration);

        csv.Read();
        csv.ReadHeader();
        var columnCount = csv.ColumnCount;

        Dictionary<string, string[]> entryDictionary = new Dictionary<string, string[]>();
        // The row each ID was last read from, to name both rows when an ID repeats.
        Dictionary<string, int> rowById = new Dictionary<string, int>();
        // ReadHeader throws when there is no header row, so HeaderRecord is set from here on.
        List<string> headerList = csv.HeaderRecord?.Skip(1).ToList() ?? new List<string>();

        while (csv.Read())
        {
            var stringId = csv.GetField(0);

            // Skip rows with a blank/whitespace ID. Translators commonly leave
            // the ID column empty on dialog continuation rows; storing those
            // would alias every blank-ID row to the same key (last-write-wins)
            // and cause empty Text values to translate to leaked content.
            // See issue #2685.
            // A "//" ID marks a comment row.
            if (string.IsNullOrWhiteSpace(stringId) || stringId!.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            string[] translatedStrings = new string[columnCount];

            translatedStrings[0] = stringId;

            // A row can have fewer cells than the header, e.g. when an editor drops trailing
            // empty cells (#5095). A missing cell falls back to the ID, like a missing RESX
            // translation does.
            int rowCellCount = csv.Parser.Count;
            for (int i = 1; i < columnCount; i++)
            {
                translatedStrings[i] = i < rowCellCount
                    ? csv.GetField(i) ?? stringId
                    : stringId;
            }

            int row = csv.Parser.Row;
            if (rowById.TryGetValue(stringId, out int previousRow))
            {
                onWarning?.Invoke(
                    $"Key '{stringId}' is defined on CSV row {previousRow} and row {row}; using row {row} (last-write-wins).");
            }
            rowById[stringId] = row;

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
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var requestedPath in baseResxFilePaths)
        {
            // A project saved on Windows stores "Localization\Strings.resx"; macOS/Linux read a
            // backslash as part of the file name, so the path and satellite search need native separators.
            var standardizedPath = FileManager.Standardize(requestedPath, preserveCase: true);
            // In a macOS .app the file may be in Contents/Resources/; its satellites sit beside it.
            var baseResxFilePath = FileManager.ResolveExistingFilePath(standardizedPath) ?? standardizedPath;

            // Skip duplicates — a list with the same path twice would otherwise report
            // every key as a collision against itself, spamming onWarning.
            if (!seen.Add(Path.GetFullPath(baseResxFilePath)))
            {
                continue;
            }

            // Path.GetDirectoryName returns "" for a bare filename; Directory.GetFiles("")
            // throws. Normalize to current directory so the public API tolerates either form.
            var directory = Path.GetDirectoryName(baseResxFilePath);
            if (string.IsNullOrEmpty(directory))
            {
                directory = ".";
            }
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
