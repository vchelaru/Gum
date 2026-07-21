using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.IO;

namespace GumToolUnitTests.Commands;

/// <summary>
/// Covers only the FileCommands behavior that requires the real, concrete CsvLocalizationLoader
/// (which depends on CsvLibrary and can't be referenced from the headless
/// Gum.Presentation.Tests project). Every other FileCommands test lives in
/// Tests/Gum.Presentation.Tests/FileCommandsTests.cs, alongside the class itself.
/// </summary>
public class FileCommandsTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly FileCommands _fileCommands;
    private readonly Mock<IProjectState> _projectState;
    private readonly LocalizationService _localizationService;
    private readonly GumProjectSave _gumProject;
    private string? _tempDirectory;

    public FileCommandsTests()
    {
        _mocker = new AutoMocker();

        _localizationService = new LocalizationService();
        _mocker.Use<ILocalizationService>(_localizationService);

        // Real (not mocked) CsvLocalizationLoader - this class depends on CsvLibrary, which can't
        // be referenced from the headless Gum.Presentation.Tests project, so this is the one test
        // that stays here rather than moving with the rest of FileCommandsTests.
        _mocker.Use<ICsvLocalizationLoader>(new CsvLocalizationLoader());

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
    }

    private static string CreateTempDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(),
            "GumFileCommandsTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
