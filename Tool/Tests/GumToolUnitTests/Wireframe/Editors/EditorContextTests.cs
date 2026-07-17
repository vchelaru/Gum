using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Gum.Wireframe.Editors;
using Moq;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Numerics;

namespace GumToolUnitTests.Wireframe.Editors;

/// <summary>
/// Pinning tests for EditorContext.DoEndOfSettingValuesLogic(): the PluginManager notification
/// used to be a self-located Locator.GetRequiredService&lt;IPluginManager&gt;() call, which made this
/// path untestable (no test registers a Locator provider). It is now constructor-injected.
/// </summary>
public class EditorContextTests : BaseTestClass
{
    [Fact]
    public void DoEndOfSettingValuesLogic_ShouldNotifyInjectedPluginManager_WhenVariableListChanged()
    {
        // Arrange
        ScreenSave selectedElement = new ScreenSave();

        StateSave stateSave = new StateSave();
        VariableListSave<Vector2> pointsVariableList = new VariableListSave<Vector2> { Name = "Points" };
        stateSave.VariableLists.Add(pointsVariableList);

        Mock<ISelectedState> mockSelectedState = new Mock<ISelectedState>();
        mockSelectedState.Setup(x => x.SelectedElement).Returns(selectedElement);
        mockSelectedState.Setup(x => x.SelectedStateSave).Returns(stateSave);
        mockSelectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave>());

        Mock<IWireframeObjectManager> mockWireframeObjectManager = new Mock<IWireframeObjectManager>();
        mockWireframeObjectManager.Setup(x => x.GetRepresentation(selectedElement)).Returns(new GraphicalUiElement());

        Mock<IPluginManager> mockPluginManager = new Mock<IPluginManager>();

        SelectionManager selectionManager = new SelectionManager(
            Mock.Of<ISelectedState>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IEditingManager>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IHotkeyManager>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IProjectManager>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IElementCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<IUiSettingsService>(),
            Mock.Of<IToolFontService>(),
            Mock.Of<IPluginManager>());

        EditorContext context = new EditorContext(
            mockSelectedState.Object,
            selectionManager,
            Mock.Of<IElementCommands>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            Mock.Of<IHotkeyManager>(),
            mockWireframeObjectManager.Object,
            Mock.Of<IUiSettingsService>(),
            new Layer(),
            System.Drawing.Color.White,
            System.Drawing.Color.White,
            Mock.Of<IToolFontService>(),
            mockPluginManager.Object);

        // Snapshot the "before drag" state so DoEndOfSettingValuesLogic has something to diff against.
        context.GrabbedState.HandlePush();

        // Act
        context.DoEndOfSettingValuesLogic();

        // Assert
        mockPluginManager.Verify(
            x => x.VariableSet(selectedElement, null, "Points", It.IsAny<object>()),
            Times.Once);
    }
}
