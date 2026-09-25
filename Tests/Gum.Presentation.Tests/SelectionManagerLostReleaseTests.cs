using Gum.Commands;
using Gum.DataTypes;
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
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins that an idle frame (no push, down or click) still reaches the editor's handlers, so a
/// handle drag whose release the manager never ran for (camera pan, off-canvas release; issue
/// #4791) is ended instead of staying active until the next click.
/// </summary>
public class SelectionManagerLostReleaseTests : BaseTestClass
{
    private readonly Mock<IGumCursorState> _cursor = new();
    private readonly Mock<IInputHandler> _handler = new();
    private readonly SelectionManager _selectionManager;

    public SelectionManagerLostReleaseTests()
    {
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave>());
        selectedState.Setup(x => x.SelectedElement).Returns(new ComponentSave { Name = "Component" });
        selectedState.Setup(x => x.GetTopLevelElementStack()).Returns(new List<ElementWithState>());

        var factory = new Mock<IWireframeEditorFactory>();
        factory
            .Setup(f => f.CreateStandardEditor(It.IsAny<ISelectionManager>(), It.IsAny<Layer>(), It.IsAny<Camera>(), It.IsAny<IGumCursorState>()))
            .Returns<ISelectionManager, Layer, Camera, IGumCursorState>((sm, _, _, cursor) =>
            {
                var editor = new TestWireframeEditor(
                    Mock.Of<IHotkeyManager>(), sm, selectedState.Object, Mock.Of<IElementCommands>(),
                    Mock.Of<IGuiCommands>(), Mock.Of<IFileCommands>(), Mock.Of<ISetVariableLogic>(),
                    Mock.Of<IUndoManager>(), Mock.Of<IVariableInCategoryPropagationLogic>(),
                    Mock.Of<IWireframeObjectManager>(), Mock.Of<IUiSettingsService>(), new Layer(),
                    System.Drawing.Color.White, System.Drawing.Color.White, new Camera(), cursor,
                    Mock.Of<IPluginManager>(),
            new Gum.Plugins.InternalPlugins.EditorTab.Services.CanvasDisplayScale());
                editor.AddHandler(_handler.Object);
                return editor;
            });

        _selectionManager = new SelectionManager(
            selectedState.Object,
            Mock.Of<IUndoManager>(),
            Mock.Of<IContextMenuState>(),
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            Mock.Of<IHotkeyManager>(),
            Mock.Of<IWireframeObjectManager>(m => m.AllIpsos == new List<GraphicalUiElement>()),
            Mock.Of<IGuiCommands>(),
            factory.Object,
            Mock.Of<INineSliceCoordinateRefresher>(),
            Mock.Of<IPreciseHitTester>());

        _selectionManager.Initialize(
            new Layer(),
            new Camera(),
            _cursor.Object,
            Mock.Of<ISelectionRectangleVisual>(),
            Mock.Of<IHighlightOutlineVisual>(),
            Mock.Of<IHighlightOverlayVisual>());

        _selectionManager.SelectedGue = new GraphicalUiElement { Tag = new InstanceSave { Name = "Instance" } };
    }

    [Fact]
    public void Activity_ShouldReleaseAStaleActiveHandler_OnAnIdleFrame()
    {
        _handler.SetupGet(h => h.IsActive).Returns(true);
        _cursor.SetupGet(c => c.PrimaryDownIgnoringIsInWindow).Returns(false);

        _selectionManager.Activity(forceNoHighlight: false);

        _handler.Verify(h => h.HandleRelease(), Times.Once);
    }
}
