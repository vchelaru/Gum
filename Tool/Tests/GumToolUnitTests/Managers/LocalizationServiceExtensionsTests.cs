using System.IO;
using Gum.Localization;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Managers;

/// <summary>
/// Regression coverage for the tool's CSV localization loader (distinct from GumCommon's
/// AddCsvDatabase, which the runtime uses). Two bugs found investigating a live crash:
/// 1. Languages included the "String ID" header column itself, off-by-one-ing every language
///    index against the per-ID arrays (which correctly start translations at index 1) - selecting
///    the last of N languages threw IndexOutOfRangeException in LocalizationService.
/// 2. The delimiter parameter was silently ignored (CsvFileManager.CsvDeserializeDictionary always
///    parsed with ',' internally), so a non-comma-delimited file passed through this method
///    couldn't work no matter what the caller specified.
/// </summary>
public class LocalizationServiceExtensionsTests
{
    [Fact]
    public void AddDatabaseFromCsv_LanguagesShouldNotIncludeTheStringIdColumn()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "String ID,English,Spanish\nT_Cancel,Cancel,Cancelar\n");

            LocalizationService service = new();
            service.AddDatabaseFromCsv(path, ',');

            service.Languages.ShouldBe(new[] { "English", "Spanish" });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AddDatabaseFromCsv_SelectingTheLastLanguage_ShouldTranslateCorrectly_NotThrow()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "String ID,English,Spanish\nT_Cancel,Cancel,Cancelar\n");

            LocalizationService service = new();
            service.AddDatabaseFromCsv(path, ',');

            // Mirrors ProjectPropertiesChangeLogic's `Languages.IndexOf(name) + 1`: selecting the
            // last language ("Spanish", index 1) resolves to CurrentLanguage 2.
            service.CurrentLanguage = service.Languages.Count;
            service.Translate("T_Cancel").ShouldBe("Cancelar");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AddDatabaseFromCsv_ShouldRespectTheGivenDelimiter()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "String ID;English;Spanish\nT_Cancel;Cancel;Cancelar\n");

            LocalizationService service = new();
            service.AddDatabaseFromCsv(path, ';');

            service.Languages.ShouldBe(new[] { "English", "Spanish" });
            service.CurrentLanguage = 2;
            service.Translate("T_Cancel").ShouldBe("Cancelar");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
