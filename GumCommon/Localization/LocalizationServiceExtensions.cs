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
        var directory = Path.GetDirectoryName(baseResxFilePath)!;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseResxFilePath);

        // Collect all .resx files: base file first, then satellite files sorted alphabetically
        var resxFiles = new List<(string languageName, string filePath)>();

        // The base file represents the default language
        resxFiles.Add((fileNameWithoutExtension, baseResxFilePath));

        // Discover satellite files matching the pattern: BaseName.{culture}.resx
        var searchPattern = fileNameWithoutExtension + ".*.resx";
        foreach (var satelliteFile in Directory.GetFiles(directory, searchPattern).OrderBy(f => f))
        {
            // Extract the culture portion from e.g. "Strings.es.resx" -> "es"
            var satelliteFileName = Path.GetFileNameWithoutExtension(satelliteFile);
            var cultureName = satelliteFileName.Substring(fileNameWithoutExtension.Length + 1);

            resxFiles.Add((cultureName, satelliteFile));
        }

        // Read all files into streams and delegate to the stream-based overload
        var resxStreams = new List<(string languageName, Stream stream)>();
        try
        {
            foreach (var (languageName, filePath) in resxFiles)
            {
                var stream = File.OpenRead(filePath);
                resxStreams.Add((languageName, stream));
            }

            service.AddResxDatabase(resxStreams);
        }
        finally
        {
            foreach (var (_, stream) in resxStreams)
            {
                stream.Dispose();
            }
        }
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
        var streamList = resxStreams.ToList();
        var headerList = streamList.Select(s => s.languageName).ToList();

        // Parse each .resx stream into a dictionary of name -> value
        var languageDictionaries = new List<Dictionary<string, string>>();
        foreach (var (_, stream) in streamList)
        {
            languageDictionaries.Add(ReadResxStream(stream));
        }

        // Collect all string IDs across all languages
        var allStringIds = new HashSet<string>();
        foreach (var dict in languageDictionaries)
        {
            foreach (var key in dict.Keys)
            {
                allStringIds.Add(key);
            }
        }

        // Build the entry dictionary in the same format as the CSV loader:
        // translatedStrings[0] = stringId, translatedStrings[1..N] = translations
        var totalColumns = streamList.Count + 1;
        var entryDictionary = new Dictionary<string, string[]>();

        foreach (var stringId in allStringIds)
        {
            var translatedStrings = new string[totalColumns];
            translatedStrings[0] = stringId;

            for (int i = 0; i < streamList.Count; i++)
            {
                if (languageDictionaries[i].TryGetValue(stringId, out var value))
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

        service.AddDatabase(entryDictionary, headerList);
    }

    private static Dictionary<string, string> ReadResxStream(Stream stream)
    {
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
