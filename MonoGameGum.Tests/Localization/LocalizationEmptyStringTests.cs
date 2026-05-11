using Gum.Localization;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Localization;

/// <summary>
/// Tests for empty/null string ID handling in <see cref="LocalizationService"/>.
/// Regression coverage for issue #2685: an empty Text value picked up content
/// from blank-ID CSV continuation rows because (a) blank-ID rows were not
/// filtered when loading and (b) <c>TranslateForLanguage</c> performed the
/// dictionary lookup before short-circuiting on empty input.
/// </summary>
public class LocalizationEmptyStringTests
{
    private readonly LocalizationService _service;

    public LocalizationEmptyStringTests()
    {
        _service = new LocalizationService();
    }

    [Fact]
    public void Translate_ShouldReturnEmpty_WhenStringIdIsEmpty_EvenIfDatabaseHasEmptyKey()
    {
        // Layer 1 contract: TranslateForLanguage must short-circuit on empty input
        // so that no dictionary lookup can leak content from a blank-ID entry.
        // We seed the database directly with an empty key whose translations have
        // real content, simulating the post-load state if the loader's filter
        // ever regressed again.
        Dictionary<string, string[]> entryDictionary = new Dictionary<string, string[]>
        {
            { "T_Hello", new[] { "T_Hello", "Hello", "Hola" } },
            { "", new[] { "", "Leaked English", "Leaked Spanish" } }
        };
        List<string> headers = new List<string> { "English", "Spanish" };

        _service.AddDatabase(entryDictionary, headers);

        _service.CurrentLanguage = 1;
        _service.Translate("").ShouldBe("");

        _service.CurrentLanguage = 2;
        _service.Translate("").ShouldBe("");
    }

    [Fact]
    public void AddCsvDatabase_ShouldSkipBlankIdRows()
    {
        // Layer 2 contract: the CSV loader must not store rows whose first
        // column is empty/whitespace. Mirrors the real-world dialog-continuation
        // pattern from issue #2685 where translators leave the ID blank on
        // continuation rows but populate the translation columns.
        string csv =
            "StringId,English,Spanish\n" +
            "T_Hello,Hello,Hola\n" +
            ",Continuation English,Continuation Spanish\n" +
            "   ,Whitespace English,Whitespace Spanish\n";
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _service.AddCsvDatabase(stream);

        // Blank-ID rows must not be stored.
        List<string> keys = _service.Keys.ToList();
        keys.ShouldNotContain("");
        keys.ShouldNotContain("   ");

        // Translating empty input still returns empty (belt-and-suspenders with layer 1).
        _service.CurrentLanguage = 1;
        _service.Translate("").ShouldBe("");

        // Real keys still translate normally.
        _service.Translate("T_Hello").ShouldBe("Hello");
        _service.CurrentLanguage = 2;
        _service.Translate("T_Hello").ShouldBe("Hola");
    }

    [Fact]
    public void Translate_ShouldReturnTranslation_ForPopulatedKey()
    {
        // Regression guard: the layer 1 short-circuit must not affect the
        // happy path for non-empty keys.
        string csv = "StringId,English,Spanish\nT_Hello,Hello,Hola\n";
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _service.AddCsvDatabase(stream);

        _service.CurrentLanguage = 1;
        _service.Translate("T_Hello").ShouldBe("Hello");

        _service.CurrentLanguage = 2;
        _service.Translate("T_Hello").ShouldBe("Hola");
    }

    [Fact]
    public void Translate_ShouldReturnNullStringSentinel_WhenStringIdIsNull()
    {
        // Pin existing behavior: null input is distinct from empty input and
        // continues to return the "NULL STRING" sentinel.
        string csv = "StringId,English\nT_Hello,Hello\n";
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _service.AddCsvDatabase(stream);

        _service.CurrentLanguage = 1;
        _service.Translate(null!).ShouldBe("NULL STRING");
    }
}
