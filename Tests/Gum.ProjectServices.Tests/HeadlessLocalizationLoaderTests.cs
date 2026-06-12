using Gum.DataTypes;
using Gum.Localization;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gum.ProjectServices.Tests;

public class HeadlessLocalizationLoaderTests : IDisposable
{
    private readonly HeadlessLocalizationLoader _sut;
    private readonly TestLogger _logger;
    private readonly LocalizationService _localizationService;
    private readonly string _tempDirectory;

    public HeadlessLocalizationLoaderTests()
    {
        _logger = new TestLogger();
        _sut = new HeadlessLocalizationLoader(_logger);
        _localizationService = new LocalizationService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumHeadlessLocalizationTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void LoadLocalizationFiles_MissingCsv_LogsErrorAndDoesNotLoad()
    {
        GumProjectSave project = CreateProjectWithFiles("DoesNotExist.csv");

        _sut.LoadLocalizationFiles(project, _tempDirectory, _localizationService);

        _localizationService.HasDatabase.ShouldBeFalse();
        _logger.Errors.ShouldContain(error => error.Contains("DoesNotExist.csv"));
    }

    [Fact]
    public void LoadLocalizationFiles_MixedCsvAndResx_LogsErrorAndDoesNotLoad()
    {
        WriteCsv("Strings.csv");
        WriteResx("Strings.resx");
        GumProjectSave project = CreateProjectWithFiles("Strings.csv", "Strings.resx");

        _sut.LoadLocalizationFiles(project, _tempDirectory, _localizationService);

        _localizationService.HasDatabase.ShouldBeFalse();
        _logger.Errors.ShouldContain(error => error.Contains("not all are .resx"));
    }

    [Fact]
    public void LoadLocalizationFiles_MultipleCsvs_LogsErrorAndDoesNotLoad()
    {
        WriteCsv("First.csv");
        WriteCsv("Second.csv");
        GumProjectSave project = CreateProjectWithFiles("First.csv", "Second.csv");

        _sut.LoadLocalizationFiles(project, _tempDirectory, _localizationService);

        _localizationService.HasDatabase.ShouldBeFalse();
        _logger.Errors.ShouldContain(error => error.Contains("not all are .resx"));
    }

    [Fact]
    public void LoadLocalizationFiles_MultipleResxFiles_LoadsAllKeys()
    {
        WriteResx("First.resx", ("T_First", "First"));
        WriteResx("Second.resx", ("T_Second", "Second"));
        GumProjectSave project = CreateProjectWithFiles("First.resx", "Second.resx");

        _sut.LoadLocalizationFiles(project, _tempDirectory, _localizationService);

        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.Keys.ShouldContain("T_First");
        _localizationService.Keys.ShouldContain("T_Second");
    }

    [Fact]
    public void LoadLocalizationFiles_NoFiles_DoesNothing()
    {
        GumProjectSave project = new GumProjectSave();

        _sut.LoadLocalizationFiles(project, _tempDirectory, _localizationService);

        _localizationService.HasDatabase.ShouldBeFalse();
        _logger.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void LoadLocalizationFiles_SingleCsv_LoadsDatabase()
    {
        WriteCsv("LocalizationDB.csv");
        GumProjectSave project = CreateProjectWithFiles("LocalizationDB.csv");

        _sut.LoadLocalizationFiles(project, _tempDirectory, _localizationService);

        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.Keys.ShouldContain("T_OK");
        _localizationService.Languages.ShouldBe(new[] { "English", "Spanish" });
    }

    [Fact]
    public void LoadLocalizationFiles_SingleCsv_SetsCurrentLanguageFromProject()
    {
        WriteCsv("LocalizationDB.csv");
        GumProjectSave project = CreateProjectWithFiles("LocalizationDB.csv");
        project.CurrentLanguageIndex = 1;

        _sut.LoadLocalizationFiles(project, _tempDirectory, _localizationService);

        _localizationService.CurrentLanguage.ShouldBe(1);
    }

    [Fact]
    public void LoadLocalizationFiles_SingleResx_LoadsDatabase()
    {
        WriteResx("Strings.resx", ("T_Hello", "Hello"));
        GumProjectSave project = CreateProjectWithFiles("Strings.resx");

        _sut.LoadLocalizationFiles(project, _tempDirectory, _localizationService);

        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.Keys.ShouldContain("T_Hello");
    }

    private GumProjectSave CreateProjectWithFiles(params string[] relativeFiles)
    {
        GumProjectSave project = new GumProjectSave();
        foreach (string relativeFile in relativeFiles)
        {
            project.LocalizationFiles.Add(relativeFile);
        }
        return project;
    }

    private void WriteCsv(string relativeFile)
    {
        File.WriteAllText(Path.Combine(_tempDirectory, relativeFile),
            "String ID,English,Spanish\nT_OK,OK,De acuerdo\n");
    }

    private void WriteResx(string relativeFile, params (string key, string value)[] entries)
    {
        if (entries.Length == 0)
        {
            entries = new[] { ("T_OK", "OK") };
        }

        string dataElements = "";
        foreach ((string key, string value) in entries)
        {
            dataElements += $"  <data name=\"{key}\" xml:space=\"preserve\"><value>{value}</value></data>\n";
        }

        File.WriteAllText(Path.Combine(_tempDirectory, relativeFile),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n" + dataElements + "</root>");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private class TestLogger : ICodeGenLogger
    {
        public List<string> Outputs { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();

        public void PrintOutput(string message) => Outputs.Add(message);
        public void PrintError(string message) => Errors.Add(message);
    }
}
