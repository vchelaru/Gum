using CsvLibrary;
using Gum.Localization;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Extension methods for LocalizationService.
/// </summary>
public static class LocalizationServiceExtensions
{
    /// <summary>
    /// Loads a localization database from a CSV file.
    /// </summary>
    /// <param name="service">The ILocalizationService instance.</param>
    /// <param name="fileName">Path to the CSV file.</param>
    /// <param name="delimiter">The delimiter character used in the CSV file.</param>
    public static void AddDatabaseFromCsv(this ILocalizationService service, string fileName, char delimiter)
    {
        Dictionary<string, string[]> entryDictionary = new Dictionary<string, string[]>();

        // CsvFileManager.CsvDeserializeDictionary hardcodes ',' internally (via the parameterless
        // CsvDeserializeToRuntime(fileName) overload), silently ignoring any other delimiter passed
        // to this method - call the delimiter-aware overload directly instead.
        RuntimeCsvRepresentation rcr = CsvFileManager.CsvDeserializeToRuntime(fileName, delimiter);
        rcr.FillObjectDictionary(
            entryDictionary,
            // FRB supports multiple lines of text per single string ID. We don't support this in Gum (yet?), so just use the first:
            duplicateDictionaryEntryBehavior: DuplicateDictionaryEntryBehavior.PreserveFirst);

        // Remove comment lines (lines starting with //)
        var keys = entryDictionary.Keys.ToArray();
        foreach (var key in keys)
        {
            if (key?.Trim().StartsWith("//") == true)
            {
                entryDictionary.Remove(key);
            }
        }

        // The first column is the string ID itself, not a language - ILocalizationService.Languages
        // must list only translation languages so its index lines up with a per-ID array's index
        // (array[0] is the ID, array[i+1] is Languages[i]'s translation). Skip it here to match
        // GumCommon's AddCsvDatabase, which does the same via HeaderRecord.Skip(1). Including it was
        // an off-by-one: e.g. selecting the last of 2 languages resolved to array index 3 on a
        // length-3 array and threw IndexOutOfRangeException.
        List<string> headerList = rcr.Headers.Skip(1).Select(header => header.Name).ToList();

        service.AddDatabase(entryDictionary, headerList);
    }
}
