using Gum.Commands;
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
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// A handle drag whose release the editor never sees - the wireframe canvas skips selection
/// activity while the camera pans (issue #4791), and PrimaryClick needs the cursor on the canvas -
/// must release its handler when input next runs with the button up. Otherwise the handler stays
/// active: it suppresses marquee selection and keeps dragging on the next unrelated left-drag.
/// </summary>
public class WireframeEditorLostReleaseTests
{
    private readonly Mock<IGumCursorState> _cursor = new();
    private readonly Mock<IInputHandler> _handler = new();
    private readonly TestWireframeEditor _sut;

    public WireframeEditorLostReleaseTests()
    {
        _sut = new TestWireframeEditor(
            Mock.Of<IHotkeyManager>(),
            Mock.Of<ISelectionManager>(),
            Mock.Of<ISelectedState>(),
            Mock.Of<IElementCommands>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IUiSettingsService>(),
            new Layer(),
            System.Drawing.Color.White,
            System.Drawing.Color.White,
            new Camera(),
            _cursor.Object,
            Mock.Of<IPluginManager>(),
            new Gum.Services.CanvasDisplayScale());

        bool isActive = false;
        _handler.SetupGet(h => h.IsActive).Returns(() => isActive);
        _handler.Setup(h => h.HandlePush(It.IsAny<float>(), It.IsAny<float>()))
            .Returns(true)
            .Callback(() => isActive = true);
        _handler.Setup(h => h.HandleRelease()).Callback(() => isActive = false);
        _sut.AddHandler(_handler.Object);
    }

    private void SetCursor(bool push, bool down, bool click, bool downIgnoringIsInWindow)
    {
        _cursor.SetupGet(c => c.PrimaryPush).Returns(push);
        _cursor.SetupGet(c => c.PrimaryDown).Returns(down);
        _cursor.SetupGet(c => c.PrimaryClick).Returns(click);
        _cursor.SetupGet(c => c.PrimaryDownIgnoringIsInWindow).Returns(downIgnoringIsInWindow);
    }

    [Fact]
    public void ProcessHandleInput_ShouldReleaseTheActiveHandler_WhenTheButtonIsUpWithNoClickEdge()
    {
        SetCursor(push: true, down: true, click: false, downIgnoringIsInWindow: true);
        _sut.ProcessHandleInput(_cursor.Object, 0f, 0f);
        _sut.IsAnyHandlerActive.ShouldBeTrue();

        // The release edge passed on a frame this editor never ran (camera pan / off-canvas).
        SetCursor(push: false, down: false, click: false, downIgnoringIsInWindow: false);
        _sut.ProcessHandleInput(_cursor.Object, 0f, 0f);

        _handler.Verify(h => h.HandleRelease(), Times.Once);
        _sut.IsAnyHandlerActive.ShouldBeFalse();
    }

    [Fact]
    public void ProcessHandleInput_ShouldNotReleaseTheActiveHandler_WhileTheButtonIsStillHeldOffCanvas()
    {
        SetCursor(push: true, down: true, click: false, downIgnoringIsInWindow: true);
        _sut.ProcessHandleInput(_cursor.Object, 0f, 0f);

        // Dragged off the canvas with the button held: PrimaryDown is false but the drag is alive.
        SetCursor(push: false, down: false, click: false, downIgnoringIsInWindow: true);
        _sut.ProcessHandleInput(_cursor.Object, 0f, 0f);

        _handler.Verify(h => h.HandleRelease(), Times.Never);
        _sut.IsAnyHandlerActive.ShouldBeTrue();
    }
}
