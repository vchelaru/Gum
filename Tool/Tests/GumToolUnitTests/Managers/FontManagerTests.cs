using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.FontGeneration;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;

namespace GumToolUnitTests.Managers;

public class FontManagerTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly Mock<IHeadlessFontGenerationService> _headlessServiceMock;
    private readonly Mock<IFileCommands> _fileCommandsMock;
    private readonly Mock<IProjectState> _projectStateMock;
    private readonly FontManager _fontManager;

    /// <summary>
    /// The expected project directory as returned by FilePath.FullPath.
    /// FilePath normalizes separators per-platform, so we capture it once.
    /// </summary>
    private readonly string _expectedProjectDirectory;

    public FontManagerTests()
    {
        _mocker = new AutoMocker();

        FilePath projectDir = new FilePath("/test/project/");
        _expectedProjectDirectory = projectDir.FullPath;

        _fileCommandsMock = _mocker.GetMock<IFileCommands>();
        _fileCommandsMock.Setup(f => f.ProjectDirectory).Returns(projectDir);

        _projectStateMock = _mocker.GetMock<IProjectState>();
        _projectStateMock.Setup(p => p.GumProjectSave).Returns(new GumProjectSave());

        _headlessServiceMock = _mocker.GetMock<IHeadlessFontGenerationService>();

        _fontManager = _mocker.CreateInstance<FontManager>();
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldDelegateToHeadlessService()
    {
        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontName = "Arial";
        bmfcSave.FontSize = 24;

        _fontManager.CreateFontIfNecessary(bmfcSave);

        _headlessServiceMock.Verify(s => s.CreateFontIfNecessary(
            bmfcSave,
            _expectedProjectDirectory,
            false),
            Times.Once);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldDefaultAutoSizeToFalse_WhenProjectIsNull()
    {
        _projectStateMock.Setup(p => p.GumProjectSave).Returns((GumProjectSave?)null);

        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontName = "Arial";
        bmfcSave.FontSize = 18;

        _fontManager.CreateFontIfNecessary(bmfcSave);

        _headlessServiceMock.Verify(s => s.CreateFontIfNecessary(
            bmfcSave,
            _expectedProjectDirectory,
            false),
            Times.Once);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldPassAutoSizeFromProjectState()
    {
        GumProjectSave project = new GumProjectSave();
        project.AutoSizeFontOutputs = true;
        _projectStateMock.Setup(p => p.GumProjectSave).Returns(project);

        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontName = "Arial";
        bmfcSave.FontSize = 24;

        _fontManager.CreateFontIfNecessary(bmfcSave);

        _headlessServiceMock.Verify(s => s.CreateFontIfNecessary(
            bmfcSave,
            _expectedProjectDirectory,
            true),
            Times.Once);
    }

    [Fact]
    public void GenerateMissingFontsForReferencingElements_ShouldDelegateToHeadlessService()
    {
        GumProjectSave gumProject = new GumProjectSave();
        StateSave stateSave = new StateSave();

        _fontManager.GenerateMissingFontsForReferencingElements(gumProject, stateSave);

        _headlessServiceMock.Verify(s => s.GenerateMissingFontsForReferencingElements(
            gumProject,
            stateSave,
            _expectedProjectDirectory),
            Times.Once);
    }
}
