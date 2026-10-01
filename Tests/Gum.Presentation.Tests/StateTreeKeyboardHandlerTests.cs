using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The states tree's hotkeys, extracted from the WPF view's code-behind so both heads share them.
/// </summary>
public class StateTreeKeyboardHandlerTests
{
    private readonly Mock<IStateTreeViewRightClickService> _rightClickService;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<ICopyPasteLogic> _copyPasteLogic;
    private readonly Mock<IHotkeyManager> _hotkeyManager;
    private readonly CopiedData _copiedData;
    private readonly StateTreeKeyboardHandler _sut;

    public StateTreeKeyboardHandlerTests()
    {
        _rightClickService = new Mock<IStateTreeViewRightClickService>();
        _selectedState = new Mock<ISelectedState>();
        _copyPasteLogic = new Mock<ICopyPasteLogic>();
        _copiedData = new CopiedData();
        _copyPasteLogic.Setup(x => x.CopiedData).Returns(_copiedData);

        // The tool's default bindings.
        _hotkeyManager = new Mock<IHotkeyManager>();
        _hotkeyManager.Setup(x => x.ReorderUp).Returns(KeyCombination.Alt(GumKey.Up));
        _hotkeyManager.Setup(x => x.ReorderDown).Returns(KeyCombination.Alt(GumKey.Down));
        _hotkeyManager.Setup(x => x.Rename).Returns(KeyCombination.Pressed(GumKey.F2));
        _hotkeyManager.Setup(x => x.Delete).Returns(KeyCombination.Pressed(GumKey.Delete));
        _hotkeyManager.Setup(x => x.Copy).Returns(KeyCombination.Ctrl(GumKey.C));
        _hotkeyManager.Setup(x => x.Paste).Returns(KeyCombination.Ctrl(GumKey.V));

        _sut = new StateTreeKeyboardHandler(
            _rightClickService.Object,
            _hotkeyManager.Object,
            _selectedState.Object,
            _copyPasteLogic.Object);
    }

    [Fact]
    public void Delete_OnTheDefaultState_IsConsumedWithoutDeleting()
    {
        ComponentSave element = new ComponentSave { Name = "Button" };
        element.States.Add(new StateSave { Name = "Default" });
        _selectedState.Setup(x => x.SelectedElement).Returns(element);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(element.DefaultState);

        bool handled = _sut.HandleKeyDown(new GumKeyEventArgs { Key = GumKey.Delete });

        handled.ShouldBeTrue();
        _rightClickService.Verify(x => x.DeleteStateClick(), Times.Never);
    }

    [Fact]
    public void Delete_OnACategorizedState_DeletesIt()
    {
        ComponentSave element = new ComponentSave { Name = "Button" };
        element.States.Add(new StateSave { Name = "Default" });
        StateSave highlighted = new StateSave { Name = "Highlighted" };
        _selectedState.Setup(x => x.SelectedElement).Returns(element);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(highlighted);

        bool handled = _sut.HandleKeyDown(new GumKeyEventArgs { Key = GumKey.Delete });

        handled.ShouldBeTrue();
        _rightClickService.Verify(x => x.DeleteStateClick(), Times.Once);
    }

    [Fact]
    public void DeleteAlt_OnAStateAndOnACategory_DeletesThem()
    {
        // macOS binds Backspace as the alternative delete key (#5552).
        _hotkeyManager.Setup(x => x.DeleteAlt).Returns(KeyCombination.Pressed(GumKey.Back));
        _selectedState.Setup(x => x.SelectedStateSave).Returns(new StateSave { Name = "Highlighted" });

        bool stateHandled = _sut.HandleKeyDown(new GumKeyEventArgs { Key = GumKey.Back });
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(new StateSaveCategory { Name = "Visibility" });
        bool categoryHandled = _sut.HandleKeyDown(new GumKeyEventArgs { Key = GumKey.Back });

        stateHandled.ShouldBeTrue();
        categoryHandled.ShouldBeTrue();
        _rightClickService.Verify(x => x.DeleteStateClick(), Times.Once);
        _rightClickService.Verify(x => x.DeleteCategoryClick(), Times.Once);
    }

    [Fact]
    public void Delete_OnACategory_DeletesTheCategory()
    {
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(new StateSaveCategory { Name = "Visibility" });

        bool handled = _sut.HandleKeyDown(new GumKeyEventArgs { Key = GumKey.Delete });

        handled.ShouldBeTrue();
        _rightClickService.Verify(x => x.DeleteCategoryClick(), Times.Once);
    }

    [Fact]
    public void Paste_WithACopiedCategory_PastesTheCategory()
    {
        _copiedData.CopiedCategory = new StateSaveCategory { Name = "Visibility" };

        bool handled = _sut.HandleKeyDown(new GumKeyEventArgs { Key = GumKey.V, IsCtrlDown = true });

        handled.ShouldBeTrue();
        _copyPasteLogic.Verify(x => x.OnPaste(CopyType.Category, It.IsAny<TopOrRecursive>()), Times.Once);
    }

    [Fact]
    public void ReorderUp_MovesTheSelectedStateUp()
    {
        bool handled = _sut.HandleKeyDown(new GumKeyEventArgs { Key = GumKey.Up, IsAltDown = true });

        handled.ShouldBeTrue();
        _rightClickService.Verify(x => x.MoveStateInDirection(-1), Times.Once);
    }

    [Fact]
    public void AnUnboundKey_IsNotConsumed()
    {
        bool handled = _sut.HandleKeyDown(new GumKeyEventArgs { Key = GumKey.C });

        handled.ShouldBeFalse();
    }
}
