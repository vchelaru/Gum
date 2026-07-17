using Gum.Commands;
using Gum.Plugins.AlignmentButtons;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class CommonControlLogicTests
{
    private readonly Mock<IWireframeCommands> _wireframeCommands;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly CommonControlLogic _commonControlLogic;

    public CommonControlLogicTests()
    {
        _wireframeCommands = new Mock<IWireframeCommands>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();

        // CommonControlLogic depends on IWireframeCommands (not the concrete WireframeCommands),
        // so it can be mocked directly instead of standing up a real WireframeCommands wired to a
        // mocked IWireframeObjectManager (issue #3754).
        _commonControlLogic = new CommonControlLogic(
            Mock.Of<ISelectedState>(),
            _wireframeCommands.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            Mock.Of<ISetVariableLogic>());
    }

    [Fact]
    public void RefreshAndSave_CallsAllThreeInjectedCommands()
    {
        _commonControlLogic.RefreshAndSave();

        _guiCommands.Verify(x => x.RefreshVariables(true), Times.Once);
        _wireframeCommands.Verify(x => x.Refresh(true, false), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveCurrentElement(), Times.Once);
    }
}
