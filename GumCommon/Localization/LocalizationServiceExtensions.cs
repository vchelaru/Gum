using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
