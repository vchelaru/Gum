using Gum.Managers;
using Gum.Plugins.ScrollBarPlugin;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

public class ScrollbarServiceTests : BaseTestClass
{
    // ScrollbarService now takes ISelectedState + IWireframeObjectManager via its constructor (drained
    // from Locator), alongside the IProjectManager it already injected. This pins that the constructor
    // only stores its dependencies -- it service-locates nothing and touches none of them, which is the
    // behavior the drain must preserve. (Its event handlers all dereference a ScrollBarControlLogic that
    // only exists once a WinForms wireframe is initialized, so they are not exercisable headless.)
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly ScrollbarService _scrollbarService;

    public ScrollbarServiceTests()
    {
        _scrollbarService = new ScrollbarService(
            _selectedState.Object,
            _wireframeObjectManager.Object,
            _projectManager.Object);
    }

    [Fact]
    public void Constructor_stores_dependencies_without_invoking_them()
    {
        _scrollbarService.ShouldNotBeNull();

        _selectedState.VerifyNoOtherCalls();
        _wireframeObjectManager.VerifyNoOtherCalls();
        _projectManager.VerifyNoOtherCalls();
    }
}
