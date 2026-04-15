using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GumToolUnitTests.Commands;

public class FileCommandsTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly FileCommands _fileCommands;
    private readonly Mock<IProjectState> _projectState;
    private readonly Mock<IOutputManager> _outputManager;
    private readonly LocalizationService _localizationService;
    private readonly GumProjectSave _gumProject;
    private readonly List<string> _outputCalls;
    private readonly List<string> _errorCalls;
    private string? _tempDirectory;

    public FileCommandsTests()
    {
        _mocker = new AutoMocker();

        _localizationService = new LocalizationService();
        _mocker.Use<ILocalizationService>(_localizationService);

        _outputCalls = new List<string>();
        _errorCalls = new List<string>();
        _outputManager = _mocker.GetMock<IOutputManager>();
        _outputManager.Setup(o => o.AddOutput(It.IsAny<string>()))
            .Callback<string>(s => _outputCalls.Add(s));
        _outputManager.Setup(o => o.AddError(It.IsAny<string>()))
            .Callback<string>(s => _errorCalls.Add(s));

        _gumProject = new GumProjectSave();
        _projectState = _mocker.GetMock<IProjectState>();
        _projectState.Setup(p => p.GumProjectSave).Returns(_gumProject);
        _projectState.Setup(p => p.ProjectDirectory).Returns(() =>
            _tempDirectory == null ? null : _tempDirectory + Path.DirectorySeparatorChar);

        _fileCommands = _mocker.CreateInstance<FileCommands>();
    }

    public override void Dispose()
    {
        if (_tempDirectory != null && Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        base.Dispose();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldDoNothing_WhenLocalizationFilesIsEmpty()
    {
        bool raised = false;
        _fileCommands.LocalizationLoaded += () => raised = true;

        _fileCommands.LoadLocalizationFile();

        _outputCalls.Count.ShouldBe(0);
        _errorCalls.Count.ShouldBe(0);
        _localizationService.HasDatabase.ShouldBeFalse();
        raised.ShouldBeTrue();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldLoadMultipleResx_WhenMultipleResxEntries()
    {
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.resx"),
            new Dictionary<string, string> { { "B_Save", "Save" } });
        _gumProject.LocalizationFiles.Add("Strings.resx");
        _gumProject.LocalizationFiles.Add("Buttons.resx");

        _fileCommands.LoadLocalizationFile();

        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.CurrentLanguage = 1;
        _localizationService.Translate("T_OK").ShouldBe("OK");
        _localizationService.Translate("B_Save").ShouldBe("Save");
        _errorCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void LoadLocalizationFile_ShouldLoadSingleCsv_WhenOneCsvEntry()
    {
        _tempDirectory = CreateTempDirectory();
        string csvPath = Path.Combine(_tempDirectory, "Strings.csv");
        File.WriteAllText(csvPath, "StringId,English,Spanish\nT_OK,OK,Aceptar\n");
        _gumProject.LocalizationFiles.Add("Strings.csv");

        _fileCommands.LoadLocalizationFile();

        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.CurrentLanguage = 1;
        _localizationService.Translate("T_OK").ShouldBe("OK");
        _localizationService.CurrentLanguage = 2;
        _localizationService.Translate("T_OK").ShouldBe("Aceptar");
        _errorCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void LoadLocalizationFile_ShouldLoadSingleResx_WhenOneResxEntry()
    {
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.es.resx"),
            new Dictionary<string, string> { { "T_OK", "Aceptar" } });
        _gumProject.LocalizationFiles.Add("Strings.resx");

        _fileCommands.LoadLocalizationFile();

        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.Languages.Count.ShouldBe(2);
        _localizationService.CurrentLanguage = 2;
        _localizationService.Translate("T_OK").ShouldBe("Aceptar");
        _errorCalls.Count.ShouldBe(0);
    }

    [Fact]
    public void LoadLocalizationFile_ShouldReportError_WhenMixingCsvAndResx()
    {
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        File.WriteAllText(Path.Combine(_tempDirectory, "Strings.csv"),
            "StringId,English\nT_OK,OK\n");
        _gumProject.LocalizationFiles.Add("Strings.resx");
        _gumProject.LocalizationFiles.Add("Strings.csv");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _errorCalls[0].ShouldContain("not all are .resx");
        _localizationService.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldReportError_WhenMultipleCsvEntries()
    {
        _tempDirectory = CreateTempDirectory();
        File.WriteAllText(Path.Combine(_tempDirectory, "A.csv"), "StringId,English\nT_OK,OK\n");
        File.WriteAllText(Path.Combine(_tempDirectory, "B.csv"), "StringId,English\nT_Cancel,Cancel\n");
        _gumProject.LocalizationFiles.Add("A.csv");
        _gumProject.LocalizationFiles.Add("B.csv");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _localizationService.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldRouteCollisionToOutputTab_WhenMultipleResxFilesDefineSameKey()
    {
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_Shared", "FromStrings" } });
        WriteResxFile(Path.Combine(_tempDirectory, "Buttons.resx"),
            new Dictionary<string, string> { { "T_Shared", "FromButtons" } });
        _gumProject.LocalizationFiles.Add("Strings.resx");
        _gumProject.LocalizationFiles.Add("Buttons.resx");

        _fileCommands.LoadLocalizationFile();

        _outputCalls.Count.ShouldBe(1);
        _outputCalls[0].ShouldContain("T_Shared");
        _outputCalls[0].ShouldContain("Strings.resx");
        _outputCalls[0].ShouldContain("Buttons.resx");
    }

    [Fact]
    public void LoadLocalizationFile_ShouldReportError_WhenSingleCsvFileMissing()
    {
        _tempDirectory = CreateTempDirectory();
        _gumProject.LocalizationFiles.Add("DoesNotExist.csv");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _errorCalls[0].ShouldContain("DoesNotExist.csv");
        _localizationService.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldReportError_WhenSingleResxFileMissing()
    {
        // Single-RESX now flows through the multi-file path, so a missing file is reported
        // via AddError rather than silently no-opping (prior behavior).
        _tempDirectory = CreateTempDirectory();
        _gumProject.LocalizationFiles.Add("DoesNotExist.resx");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _errorCalls[0].ShouldContain("DoesNotExist.resx");
        _localizationService.HasDatabase.ShouldBeFalse();
    }

    [Fact]
    public void LoadLocalizationFile_ShouldWarnAndSkip_WhenBaseFileMissing()
    {
        // Production behavior: in the multi-file branch, missing files are reported via
        // AddError (not AddOutput), and remaining existing files still load.
        _tempDirectory = CreateTempDirectory();
        WriteResxFile(Path.Combine(_tempDirectory, "Strings.resx"),
            new Dictionary<string, string> { { "T_OK", "OK" } });
        _gumProject.LocalizationFiles.Add("Strings.resx");
        _gumProject.LocalizationFiles.Add("DoesNotExist.resx");

        _fileCommands.LoadLocalizationFile();

        _errorCalls.Count.ShouldBe(1);
        _errorCalls[0].ShouldContain("DoesNotExist.resx");
        _localizationService.HasDatabase.ShouldBeTrue();
        _localizationService.CurrentLanguage = 1;
        _localizationService.Translate("T_OK").ShouldBe("OK");
    }

    #region Helpers

    private static string CreateResxContent(Dictionary<string, string> entries)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<root>");
        sb.AppendLine("  <resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader>");
        sb.AppendLine("  <resheader name=\"version\"><value>2.0</value></resheader>");

        foreach (KeyValuePair<string, string> entry in entries)
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
        string tempDir = Path.Combine(Path.GetTempPath(),
            "GumFileCommandsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void WriteResxFile(string filePath, Dictionary<string, string> entries)
    {
        File.WriteAllText(filePath, CreateResxContent(entries));
    }

    #endregion
}
