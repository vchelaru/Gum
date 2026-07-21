using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for StateTreeRightClickViewModel, extracted out of
/// <c>StateTreeViewRightClickService.PopulateContextMenu</c> into the headless Gum.Presentation
/// assembly (ADR-0005): which right-click items are shown, their header text, and the state-move
/// logic behind "Move Up"/"Move Down".
/// </summary>
public class StateTreeRightClickViewModelTests
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<IEditCommands> _editCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<ICopyPasteLogic> _copyPasteLogic;
    private readonly StateTreeRightClickViewModel _sut;

    public StateTreeRightClickViewModelTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _elementCommands = new Mock<IElementCommands>();
        _editCommands = new Mock<IEditCommands>();
        _dialogService = new Mock<IDialogService>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _copyPasteLogic = new Mock<ICopyPasteLogic>();

        _copyPasteLogic.Setup(x => x.CopiedData).Returns(new CopiedData());

        _sut = new StateTreeRightClickViewModel(
            _selectedState.Object,
            _elementCommands.Object,
            _editCommands.Object,
            _dialogService.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _copyPasteLogic.Object);
    }

    [Fact]
    public void DeleteCategoryClick_ShouldAskToDeleteStateCategory()
    {
        StateSaveCategory category = new() { Name = "Visibility" };
        ComponentSave element = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);

        _sut.DeleteCategoryClick();

        _editCommands.Verify(x => x.AskToDeleteStateCategory(category, element), Times.Once);
    }

    [Fact]
    public void DeleteStateClick_ShouldAskToDeleteState()
    {
        StateSave state = new() { Name = "Hovered" };
        ComponentSave element = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedStateSave).Returns(state);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);

        _sut.DeleteStateClick();

        _editCommands.Verify(x => x.AskToDeleteState(state, element), Times.Once);
    }

    [Fact]
    public void GetMenuItems_ShouldExcludeAddState_WhenNoCategorySelected()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns((StateSaveCategory?)null);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        result.Any(item => item.Text == "Add State").ShouldBeFalse();
    }

    [Fact]
    public void GetMenuItems_ShouldIncludeAddCategory_Always()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        result.Any(item => item.Text == "Add Category").ShouldBeTrue();
    }

    [Fact]
    public void GetMenuItems_ShouldIncludeAddState_WhenCategorySelected()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSaveCategory category = new() { Name = "Visibility" };
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        result.Any(item => item.Text == "Add State").ShouldBeTrue();
    }

    [Fact]
    public void GetMenuItems_ShouldIncludeCategoryCommands_WhenOnlyCategorySelected()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSaveCategory category = new() { Name = "Visibility" };
        element.Categories.Add(category);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        result.Any(item => item.Text == "Rename Category").ShouldBeTrue();
        result.Any(item => item.Text == "Copy [Visibility]").ShouldBeTrue();
        result.Any(item => item.Text == "Delete [Visibility]").ShouldBeTrue();
    }

    [Fact]
    public void GetMenuItems_ShouldIncludeMoveDown_WhenNotLastInCategory()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSaveCategory category = new() { Name = "Visibility" };
        StateSave state1 = new() { Name = "State1", ParentContainer = element };
        StateSave state2 = new() { Name = "State2", ParentContainer = element };
        category.States.Add(state1);
        category.States.Add(state2);
        element.Categories.Add(category);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(state1);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        ContextMenuItemViewModel moveDown = result.First(item => item.Text == "v Move Down");
        moveDown.Shortcut.ShouldBe("Alt+Down");
        result.Any(item => item.Text == "^ Move Up").ShouldBeFalse();
    }

    [Fact]
    public void GetMenuItems_ShouldIncludeMoveUp_WhenNotFirstInCategory()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSaveCategory category = new() { Name = "Visibility" };
        StateSave state1 = new() { Name = "State1", ParentContainer = element };
        StateSave state2 = new() { Name = "State2", ParentContainer = element };
        category.States.Add(state1);
        category.States.Add(state2);
        element.Categories.Add(category);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(state2);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        ContextMenuItemViewModel moveUp = result.First(item => item.Text == "^ Move Up");
        moveUp.Shortcut.ShouldBe("Alt+Up");
        result.Any(item => item.Text == "v Move Down").ShouldBeFalse();
    }

    [Fact]
    public void GetMenuItems_ShouldIncludePasteCategory_WhenCategoryIsCopied()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _copyPasteLogic.Setup(x => x.CopiedData).Returns(new CopiedData { CopiedCategory = new StateSaveCategory() });

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        result.Any(item => item.Text == "Paste Category").ShouldBeTrue();
    }

    [Fact]
    public void GetMenuItems_ShouldIncludeStateCommands_WithStateNameInHeader_WhenNonDefaultStateSelected()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSave state = new() { Name = "Hovered", ParentContainer = element };
        element.Categories.Add(new StateSaveCategory { Name = "Visibility", States = { state } });
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(state);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        result.Any(item => item.Text == "Rename [Hovered]").ShouldBeTrue();
        result.Any(item => item.Text == "Delete [Hovered]").ShouldBeTrue();
        result.Any(item => item.Text == "Duplicate [Hovered]").ShouldBeTrue();
        result.Any(item => item.Text == "Set [Hovered] variables to default").ShouldBeTrue();
    }

    [Fact]
    public void GetMenuItems_ShouldExcludeStateCommands_WhenSelectedStateIsDefault()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSave defaultState = new() { Name = "Default", ParentContainer = element };
        element.States.Add(defaultState);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(defaultState);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        result.Any(item => item.Text.StartsWith("Rename [")).ShouldBeFalse();
        result.Any(item => item.Text.StartsWith("Delete [")).ShouldBeFalse();
    }

    [Fact]
    public void GetMenuItems_ShouldNestMoveToCategoryChildren_UnderOneParentItem()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSaveCategory currentCategory = new() { Name = "Visibility" };
        StateSaveCategory otherCategory = new() { Name = "Layout" };
        StateSave state = new() { Name = "Hovered", ParentContainer = element };
        currentCategory.States.Add(state);
        element.Categories.Add(currentCategory);
        element.Categories.Add(otherCategory);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(currentCategory);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(state);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        ContextMenuItemViewModel moveToCategory = result.First(item => item.Text == "Move to category");
        moveToCategory.Children.Count.ShouldBe(1);
        moveToCategory.Children[0].Text.ShouldBe("Layout");
    }

    [Fact]
    public void GetMenuItems_ShouldReturnEmpty_WhenNoStateContainerSelected()
    {
        _selectedState.Setup(x => x.SelectedStateContainer).Returns((IStateContainer?)null);

        List<ContextMenuItemViewModel> result = _sut.GetMenuItems();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void MoveStateInDirection_ShouldMoveStateUp_AndRefresh_WhenNotFirst()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSaveCategory category = new() { Name = "Visibility" };
        StateSave state1 = new() { Name = "State1", ParentContainer = element };
        StateSave state2 = new() { Name = "State2", ParentContainer = element };
        category.States.Add(state1);
        category.States.Add(state2);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(state2);

        bool result = _sut.MoveStateInDirection(-1);

        result.ShouldBeTrue();
        category.States[0].ShouldBe(state2);
        category.States[1].ShouldBe(state1);
        _guiCommands.Verify(x => x.RefreshStateTreeView(), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveCurrentObject(), Times.Once);
    }

    [Fact]
    public void MoveStateInDirection_ShouldNotMoveOrRefresh_WhenAlreadyFirst()
    {
        ComponentSave element = new() { Name = "MyComponent" };
        StateSaveCategory category = new() { Name = "Visibility" };
        StateSave state1 = new() { Name = "State1", ParentContainer = element };
        StateSave state2 = new() { Name = "State2", ParentContainer = element };
        category.States.Add(state1);
        category.States.Add(state2);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(state1);

        bool result = _sut.MoveStateInDirection(-1);

        result.ShouldBeFalse();
        category.States[0].ShouldBe(state1);
        category.States[1].ShouldBe(state2);
        _guiCommands.Verify(x => x.RefreshStateTreeView(), Times.Never);
        _fileCommands.Verify(x => x.TryAutoSaveCurrentObject(), Times.Never);
    }

    [Fact]
    public void RenameCategoryClick_ShouldAskToRenameStateCategory()
    {
        StateSaveCategory category = new() { Name = "Visibility" };
        ComponentSave element = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);

        _sut.RenameCategoryClick();

        _editCommands.Verify(x => x.AskToRenameStateCategory(category, element), Times.Once);
    }

    [Fact]
    public void RenameStateClick_ShouldAskToRenameState()
    {
        StateSave state = new() { Name = "Hovered" };
        ComponentSave element = new() { Name = "MyComponent" };
        _selectedState.Setup(x => x.SelectedStateSave).Returns(state);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);

        _sut.RenameStateClick();

        _editCommands.Verify(x => x.AskToRenameState(state, element), Times.Once);
    }
}
