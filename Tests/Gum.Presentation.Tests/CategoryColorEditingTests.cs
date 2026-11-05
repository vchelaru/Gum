using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolCommands;
using Gum.Plugins.InternalPlugins.StatePlugin;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Gum.ViewModels;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Choosing a category color (#7591): the right-click "Color" submenu, and the command behind it, which
/// is one undoable step that notifies plugins, refreshes the tree and the Variables tab, and saves.
/// </summary>
public class CategoryColorEditingTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IAnimationUndoProvider> _animationUndoProvider = new();
    private readonly ComponentSave _component;
    private readonly StateSaveCategory _category;
    private readonly UndoManager _undoManager;
    private readonly EditCommands _editCommands;

    public CategoryColorEditingTests()
    {
        _component = new ComponentSave { Name = "Button" };
        _component.States.Add(new StateSave { Name = "Default" });
        _category = new StateSaveCategory { Name = "Size" };
        _component.Categories.Add(_category);

        _selectedState.Setup(x => x.SelectedElement).Returns(_component);
        _selectedState.Setup(x => x.SelectedComponent).Returns(_component);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(_component);
        _selectedState.Setup(x => x.SelectedStateCategorySave).Returns(_category);
        _selectedState.Setup(x => x.SelectedStateSave).Returns((StateSave?)null);

        Mock<IReferenceFinderProjectProvider> projectProvider = new();
        projectProvider.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
        _undoManager = new UndoManager(
            _selectedState.Object,
            Mock.Of<IUndoRenameLogic>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<IMessenger>(),
            Mock.Of<IUndoPluginNotifier>(),
            _animationUndoProvider.Object,
            projectProvider.Object,
            new Lazy<IVariableReferenceLogic>(() => Mock.Of<IVariableReferenceLogic>()));

        _mocker.Use<ISelectedState>(_selectedState.Object);
        _mocker.Use<IUndoManager>(_undoManager);
        _editCommands = _mocker.CreateInstance<EditCommands>();
    }

    private StateTreeRightClickViewModel CreateMenuViewModel(IEditCommands editCommands)
    {
        Mock<ICopyPasteLogic> copyPaste = new();
        copyPaste.Setup(x => x.CopiedData).Returns(new CopiedData());
        return new StateTreeRightClickViewModel(
            _selectedState.Object,
            Mock.Of<IElementCommands>(),
            editCommands,
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            copyPaste.Object,
            _undoManager);
    }

    private ContextMenuItemViewModel ColorMenu(StateTreeRightClickViewModel viewModel) =>
        viewModel.GetMenuItems().Single(item => item.Text == "Color");

    [Fact]
    public void CategoryMenu_HasAColorSubmenu_WithTenColorsASeparatorAndNone()
    {
        ContextMenuItemViewModel color = ColorMenu(CreateMenuViewModel(Mock.Of<IEditCommands>()));

        color.Children.Select(child => child.Text).ShouldBe(
            new[] { "Indigo", "Violet", "Magenta", "Rose", "Brown", "Green", "Teal", "Slate", "Orange", "Sky", "", "None" });
        color.Children[10].IsSeparator.ShouldBeTrue();
        color.Children.Take(10).Select(child => child.ColorName).ShouldBe(CategoryColors.Names);
    }

    [Fact]
    public void CategoryMenu_ChecksOnlyTheCurrentColor_AndShowsItsDotOnTheColorItem()
    {
        _category.ColorName = "Brown";

        ContextMenuItemViewModel color = ColorMenu(CreateMenuViewModel(Mock.Of<IEditCommands>()));

        color.IconColorName.ShouldBe("Brown");
        color.Children.Where(child => child.IsChecked).Select(child => child.Text).ShouldBe(new[] { "Brown" });
    }

    [Fact]
    public void CategoryMenu_ChecksNothing_ForAnUncoloredOrUnknownColorCategory()
    {
        ColorMenu(CreateMenuViewModel(Mock.Of<IEditCommands>())).Children.ShouldAllBe(child => !child.IsChecked);

        _category.ColorName = "FromANewerTool";
        ContextMenuItemViewModel color = ColorMenu(CreateMenuViewModel(Mock.Of<IEditCommands>()));

        color.IconColorName.ShouldBeNull();
        color.Children.ShouldAllBe(child => !child.IsChecked);
        _category.ColorName.ShouldBe("FromANewerTool");
    }

    [Fact]
    public void CategoryMenu_ColorRowsAndNone_SetTheCategoryColor()
    {
        Mock<IEditCommands> editCommands = new();
        ContextMenuItemViewModel color = ColorMenu(CreateMenuViewModel(editCommands.Object));

        color.Children.Single(child => child.Text == "Rose").Action!();
        color.Children.Single(child => child.Text == "None").Action!();

        editCommands.Verify(x => x.SetStateCategoryColor(_category, "Rose"), Times.Once);
        editCommands.Verify(x => x.SetStateCategoryColor(_category, null), Times.Once);
    }

    [Fact]
    public void StateMenu_HasNoColorItem_WhenAStateIsSelected()
    {
        _selectedState.Setup(x => x.SelectedStateSave).Returns(new StateSave { Name = "Large" });

        CreateMenuViewModel(Mock.Of<IEditCommands>()).GetMenuItems().ShouldNotContain(item => item.Text == "Color");
    }

    [Fact]
    public void SetStateCategoryColor_IsOneUndoStep_AndUndoAndRedoRestoreIt()
    {
        _undoManager.RecordState();

        _editCommands.SetStateCategoryColor(_category, "Teal");

        _category.ColorName.ShouldBe("Teal");
        _undoManager.CurrentElementHistory!.Actions.Count.ShouldBe(1);

        _undoManager.PerformUndo();

        _component.Categories.Single().ColorName.ShouldBeNull();

        _undoManager.PerformRedo();

        _component.Categories.Single().ColorName.ShouldBe("Teal");
    }

    [Fact]
    public void SetStateCategoryColor_ToNone_ClearsItAndUndoBringsItBack()
    {
        _category.ColorName = "Teal";
        _undoManager.RecordState();

        _editCommands.SetStateCategoryColor(_category, null);

        _category.ColorName.ShouldBeNull();

        _undoManager.PerformUndo();

        _component.Categories.Single().ColorName.ShouldBe("Teal");
    }

    [Fact]
    public void SetStateCategoryColor_NotifiesPluginsRefreshesAndSaves()
    {
        Mock<IPluginManager> pluginManager = _mocker.GetMock<IPluginManager>();
        Mock<IGuiCommands> guiCommands = _mocker.GetMock<IGuiCommands>();
        Mock<IFileCommands> fileCommands = _mocker.GetMock<IFileCommands>();

        _editCommands.SetStateCategoryColor(_category, "Green");

        pluginManager.Verify(x => x.CategoryColorChange(_category), Times.Once);
        guiCommands.Verify(x => x.RefreshStateTreeView(), Times.Once);
        guiCommands.Verify(x => x.RefreshVariables(true), Times.Once);
        fileCommands.Verify(x => x.TryAutoSaveCurrentObject(), Times.Once);
    }

    [Fact]
    public void SetStateCategoryColor_WithAnEmptyName_IsNormalizedToUnset()
    {
        _category.ColorName = "Green";

        _editCommands.SetStateCategoryColor(_category, "");

        _category.ColorName.ShouldBeNull();
    }

    [Fact]
    public void SetStateCategoryColor_DoesNothing_WhenTheColorIsAlreadySet()
    {
        _category.ColorName = "Green";
        Mock<IPluginManager> pluginManager = _mocker.GetMock<IPluginManager>();

        _editCommands.SetStateCategoryColor(_category, "Green");

        pluginManager.Verify(x => x.CategoryColorChange(It.IsAny<StateSaveCategory>()), Times.Never);
    }

    [Fact]
    public void Clone_KeepsTheColor_SoACopiedCategoryPastesColored()
    {
        _category.ColorName = "Violet";

        StateSaveCategory pasted = _category.Clone();

        pasted.ColorName.ShouldBe("Violet");
    }
}
