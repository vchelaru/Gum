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
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// Test-only concrete subclass — WireframeEditor is abstract and StandardWireframeEditor/
/// PolygonWireframeEditor (its real subclasses) stay tool-side because they construct
/// XNALIKE-only visual objects directly (see WireframeEditor's relocation notes, part of #3846).
/// </summary>
internal class TestWireframeEditor : WireframeEditor
{
    public TestWireframeEditor(
        IHotkeyManager hotkeyManager,
        ISelectionManager selectionManager,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUndoManager undoManager,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        IUiSettingsService uiSettingsService,
        Layer layer,
        System.Drawing.Color lineColor,
        System.Drawing.Color textColor,
        Camera camera,
        IGumCursorState cursor,
        IPluginManager pluginManager)
        : base(hotkeyManager, selectionManager, selectedState, elementCommands, guiCommands,
              fileCommands, setVariableLogic, undoManager, variableInCategoryPropagationLogic,
              wireframeObjectManager, uiSettingsService, layer, lineColor, textColor, camera,
              cursor, pluginManager)
    {
    }

    public override bool HasCursorOverHandles => false;

    public void AddHandler(IInputHandler handler) => _inputHandlers.Add(handler);
}

/// <summary>
/// Pins WireframeEditor.GetCursorToShow's priority-ordering: it's the exact logic that used to
/// live in GetWindowsCursorToShow (returning a WinForms Cursor) before the relocation to headless
/// Gum.Presentation (part of #3846) converted it to the neutral GumCursorKind.
/// </summary>
public class WireframeEditorCursorTests
{
    private static TestWireframeEditor CreateSut(Mock<ISelectedState>? selectedState = null)
    {
        return new TestWireframeEditor(
            Mock.Of<IHotkeyManager>(),
            Mock.Of<ISelectionManager>(),
            (selectedState ?? new Mock<ISelectedState>()).Object,
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
            Mock.Of<IGumCursorState>(),
            Mock.Of<IPluginManager>());
    }

    private static Mock<IInputHandler> CreateHandlerMock(int priority, GumCursorKind? cursorToShow)
    {
        var handler = new Mock<IInputHandler>();
        handler.SetupGet(h => h.Priority).Returns(priority);
        handler.Setup(h => h.GetCursorToShow(It.IsAny<float>(), It.IsAny<float>())).Returns(cursorToShow);
        return handler;
    }

    [Fact]
    public void GetCursorToShow_ShouldReturnHighestPriorityHandlersCursor_WhenMultipleHandlersHaveAnOpinion()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddHandler(CreateHandlerMock(priority: 50, GumCursorKind.Hand).Object);
        sut.AddHandler(CreateHandlerMock(priority: 100, GumCursorKind.Cross).Object);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBe(GumCursorKind.Cross);
    }

    [Fact]
    public void GetCursorToShow_ShouldFallThroughToLowerPriorityHandler_WhenHigherPriorityHandlerHasNoOpinion()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddHandler(CreateHandlerMock(priority: 50, GumCursorKind.SizeAll).Object);
        sut.AddHandler(CreateHandlerMock(priority: 100, cursorToShow: null).Object);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBe(GumCursorKind.SizeAll);
    }

    [Fact]
    public void GetCursorToShow_ShouldReturnNull_WhenNoHandlerHasAnOpinion()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddHandler(CreateHandlerMock(priority: 100, cursorToShow: null).Object);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBeNull();
    }

    [Fact]
    public void GetCursorToShow_ShouldReturnNull_WhenSelectionIsLocked()
    {
        // Arrange
        var lockedInstance = new InstanceSave { Locked = true };
        var selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(s => s.SelectedInstance).Returns(lockedInstance);
        var sut = CreateSut(selectedState);
        // Even a handler with a strong opinion must be ignored while locked.
        sut.AddHandler(CreateHandlerMock(priority: 100, GumCursorKind.SizeAll).Object);

        // Act
        var cursor = sut.GetCursorToShow(0f, 0f);

        // Assert
        cursor.ShouldBeNull();
    }
}
