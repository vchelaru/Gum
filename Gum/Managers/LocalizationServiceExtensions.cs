using CsvLibrary;
using Gum.Localization;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Extension methods for LocalizationManager that handle CSV file loading.
/// This keeps CSV-specific dependencies separate from the core LocalizationManager.
/// </summary>
public static class LocalizationServiceExtensions
{
    /// <summary>
    /// Loads a localization database from a CSV file.
    /// </summary>
    /// <param name="manager">The LocalizationManager instance.</param>
    /// <param name="fileName">Path to the CSV file.</param>
    /// <param name="delimiter">The delimiter character used in the CSV file.</param>
    public static void AddDatabaseFromCsv(this ILocalizationService manager, string fileName, char delimiter)
    {
        RuntimeCsvRepresentation rcr;

        Dictionary<string, string[]> entryDictionary = new Dictionary<string, string[]>();

        CsvFileManager.CsvDeserializeDictionary<string, string[]>(
            fileName,
            entryDictionary,
            // FRB supports multiple lines of text per single string ID. We don't support this in Gum (yet?), so just use the first:
            DuplicateDictionaryEntryBehavior.PreserveFirst,
            out rcr);

        // Remove comment lines (lines starting with //)
        var keys = entryDictionary.Keys.ToArray();
        foreach (var key in keys)
        {
            if (key?.Trim().StartsWith("//") == true)
            {
                entryDictionary.Remove(key);
            }
        }

        List<string> headerList = new List<string>();

        foreach (CsvHeader header in rcr.Headers)
        {
            headerList.Add(header.Name);
        }

        manager.AddDatabase(entryDictionary, headerList);
    }
}
