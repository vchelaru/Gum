using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.Errors;
using Gum.Services;
using Gum.ToolStates;
using Moq;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.Errors;

/// <summary>
/// The Errors tab re-checks the selected element whenever a variable is set. The check walks the
/// element's file references and hits the disk, so it must only run for a committed value - not on
/// every tick of a drag (issue #4946).
/// </summary>
public class MainErrorsPluginTests : BaseTestClass
{
    private readonly Mock<IErrorChecker> _errorChecker = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly MainErrorsPlugin _plugin;

    public MainErrorsPluginTests()
    {
        _project = new GumProjectSave();
        _screen = new ScreenSave { Name = "MyScreen" };
        _project.Screens.Add(_screen);
        _projectState.Setup(p => p.GumProjectSave).Returns(_project);
        _errorChecker.Setup(c => c.GetErrorsFor(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()))
            .Returns(new ErrorViewModel[0]);

        Mock<ITabManager> tabManager = new();
        tabManager.Setup(t => t.AddControl(It.IsAny<object>(), "Errors", It.IsAny<TabLocation>()))
            .Returns(Mock.Of<IPluginTab>());

        _plugin = new MainErrorsPlugin(
            _errorChecker.Object,
            Mock.Of<IMessenger>(),
            Mock.Of<ISelectedState>(),
            Mock.Of<IClipboardService>(),
            Mock.Of<IFileSystemRevealService>(),
            _projectState.Object)
        {
            TabManager = tabManager.Object,
        };
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
