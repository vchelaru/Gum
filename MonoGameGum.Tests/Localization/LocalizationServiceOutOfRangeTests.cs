using Gum.Localization;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Localization;

/// <summary>
/// Regression coverage for a live crash: a malformed database (a per-ID translations array
/// shorter than the requested language index) threw IndexOutOfRangeException instead of
/// degrading gracefully. Surfaced by a real project where the tool's CSV loader built
/// Languages one entry too long (including the "String ID" column itself), so selecting the
/// last language resolved to an index one past the end of every per-ID array.
/// </summary>
public class LocalizationServiceOutOfRangeTests
{
    private readonly LocalizationService _service;

    public LocalizationServiceOutOfRangeTests()
    {
        _service = new LocalizationService();
    }

    [Fact]
    public void TranslateForLanguage_ShouldReturnStringId_WhenLanguageIndexIsBeyondTheIdsTranslationArray()
    {
        Dictionary<string, string[]> entryDictionary = new()
        {
            { "T_Cancel", new[] { "T_Cancel", "Cancel", "Cancelar" } }
        };
        List<string> headers = new() { "English", "Spanish" };
        _service.AddDatabase(entryDictionary, headers);

        _service.TranslateForLanguage("T_Cancel", 3).ShouldBe("T_Cancel");
    }

    [Fact]
    public void TranslateForLanguage_ShouldReturnStringId_WhenLanguageIndexIsNegative()
    {
        Dictionary<string, string[]> entryDictionary = new()
        {
            { "T_Cancel", new[] { "T_Cancel", "Cancel", "Cancelar" } }
        };
        List<string> headers = new() { "English", "Spanish" };
        _service.AddDatabase(entryDictionary, headers);

        _service.TranslateForLanguage("T_Cancel", -1).ShouldBe("T_Cancel");
    }

    [Fact]
    public void TranslateForLanguage_ShouldStillReturnTranslation_ForAnInRangeLanguageIndex()
    {
        Dictionary<string, string[]> entryDictionary = new()
        {
            { "T_Cancel", new[] { "T_Cancel", "Cancel", "Cancelar" } }
        };
        List<string> headers = new() { "English", "Spanish" };
        _service.AddDatabase(entryDictionary, headers);

        _service.TranslateForLanguage("T_Cancel", 2).ShouldBe("Cancelar");
    }
}
