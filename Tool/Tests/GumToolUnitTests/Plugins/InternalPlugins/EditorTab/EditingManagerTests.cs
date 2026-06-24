using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

public class EditingManagerTests : BaseTestClass
{
    // EditingManager now takes ISelectedState, ICircularReferenceManager, and IFavoriteComponentManager
    // via its constructor (drained from Locator), alongside the five dependencies it already injected.
    // RefreshPositionsAndScalesForInstance forwards only to the injected IWireframeObjectManager; this
    // pins that forwarding and that the three drained dependencies stay untouched on that path.
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<ICircularReferenceManager> _circularReferenceManager = new();
    private readonly Mock<IFavoriteComponentManager> _favoriteComponentManager = new();
    private readonly EditingManager _editingManager;

    public EditingManagerTests()
    {
        _editingManager = new EditingManager(
            _wireframeObjectManager.Object,
            Mock.Of<IReorderLogic>(),
            Mock.Of<IElementCommands>(),
            Mock.Of<INameVerifier>(),
            Mock.Of<ISetVariableLogic>(),
            _selectedState.Object,
            _circularReferenceManager.Object,
            _favoriteComponentManager.Object);
    }

    [Fact]
    public void RefreshPositionsAndScalesForInstance_forwards_to_wireframe_object_manager()
    {
        InstanceSave instance = new();
        List<ElementWithState> elementStack = new();

        _editingManager.RefreshPositionsAndScalesForInstance(instance, elementStack);

        _wireframeObjectManager.Verify(m => m.GetRepresentation(instance, elementStack), Times.Once);
        _selectedState.VerifyNoOtherCalls();
        _circularReferenceManager.VerifyNoOtherCalls();
        _favoriteComponentManager.VerifyNoOtherCalls();
    }
}
