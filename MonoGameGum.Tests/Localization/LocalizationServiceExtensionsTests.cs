using Gum.Localization;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Localization;

public class LocalizationServiceExtensionsTests : IDisposable
{
    private readonly LocalizationService _service;
    private string? _tempDirectory;

    public LocalizationServiceExtensionsTests()
    {
        _service = new LocalizationService();
    }

    public void Dispose()
    {
        if (_tempDirectory != null && Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region AddCsvDatabase

    [Fact]
    public void AddCsvDatabase_ShouldLoadStrings_FromStream()
    {
        var csv = "StringId,English,Spanish\nT_OK,OK,OK\nT_Cancel,Cancel,Cancelar\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _service.AddCsvDatabase(stream);

        _service.HasDatabase.ShouldBeTrue();
        _service.CurrentLanguage = 1;
        _service.Translate("T_OK").ShouldBe("OK");
        _service.Translate("T_Cancel").ShouldBe("Cancel");
    }

    [Fact]
    public void AddCsvDatabase_ShouldTranslateToSecondLanguage()
    {
        var csv = "StringId,English,Spanish\nT_OK,OK,OK\nT_Cancel,Cancel,Cancelar\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _service.AddCsvDatabase(stream);
        _service.CurrentLanguage = 2;

        _service.Translate("T_OK").ShouldBe("OK");
        _service.Translate("T_Cancel").ShouldBe("Cancelar");
    }

    [Fact]
    public void AddCsvDatabase_ShouldSetLanguageHeaders()
    {
        var csv = "StringId,English,Spanish\nT_OK,OK,OK\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _service.AddCsvDatabase(stream);

        _service.Languages.Count.ShouldBe(2);
        _service.Languages[0].ShouldBe("English");
        _service.Languages[1].ShouldBe("Spanish");
    }

    [Fact]
    public void AddCsvDatabase_ShouldHandleMultipleLanguages()
    {
        var csv = "StringId,English,Spanish,French\nT_Hello,Hello,Hola,Bonjour\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        _service.AddCsvDatabase(stream);

        _service.CurrentLanguage = 1;
        _service.Translate("T_Hello").ShouldBe("Hello");

        _service.CurrentLanguage = 2;
        _service.Translate("T_Hello").ShouldBe("Hola");

        _service.CurrentLanguage = 3;
        _service.Translate("T_Hello").ShouldBe("Bonjour");
    }

    #endregion

    #region AddResxDatabase (Streams)

    [Fact]
    public void AddResxDatabase_Streams_ShouldLoadStrings()
    {
        var englishResx = CreateResxContent(new Dictionary<string, string>
        {
            { "T_OK", "OK" },
            { "T_Cancel", "Cancel" }
        });

        var spanishResx = CreateResxContent(new Dictionary<string, string>
        {
            { "T_OK", "OK" },
            { "T_Cancel", "Cancelar" }
        });

        var streams = new List<(string languageName, Stream stream)>
        {
            ("English", new MemoryStream(Encoding.UTF8.GetBytes(englishResx))),
            ("Spanish", new MemoryStream(Encoding.UTF8.GetBytes(spanishResx)))
        };

        try
        {
            _service.AddResxDatabase(streams);

            _service.HasDatabase.ShouldBeTrue();
            _service.CurrentLanguage = 1;
            _service.Translate("T_OK").ShouldBe("OK");
            _service.Translate("T_Cancel").ShouldBe("Cancel");

            _service.CurrentLanguage = 2;
            _service.Translate("T_Cancel").ShouldBe("Cancelar");
        }
        finally
        {
            foreach (var (_, stream) in streams)
            {
                stream.Dispose();
            }
        }
    }

    [Fact]
    public void AddResxDatabase_Streams_ShouldSetLanguageHeaders()
    {
        var englishResx = CreateResxContent(new Dictionary<string, string> { { "T_OK", "OK" } });
        var spanishResx = CreateResxContent(new Dictionary<string, string> { { "T_OK", "OK" } });

        var streams = new List<(string languageName, Stream stream)>
        {
            ("English", new MemoryStream(Encoding.UTF8.GetBytes(englishResx))),
            ("Spanish", new MemoryStream(Encoding.UTF8.GetBytes(spanishResx)))
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
            foreach (var (_, stream) in streams)
            {
                stream.Dispose();
            }
        }
    }

    [Fact]
    public void AddResxDatabase_Streams_ShouldFallBackToStringId_WhenKeyMissingInLanguage()
    {
        var englishResx = CreateResxContent(new Dictionary<string, string>
        {
            { "T_OK", "OK" },
            { "T_Cancel", "Cancel" }
        });

        // Spanish is missing T_Cancel
        var spanishResx = CreateResxContent(new Dictionary<string, string>
        {
            { "T_OK", "OK" }
        });

        var streams = new List<(string languageName, Stream stream)>
        {
            ("English", new MemoryStream(Encoding.UTF8.GetBytes(englishResx))),
            ("Spanish", new MemoryStream(Encoding.UTF8.GetBytes(spanishResx)))
        };

        try
        {
            _service.AddResxDatabase(streams);

            _service.CurrentLanguage = 2;
            _service.Translate("T_Cancel").ShouldBe("T_Cancel",
                "Missing keys should fall back to the string ID");
        }
        finally
        {
            foreach (var (_, stream) in streams)
            {
                stream.Dispose();
            }
        }
    }

    [Fact]
    public void AddResxDatabase_Streams_ShouldHandleThreeLanguages()
    {
        var englishResx = CreateResxContent(new Dictionary<string, string> { { "T_Hello", "Hello" } });
        var spanishResx = CreateResxContent(new Dictionary<string, string> { { "T_Hello", "Hola" } });
        var frenchResx = CreateResxContent(new Dictionary<string, string> { { "T_Hello", "Bonjour" } });

        var streams = new List<(string languageName, Stream stream)>
        {
            ("English", new MemoryStream(Encoding.UTF8.GetBytes(englishResx))),
            ("Spanish", new MemoryStream(Encoding.UTF8.GetBytes(spanishResx))),
            ("French", new MemoryStream(Encoding.UTF8.GetBytes(frenchResx)))
        };

        try
        {
            _service.AddResxDatabase(streams);

            _service.CurrentLanguage = 1;
            _service.Translate("T_Hello").ShouldBe("Hello");

            _service.CurrentLanguage = 2;
            _service.Translate("T_Hello").ShouldBe("Hola");

            _service.CurrentLanguage = 3;
            _service.Translate("T_Hello").ShouldBe("Bonjour");
        }
        finally
        {
            foreach (var (_, stream) in streams)
            {
                stream.Dispose();
            }
        }
    }

    #endregion

    #region AddResxDatabase (File Path)

    [Fact]
    public void AddResxDatabase_FilePath_ShouldDiscoverSatelliteFiles()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK" },
            { "T_Cancel", "Cancel" }
        });

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK" },
            { "T_Cancel", "Cancelar" }
        });

        _service.AddResxDatabase(Path.Combine(_tempDirectory, "Strings.resx"));

        _service.HasDatabase.ShouldBeTrue();
        _service.Languages.Count.ShouldBe(2);
    }

    [Fact]
    public void AddResxDatabase_FilePath_ShouldTranslateToSatelliteLanguage()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_Cancel", "Cancel" }
        });

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"), new Dictionary<string, string>
        {
            { "T_Cancel", "Cancelar" }
        });

        _service.AddResxDatabase(Path.Combine(_tempDirectory, "Strings.resx"));

        // Language 0 is the string ID column, 1 is base (Strings), 2 is satellite (es)
        _service.CurrentLanguage = 2;
        _service.Translate("T_Cancel").ShouldBe("Cancelar");
    }

    [Fact]
    public void AddResxDatabase_FilePath_ShouldWorkWithMultipleSatellites()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_Hello", "Hello" }
        });

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"), new Dictionary<string, string>
        {
            { "T_Hello", "Hola" }
        });

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.fr.resx"), new Dictionary<string, string>
        {
            { "T_Hello", "Bonjour" }
        });

        _service.AddResxDatabase(Path.Combine(_tempDirectory, "Strings.resx"));

        _service.Languages.Count.ShouldBe(3);

        _service.CurrentLanguage = 1;
        _service.Translate("T_Hello").ShouldBe("Hello");

        _service.CurrentLanguage = 2;
        _service.Translate("T_Hello").ShouldBe("Hola");

        _service.CurrentLanguage = 3;
        _service.Translate("T_Hello").ShouldBe("Bonjour");
    }

    [Fact]
    public void AddResxDatabase_FilePath_ShouldWorkWithOnlyBaseFile()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK" }
        });

        _service.AddResxDatabase(Path.Combine(_tempDirectory, "Strings.resx"));

        _service.HasDatabase.ShouldBeTrue();
        _service.Languages.Count.ShouldBe(1);
        _service.CurrentLanguage = 1;
        _service.Translate("T_OK").ShouldBe("OK");
    }

    #endregion

    #region Helpers

    private static string CreateResxContent(Dictionary<string, string> entries)
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

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GumLocalizationTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void WriteResxFile(string filePath, Dictionary<string, string> entries)
    {
        File.WriteAllText(filePath, CreateResxContent(entries));
    }

    #endregion
}
