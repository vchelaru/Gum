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

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldIncludeAllPriorSourcesInWarning_WhenThreeFilesCollide()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "A.resx"), new Dictionary<string, string>
        {
            { "T_Shared", "FromA" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "B.resx"), new Dictionary<string, string>
        {
            { "T_Shared", "FromB" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "C.resx"), new Dictionary<string, string>
        {
            { "T_Shared", "FromC" }
        });

        List<string> basePaths = new List<string>
        {
            Path.Combine(_tempDirectory, "A.resx"),
            Path.Combine(_tempDirectory, "B.resx"),
            Path.Combine(_tempDirectory, "C.resx")
        };

        List<string> warnings = new List<string>();
        _service.AddResxDatabase(basePaths, onWarning: w => warnings.Add(w));

        // Two collisions: B over A, then C over {A, B}
        warnings.Count.ShouldBe(2);
        warnings[1].ShouldContain("A.resx");
        warnings[1].ShouldContain("B.resx");
        warnings[1].ShouldContain("C.resx");

        _service.CurrentLanguage = 1;
        _service.Translate("T_Shared").ShouldBe("FromC");
    }

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldInvokeOnWarning_OnKeyCollision()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_Shared", "FromStrings" }
        });

        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.resx"), new Dictionary<string, string>
        {
            { "T_Shared", "FromButtons" }
        });

        List<string> basePaths = new List<string>
        {
            Path.Combine(_tempDirectory, "Strings.resx"),
            Path.Combine(_tempDirectory, "Buttons.resx")
        };

        List<string> warnings = new List<string>();
        _service.AddResxDatabase(basePaths, onWarning: w => warnings.Add(w));

        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("T_Shared");
        warnings[0].ShouldContain("Strings.resx");
        warnings[0].ShouldContain("Buttons.resx");

        // Last-write-wins: Buttons came after Strings
        _service.CurrentLanguage = 1;
        _service.Translate("T_Shared").ShouldBe("FromButtons");
    }

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldMergeKeysAcrossFiles()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"), new Dictionary<string, string>
        {
            { "T_OK", "Aceptar" }
        });

        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.resx"), new Dictionary<string, string>
        {
            { "B_Save", "Save" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.es.resx"), new Dictionary<string, string>
        {
            { "B_Save", "Guardar" }
        });

        List<string> basePaths = new List<string>
        {
            Path.Combine(_tempDirectory, "Strings.resx"),
            Path.Combine(_tempDirectory, "Buttons.resx")
        };

        _service.AddResxDatabase(basePaths);

        _service.HasDatabase.ShouldBeTrue();
        _service.Languages.Count.ShouldBe(2);
        _service.Languages[0].ShouldBe("Default");
        _service.Languages[1].ShouldBe("es");

        _service.CurrentLanguage = 1;
        _service.Translate("T_OK").ShouldBe("OK");
        _service.Translate("B_Save").ShouldBe("Save");

        _service.CurrentLanguage = 2;
        _service.Translate("T_OK").ShouldBe("Aceptar");
        _service.Translate("B_Save").ShouldBe("Guardar");
    }

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldNotInvokeWarning_OnDuplicateKeyWithinSingleFileGroup()
    {
        _tempDirectory = CreateTempDirectory();

        // Same key in base and satellite -- NOT a cross-group collision, so onWarning
        // should not fire. Last-write-wins silently on the per-language map.
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK_Base" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK_Spanish" }
        });

        List<string> basePaths = new List<string>
        {
            Path.Combine(_tempDirectory, "Strings.resx")
        };

        List<string> warnings = new List<string>();
        _service.AddResxDatabase(basePaths, onWarning: w => warnings.Add(w));

        warnings.Count.ShouldBe(0);

        _service.CurrentLanguage = 1;
        _service.Translate("T_OK").ShouldBe("OK_Base");
        _service.CurrentLanguage = 2;
        _service.Translate("T_OK").ShouldBe("OK_Spanish");
    }

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldNotThrow_WhenOnWarningIsNullAndCollisionOccurs()
    {
        _tempDirectory = CreateTempDirectory();

        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_Shared", "FromStrings" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.resx"), new Dictionary<string, string>
        {
            { "T_Shared", "FromButtons" }
        });

        List<string> basePaths = new List<string>
        {
            Path.Combine(_tempDirectory, "Strings.resx"),
            Path.Combine(_tempDirectory, "Buttons.resx")
        };

        Should.NotThrow(() => _service.AddResxDatabase(basePaths));

        _service.CurrentLanguage = 1;
        _service.Translate("T_Shared").ShouldBe("FromButtons");
    }

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldNotThrow_WhenPathListIsEmpty()
    {
        List<string> basePaths = new List<string>();

        Should.NotThrow(() => _service.AddResxDatabase(basePaths));
    }

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldRetrieveSatelliteOnlyKey_WithBaseFallback()
    {
        _tempDirectory = CreateTempDirectory();

        // Base has no T_SatelliteOnly; satellite does.
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"), new Dictionary<string, string>
        {
            { "T_SatelliteOnly", "SoloEs" }
        });

        List<string> basePaths = new List<string>
        {
            Path.Combine(_tempDirectory, "Strings.resx")
        };

        _service.AddResxDatabase(basePaths);

        // Satellite language: key is present
        _service.CurrentLanguage = 2;
        _service.Translate("T_SatelliteOnly").ShouldBe("SoloEs");

        // Default language: missing -> fall back to string ID
        _service.CurrentLanguage = 1;
        _service.Translate("T_SatelliteOnly").ShouldBe("T_SatelliteOnly");
    }

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldThrow_WhenBaseFileIsMissing()
    {
        _tempDirectory = CreateTempDirectory();
        var missingPath = Path.Combine(_tempDirectory, "DoesNotExist.resx");

        List<string> basePaths = new List<string> { missingPath };

        var ex = Should.Throw<FileNotFoundException>(() => _service.AddResxDatabase(basePaths));
        ex.Message.ShouldContain("DoesNotExist.resx");
    }

    [Fact]
    public void AddResxDatabase_MultiPath_ShouldUnionLanguages_WhenSatellitesDiffer()
    {
        _tempDirectory = CreateTempDirectory();

        // Strings has es satellite only
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"), new Dictionary<string, string>
        {
            { "T_OK", "OK" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"), new Dictionary<string, string>
        {
            { "T_OK", "Aceptar" }
        });

        // Buttons has fr satellite only
        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.resx"), new Dictionary<string, string>
        {
            { "B_Save", "Save" }
        });
        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.fr.resx"), new Dictionary<string, string>
        {
            { "B_Save", "Enregistrer" }
        });

        List<string> basePaths = new List<string>
        {
            Path.Combine(_tempDirectory, "Strings.resx"),
            Path.Combine(_tempDirectory, "Buttons.resx")
        };

        _service.AddResxDatabase(basePaths);

        _service.Languages.Count.ShouldBe(3);
        _service.Languages[0].ShouldBe("Default");
        _service.Languages[1].ShouldBe("es");
        _service.Languages[2].ShouldBe("fr");

        // In "es" column, B_Save has no Spanish satellite -> fall back to string ID
        _service.CurrentLanguage = 2;
        _service.Translate("T_OK").ShouldBe("Aceptar");
        _service.Translate("B_Save").ShouldBe("B_Save");

        // In "fr" column, T_OK has no French satellite -> fall back to string ID
        _service.CurrentLanguage = 3;
        _service.Translate("T_OK").ShouldBe("T_OK");
        _service.Translate("B_Save").ShouldBe("Enregistrer");
    }

    [Fact]
    public void AddResxDatabase_MultiStreamGroups_ShouldInvokeOnWarning_OnKeyCollision()
    {
        string stringsEn = CreateResxContent(new Dictionary<string, string> { { "T_Shared", "FromStrings" } });
        string buttonsEn = CreateResxContent(new Dictionary<string, string> { { "T_Shared", "FromButtons" } });

        List<(string languageName, Stream stream)> stringsGroup = new List<(string, Stream)>
        {
            ("Default", new MemoryStream(Encoding.UTF8.GetBytes(stringsEn)))
        };
        List<(string languageName, Stream stream)> buttonsGroup = new List<(string, Stream)>
        {
            ("Default", new MemoryStream(Encoding.UTF8.GetBytes(buttonsEn)))
        };

        List<IEnumerable<(string languageName, Stream stream)>> groups = new List<IEnumerable<(string, Stream)>>
        {
            stringsGroup,
            buttonsGroup
        };

        List<string> warnings = new List<string>();

        try
        {
            _service.AddResxDatabase(groups, onWarning: w => warnings.Add(w));

            warnings.Count.ShouldBe(1);
            warnings[0].ShouldContain("T_Shared");

            _service.CurrentLanguage = 1;
            _service.Translate("T_Shared").ShouldBe("FromButtons");
        }
        finally
        {
            foreach ((string _, Stream stream) in stringsGroup)
            {
                stream.Dispose();
            }
            foreach ((string _, Stream stream) in buttonsGroup)
            {
                stream.Dispose();
            }
        }
    }

    [Fact]
    public void AddResxDatabase_MultiStreamGroups_ShouldMergeKeysAcrossGroups()
    {
        string stringsEn = CreateResxContent(new Dictionary<string, string> { { "T_OK", "OK" } });
        string stringsEs = CreateResxContent(new Dictionary<string, string> { { "T_OK", "Aceptar" } });
        string buttonsEn = CreateResxContent(new Dictionary<string, string> { { "B_Save", "Save" } });
        string buttonsEs = CreateResxContent(new Dictionary<string, string> { { "B_Save", "Guardar" } });

        List<(string languageName, Stream stream)> stringsGroup = new List<(string, Stream)>
        {
            ("Default", new MemoryStream(Encoding.UTF8.GetBytes(stringsEn))),
            ("es", new MemoryStream(Encoding.UTF8.GetBytes(stringsEs)))
        };
        List<(string languageName, Stream stream)> buttonsGroup = new List<(string, Stream)>
        {
            ("Default", new MemoryStream(Encoding.UTF8.GetBytes(buttonsEn))),
            ("es", new MemoryStream(Encoding.UTF8.GetBytes(buttonsEs)))
        };

        List<IEnumerable<(string languageName, Stream stream)>> groups = new List<IEnumerable<(string, Stream)>>
        {
            stringsGroup,
            buttonsGroup
        };

        try
        {
            _service.AddResxDatabase(groups);

            _service.Languages.Count.ShouldBe(2);
            _service.Languages[0].ShouldBe("Default");
            _service.Languages[1].ShouldBe("es");

            _service.CurrentLanguage = 1;
            _service.Translate("T_OK").ShouldBe("OK");
            _service.Translate("B_Save").ShouldBe("Save");

            _service.CurrentLanguage = 2;
            _service.Translate("T_OK").ShouldBe("Aceptar");
            _service.Translate("B_Save").ShouldBe("Guardar");
        }
        finally
        {
            foreach ((string _, Stream stream) in stringsGroup)
            {
                stream.Dispose();
            }
            foreach ((string _, Stream stream) in buttonsGroup)
            {
                stream.Dispose();
            }
        }
    }

    [Fact]
    public void AddResxDatabase_NamedStreamGroups_ShouldUseGroupName_InCollisionWarning()
    {
        string aEn = CreateResxContent(new Dictionary<string, string> { { "T_Shared", "FromA" } });
        string bEn = CreateResxContent(new Dictionary<string, string> { { "T_Shared", "FromB" } });

        List<(string languageName, Stream stream)> groupA = new List<(string, Stream)>
        {
            ("Default", new MemoryStream(Encoding.UTF8.GetBytes(aEn)))
        };
        List<(string languageName, Stream stream)> groupB = new List<(string, Stream)>
        {
            ("Default", new MemoryStream(Encoding.UTF8.GetBytes(bEn)))
        };

        List<(string? groupName, IEnumerable<(string languageName, Stream stream)> streams)> named
            = new List<(string?, IEnumerable<(string, Stream)>)>
        {
            ("CustomGroupA", groupA),
            ("CustomGroupB", groupB)
        };

        List<string> warnings = new List<string>();

        try
        {
            _service.AddResxDatabase(named, onWarning: w => warnings.Add(w));

            warnings.Count.ShouldBe(1);
            warnings[0].ShouldContain("CustomGroupA");
            warnings[0].ShouldContain("CustomGroupB");
        }
        finally
        {
            foreach ((string _, Stream stream) in groupA)
            {
                stream.Dispose();
            }
            foreach ((string _, Stream stream) in groupB)
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
