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
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// A push with Shift held also reaches the marquee, which records it as pending. When a handler
/// claims that push (an axis-locked move, #5258, or a Shift resize or rotation), its release must
/// clear the pending push, or a later drag that never pushed on the canvas starts a marquee from it.
/// Also pins that the marquee's minimum drag uses the canvas display scale.
/// </summary>
public class SelectionManagerShiftHandlerPushTests : BaseTestClass
{
    private readonly Mock<IGumCursorState> _cursor = new();
    private readonly Mock<IInputHandler> _handler = new();
    private readonly Mock<ISelectionRectangleVisual> _rectangleVisual = new();
    private readonly CanvasDisplayScale _displayScale = new();
    private readonly SelectionManager _selectionManager;

    public SelectionManagerShiftHandlerPushTests()
    {
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave>());
        selectedState.Setup(x => x.SelectedElement).Returns(new ComponentSave { Name = "Component" });
        selectedState.Setup(x => x.GetTopLevelElementStack()).Returns(new List<ElementWithState>());

        var hotkeyManager = new Mock<IHotkeyManager>();
        KeyCombination shift = KeyCombination.Shift();
        hotkeyManager.SetupGet(h => h.MultiSelect).Returns(shift);
        hotkeyManager.Setup(h => h.IsPressedInControl(shift)).Returns(true);

        var factory = new Mock<IWireframeEditorFactory>();
        factory
            .Setup(f => f.CreateStandardEditor(It.IsAny<ISelectionManager>(), It.IsAny<Layer>(), It.IsAny<Camera>(), It.IsAny<IGumCursorState>()))
            .Returns<ISelectionManager, Layer, Camera, IGumCursorState>((sm, _, _, cursor) =>
            {
                var editor = new TestWireframeEditor(
                    hotkeyManager.Object, sm, selectedState.Object, Mock.Of<IElementCommands>(),
                    Mock.Of<IGuiCommands>(), Mock.Of<IFileCommands>(), Mock.Of<ISetVariableLogic>(),
                    Mock.Of<IUndoManager>(), Mock.Of<IVariableInCategoryPropagationLogic>(),
                    Mock.Of<IWireframeObjectManager>(), Mock.Of<IUiSettingsService>(), new Layer(),
                    System.Drawing.Color.White, System.Drawing.Color.White, new Camera(), cursor,
                    Mock.Of<IPluginManager>(),
                    new Gum.Services.CanvasDisplayScale());
                editor.AddHandler(_handler.Object);
                return editor;
            });

        _selectionManager = new SelectionManager(
            selectedState.Object,
            Mock.Of<IUndoManager>(),
            Mock.Of<IContextMenuState>(),
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            hotkeyManager.Object,
            Mock.Of<IWireframeObjectManager>(m => m.AllIpsos == new List<GraphicalUiElement>()),
            Mock.Of<IGuiCommands>(),
            factory.Object,
            Mock.Of<INineSliceCoordinateRefresher>(),
            Mock.Of<IPreciseHitTester>());

        _selectionManager.Initialize(
            new Layer(),
            new Camera(),
            _cursor.Object,
            _rectangleVisual.Object,
            Mock.Of<IHighlightOutlineVisual>(),
            Mock.Of<IHighlightOverlayVisual>(),
            _displayScale);

        _selectionManager.SelectedGue = new GraphicalUiElement { Tag = new InstanceSave { Name = "Instance" } };
    }

    private void SetCursor(float x, bool push, bool down, bool click)
    {
        _cursor.SetupGet(c => c.X).Returns(x);
        _cursor.SetupGet(c => c.Y).Returns(0);
        _cursor.SetupGet(c => c.IsInWindow).Returns(true);
        _cursor.SetupGet(c => c.PrimaryPush).Returns(push);
        _cursor.SetupGet(c => c.PrimaryDown).Returns(down);
        _cursor.SetupGet(c => c.PrimaryDownIgnoringIsInWindow).Returns(down);
        _cursor.SetupGet(c => c.PrimaryClick).Returns(click);
    }

    [Fact]
    public void AHandlersRelease_ClearsTheMarqueesPendingShiftPush()
    {
        bool isHandlerActive = false;
        _handler.SetupGet(h => h.IsActive).Returns(() => isHandlerActive);
        _handler.Setup(h => h.HandlePush(It.IsAny<float>(), It.IsAny<float>()))
            .Returns(() => isHandlerActive = true);
        _handler.Setup(h => h.HandleRelease()).Callback(() => isHandlerActive = false);

        SetCursor(x: 0, push: true, down: true, click: false);
        _selectionManager.Activity(forceNoHighlight: false);
        SetCursor(x: 0, push: false, down: false, click: true);
        _selectionManager.Activity(forceNoHighlight: false);

        // A later held-button drag that never pushed on the canvas.
        SetCursor(x: 200, push: false, down: true, click: false);
        _selectionManager.Activity(forceNoHighlight: false);
        _selectionManager.LateActivity();

        _rectangleVisual.VerifySet(v => v.Visible = true, Times.Never);
    }

    // The marquee's minimum drag is 3 device-independent pixels, so 6 physical pixels at 200% (#5554).
    [Theory]
    [InlineData(5f, false)]
    [InlineData(6f, true)]
    public void MarqueeMinimumDrag_UsesTheCanvasDisplayScale(float dragDistance, bool expected)
    {
        _displayScale.DisplayScale = 2;

        SetCursor(x: 0, push: true, down: true, click: false);
        _selectionManager.Activity(forceNoHighlight: false);
        SetCursor(x: dragDistance, push: false, down: true, click: false);
        _selectionManager.Activity(forceNoHighlight: false);
        _selectionManager.LateActivity();

        _rectangleVisual.VerifySet(v => v.Visible = true, expected ? Times.AtLeastOnce() : Times.Never());
    }
}
