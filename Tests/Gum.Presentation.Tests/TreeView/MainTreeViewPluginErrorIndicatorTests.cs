using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;

namespace Gum.Presentation.Tests.TreeView;

/// <summary>
/// The tree's "!" indicator re-checks an element whenever a variable is set. That check reads the
/// disk, so it must only run for a committed value - not on every tick of a drag (issue #4946).
/// </summary>
public class MainTreeViewPluginErrorIndicatorTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly Mock<IErrorChecker> _errorChecker;
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly MainTreeViewPlugin _plugin;

    public MainTreeViewPluginErrorIndicatorTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        _screen = new ScreenSave { Name = "MyScreen" };
        _project.Screens.Add(_screen);

        _errorChecker = _mocker.GetMock<IErrorChecker>();
        _errorChecker.Setup(c => c.GetErrorsFor(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()))
            .Returns(new ErrorViewModel[0]);
        _mocker.GetMock<IProjectState>().Setup(p => p.GumProjectSave).Returns(_project);

        _mocker.Use(_mocker.CreateInstance<ElementTreeViewManager>());
        _plugin = _mocker.CreateInstance<MainTreeViewPlugin>();
        _plugin.StartUp();
    }

    [Fact]
    public void VariableSet_IntermediateCommit_DoesNotCheckForErrors()
    {
        _plugin.CallVariableSet(_screen, null, "Red", 0, isFullCommit: false);

        _errorChecker.Verify(
            c => c.GetErrorsFor(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()),
            Times.Never);
    }

    [Fact]
    public void VariableSet_FullCommit_ChecksForErrors()
    {
        _plugin.CallVariableSet(_screen, null, "Red", 0, isFullCommit: true);

        _errorChecker.Verify(c => c.GetErrorsFor(_screen, _project), Times.Once);
    }
}
