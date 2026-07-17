using Gum.Commands;
using Gum.Plugins;
using Gum.Plugins.AlignmentButtons;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace GumToolUnitTests.Plugins.InternalPlugins.AlignmentButtons;

public class CommonControlLogicTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager;
    private readonly CommonControlLogic _commonControlLogic;

    public CommonControlLogicTests()
    {
        _mocker = new();

        // WireframeCommands is a concrete class (not an interface) with a non-virtual Refresh(),
        // so Moq can't intercept calls on an auto-generated mock of it. Register a real
        // WireframeCommands wired to a mocked IWireframeObjectManager instead, so RefreshAndSave's
        // call to Refresh() is still observable through the interface it delegates to.
        WireframeCommands wireframeCommands = new(
            _mocker.Get<IWireframeObjectManager>(),
            new PluginManager());
        _mocker.Use(wireframeCommands);

        _commonControlLogic = _mocker.CreateInstance<CommonControlLogic>();
        _wireframeObjectManager = _mocker.GetMock<IWireframeObjectManager>();
    }

    [Fact]
    public void RefreshAndSave_CallsAllThreeInjectedCommands()
    {
        _commonControlLogic.RefreshAndSave();

        _mocker.GetMock<IGuiCommands>().Verify(x => x.RefreshVariables(true), Times.Once);
        _wireframeObjectManager.Verify(x => x.RefreshAll(true, false), Times.Once);
        _mocker.GetMock<IFileCommands>().Verify(x => x.TryAutoSaveCurrentElement(), Times.Once);
    }
}
