using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolCommands;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ProjectCommandsTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly ProjectCommands _sut;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IStandardElementsManagerGumTool> _standardElementsManagerGumTool;
    private readonly GumProjectSave _gumProject;

    public ProjectCommandsTests()
    {
        _mocker = new AutoMocker();

        _projectManager = _mocker.GetMock<IProjectManager>();
        _standardElementsManagerGumTool = _mocker.GetMock<IStandardElementsManagerGumTool>();

        _gumProject = new GumProjectSave();
        _projectManager.Setup(m => m.GumProjectSave).Returns(_gumProject);

        _sut = _mocker.CreateInstance<ProjectCommands>();
    }

    [Fact]
    public void AddScreen_ShouldFixCustomTypeConvertersThroughInjectedInterface()
    {
        ScreenSave screenSave = new() { Name = "MyScreen" };

        _sut.AddScreen(screenSave);

        _standardElementsManagerGumTool.Verify(x => x.FixCustomTypeConverters(screenSave), Times.Once);
    }

    [Fact]
    public void AddScreen_ShouldAddScreenToProject()
    {
        ScreenSave screenSave = new() { Name = "MyScreen" };

        _sut.AddScreen(screenSave);

        _gumProject.Screens.ShouldContain(screenSave);
    }
}
