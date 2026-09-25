using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Gum.Wireframe.Editors;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Generic;
using System.Numerics;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pinning tests for EditorContext.DoEndOfSettingValuesLogic(): the PluginManager notification
/// used to be a self-located Locator.GetRequiredService&lt;IPluginManager&gt;() call, which made this
/// path untestable (no test registers a Locator provider). It is now constructor-injected.
/// </summary>
public class EditorContextTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoEndOfSettingValuesLogic_ShouldNotifyPluginManager_OnlyWhenVariableListChanged(bool movePoint)
    {
        // Arrange
        ScreenSave selectedElement = new ScreenSave();

        StateSave stateSave = new StateSave();
        VariableListSave<Vector2> pointsVariableList = new VariableListSave<Vector2> { Name = "Points" };
        pointsVariableList.Value.Add(new Vector2(0, 0));
        stateSave.VariableLists.Add(pointsVariableList);

        Mock<ISelectedState> mockSelectedState = new Mock<ISelectedState>();
        mockSelectedState.Setup(x => x.SelectedElement).Returns(selectedElement);
        mockSelectedState.Setup(x => x.SelectedStateSave).Returns(stateSave);
        mockSelectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave>());

        Mock<IWireframeObjectManager> mockWireframeObjectManager = new Mock<IWireframeObjectManager>();
        mockWireframeObjectManager.Setup(x => x.GetRepresentation(selectedElement)).Returns(new GraphicalUiElement());

        Mock<IPluginManager> mockPluginManager = new Mock<IPluginManager>();

        EditorContext context = new EditorContext(
            mockSelectedState.Object,
            Mock.Of<ISelectionManager>(),
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
            new Camera(),
            Mock.Of<IGumCursorState>(),
            mockPluginManager.Object,
            new CanvasDisplayScale());

        // Snapshot the "before drag" state so DoEndOfSettingValuesLogic has something to diff against.
        context.GrabbedState.HandlePush();
        if (movePoint)
        {
            pointsVariableList.Value[0] = new Vector2(5, 5);
        }

        // Act
        context.DoEndOfSettingValuesLogic();

        // Assert
        mockPluginManager.Verify(
            x => x.VariableSet(selectedElement, null, "Points", It.IsAny<object>(), true),
            movePoint ? Times.Once() : Times.Never());
    }
}
