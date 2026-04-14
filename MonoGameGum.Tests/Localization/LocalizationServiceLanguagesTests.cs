using Gum.Localization;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Localization;

/// <summary>
/// Tests for ILocalizationService.Languages — verifies the interface contract
/// so that any implementation is guaranteed to expose language names.
/// </summary>
public class LocalizationServiceLanguagesTests
{
    private readonly ILocalizationService _service;

    public LocalizationServiceLanguagesTests()
    {
        _service = new LocalizationService();
    }

    [Fact]
    public void Languages_ShouldBeEmpty_BeforeAnyLoad()
    {
        _service.Languages.Count.ShouldBe(0);
    }

    [Fact]
    public void Languages_ShouldBeEmpty_AfterClear()
    {
        var csv = "StringId,English,Spanish\nT_OK,OK,OK\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        _service.AddCsvDatabase(stream);

        _service.Clear();

        _service.Languages.Count.ShouldBe(0);
    }

    [Fact]
    public void Languages_ShouldContainColumnHeaders_AfterCsvLoad()
    {
        var csv = "StringId,English,Spanish,French\nT_OK,OK,OK,OK\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _service.AddCsvDatabase(stream);

        _service.Languages.Count.ShouldBe(3);
        _service.Languages[0].ShouldBe("English");
        _service.Languages[1].ShouldBe("Spanish");
        _service.Languages[2].ShouldBe("French");
    }

    [Fact]
    public void Languages_ShouldContainLanguageNames_AfterResxLoad()
    {
        var englishResx = BuildResx(new() { { "T_OK", "OK" } });
        var spanishResx = BuildResx(new() { { "T_OK", "OK" } });

        var streams = new List<(string, Stream)>
        {
            ("English", new MemoryStream(Encoding.UTF8.GetBytes(englishResx))),
            ("Spanish", new MemoryStream(Encoding.UTF8.GetBytes(spanishResx))),
        };

        try
        {
            _service.AddResxDatabase(streams);

            _service.Languages.Count.ShouldBe(2);
            _service.Languages[0].ShouldBe("English");
            _service.Languages[1].ShouldBe("Spanish");
        }
        finally
        {
            foreach (var (_, s) in streams) s.Dispose();
        }
    }

    private static string BuildResx(Dictionary<string, string> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<root>");
        sb.AppendLine("  <resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader>");
        sb.AppendLine("  <resheader name=\"version\"><value>2.0</value></resheader>");
        foreach (var entry in entries)
        {
            sb.AppendLine($"  <data name=\"{entry.Key}\" xml:space=\"preserve\">");
            sb.AppendLine($"    <value>{entry.Value}</value>");
            sb.AppendLine("  </data>");
        }
        sb.AppendLine("</root>");
        return sb.ToString();
    }
}
