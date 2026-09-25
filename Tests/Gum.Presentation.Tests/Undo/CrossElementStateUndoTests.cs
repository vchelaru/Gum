using System;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Responses;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.Undo;

/// <summary>
/// End-to-end undo/redo for state and category deletes and renames that change variables on OTHER
/// elements (#5011). Runs the real <see cref="EditCommands"/>, <see cref="DeleteLogic"/>,
/// <see cref="RenameLogic"/>, <see cref="ReferenceFinder"/> and <see cref="UndoManager"/>, so the
/// lock the command takes, the cross-element mutation, and the undo replay are all exercised together.
/// </summary>
public class CrossElementStateUndoTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly GumProjectSave _project;
    private readonly UndoManager _undoManager;
    private readonly EditCommands _editCommands;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IUndoPluginNotifier> _undoPluginNotifier;

    public CrossElementStateUndoTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<IReferenceFinderProjectProvider>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);
        _mocker.GetMock<IDeleteProjectProvider>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        _selectedState = _mocker.GetMock<ISelectedState>();
        _selectedState.SetupProperty(x => x.SelectedStateSave);
        _selectedState.SetupProperty(x => x.SelectedStateCategorySave);

        _dialogService = _mocker.GetMock<IDialogService>();
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Affirmative);

        _fileCommands = _mocker.GetMock<IFileCommands>();
        _undoPluginNotifier = _mocker.GetMock<IUndoPluginNotifier>();

        Mock<IPluginManager> pluginManager = _mocker.GetMock<IPluginManager>();
        pluginManager
            .Setup(x => x.GetDeleteStateResponse(It.IsAny<StateSave>(), It.IsAny<IStateContainer>()))
            .Returns(new DeleteResponse { ShouldDelete = true });
        pluginManager
            .Setup(x => x.GetDeleteStateCategoryResponse(It.IsAny<StateSaveCategory>(), It.IsAny<IStateContainer>()))
            .Returns(new DeleteResponse { ShouldDelete = true });

        string whyNotValid;
        _mocker.GetMock<INameVerifier>()
            .Setup(x => x.IsStateNameValid(It.IsAny<string>(), It.IsAny<StateSaveCategory?>(), It.IsAny<StateSave?>(), out whyNotValid))
            .Returns(true);

        _undoManager = _mocker.CreateInstance<UndoManager>();
        _mocker.Use<IUndoManager>(_undoManager);
        _mocker.Use(new Lazy<IUndoManager>(() => _undoManager));

        _mocker.Use<IReferenceFinder>(_mocker.CreateInstance<ReferenceFinder>());
        _mocker.Use<IDeleteLogic>(_mocker.CreateInstance<DeleteLogic>());
        _mocker.Use<IRenameLogic>(_mocker.CreateInstance<RenameLogic>());

        _editCommands = _mocker.CreateInstance<EditCommands>();
    }

    #region Helpers

    private ComponentSave AddComponentWithCategory(string componentName, string categoryName, params string[] stateNames)
    {
        ComponentSave component = new ComponentSave { Name = componentName };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        foreach (string stateName in stateNames)
        {
            category.States.Add(new StateSave { Name = stateName, ParentContainer = component });
        }
        component.Categories.Add(category);

        _project.Components.Add(component);
        _project.ComponentReferences.Add(new ElementReference { Name = componentName, ElementType = ElementType.Component });
        return component;
    }

    private ScreenSave AddScreenWithInstance(string screenName, string instanceName, string instanceType)
    {
        ScreenSave screen = new ScreenSave { Name = screenName };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = instanceType, ParentContainer = screen });

        _project.Screens.Add(screen);
        _project.ScreenReferences.Add(new ElementReference { Name = screenName, ElementType = ElementType.Screen });
        return screen;
    }

    private static VariableSave AddStateVariable(StateSave state, string name, string type, string value)
    {
        VariableSave variable = new VariableSave { Name = name, Type = type, Value = value, SetsValue = true };
        state.Variables.Add(variable);
        return variable;
    }

    /// <summary>Selects the element and state the way the tool does before a state tree command, and
    /// captures the undo baseline as UndoPlugin would on that selection.</summary>
    private void Select(ElementSave element, StateSaveCategory? category, StateSave state)
    {
        _selectedState.Setup(x => x.SelectedElement).Returns(element);
        _selectedState.Setup(x => x.SelectedStateContainer).Returns(element);
        _selectedState.Setup(x => x.SelectedComponent).Returns(element as ComponentSave);
        _selectedState.Object.SelectedStateCategorySave = category;
        _selectedState.Object.SelectedStateSave = state;

        _undoManager.RecordState();
    }

    private void RenameThroughDialog(string newName)
    {
        _dialogService
            .Setup(x => x.GetUserString(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<GetUserStringOptions?>()))
            .Returns(newName);
    }

    #endregion

    #region Delete state

    [Fact]
    public void AskToDeleteState_Undo_ShouldRestoreInstanceVariableOnOtherElement()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);

        _editCommands.AskToDeleteState(hidden, button);
        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBeNull();

        _undoManager.PerformUndo();

        button.Categories.Single().States.Select(item => item.Name).ShouldBe(new[] { "Shown", "Hidden" });
        screen.DefaultState.Variables.Count(item => item.Name == "MyButton.VisibilityState").ShouldBe(1);
        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBe("Hidden");
    }

    [Fact]
    public void AskToDeleteState_UndoThenRedo_ShouldRemoveInstanceVariableAgain()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);

        _editCommands.AskToDeleteState(hidden, button);
        _undoManager.PerformUndo();
        _undoManager.PerformRedo();

        button.Categories.Single().States.Select(item => item.Name).ShouldBe(new[] { "Shown" });
        screen.DefaultState.Variables.ShouldNotContain(item => item.Name == "MyButton.VisibilityState");
    }

    [Fact]
    public void AskToDeleteState_Undo_ShouldRestoreVariableSetInsideCategorizedStateOfOtherElement()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        StateSaveCategory screenCategory = new StateSaveCategory { Name = "Mode" };
        StateSave editMode = new StateSave { Name = "Edit", ParentContainer = screen };
        screenCategory.States.Add(editMode);
        screen.Categories.Add(screenCategory);
        AddStateVariable(editMode, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);

        _editCommands.AskToDeleteState(hidden, button);
        editMode.Variables.ShouldBeEmpty();

        _undoManager.PerformUndo();

        editMode.GetValue("MyButton.VisibilityState").ShouldBe("Hidden");
        screen.DefaultState.Variables.ShouldBeEmpty();
    }

    [Fact]
    public void AskToDeleteState_Undo_ShouldRestoreVariableOnDerivedElementWithoutInstance()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ComponentSave derived = new ComponentSave { Name = "FancyButton", BaseType = "Button" };
        derived.States.Add(new StateSave { Name = "Default", ParentContainer = derived });
        _project.Components.Add(derived);
        AddStateVariable(derived.DefaultState, "VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);

        _editCommands.AskToDeleteState(hidden, button);
        derived.DefaultState.GetValue("VisibilityState").ShouldBeNull();

        _undoManager.PerformUndo();

        derived.DefaultState.GetValue("VisibilityState").ShouldBe("Hidden");
    }

    [Fact]
    public void AskToDeleteState_Undo_ShouldLeaveUnrelatedVariablesOnOtherElementUntouched()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        screen.Instances.Add(new InstanceSave { Name = "OtherButton", BaseType = "Button", ParentContainer = screen });
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        AddStateVariable(screen.DefaultState, "OtherButton.VisibilityState", "Visibility", "Shown");
        Select(button, category, hidden);

        _editCommands.AskToDeleteState(hidden, button);
        _undoManager.PerformUndo();

        screen.DefaultState.Variables.Count.ShouldBe(2);
        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBe("Hidden");
        screen.DefaultState.GetValue("OtherButton.VisibilityState").ShouldBe("Shown");
    }

    [Fact]
    public void AskToDeleteState_Undo_ShouldSaveOtherElementAndNotifyPlugins()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);
        _editCommands.AskToDeleteState(hidden, button);
        _fileCommands.Invocations.Clear();

        _undoManager.PerformUndo();

        _fileCommands.Verify(x => x.TryAutoSaveElement(screen), Times.Once);
        _undoPluginNotifier.Verify(x => x.VariableSet(screen, screen.Instances[0], "VisibilityState", null, true), Times.Once);
    }

    [Fact]
    public void AskToDeleteState_WithNoReferencesElsewhere_ShouldUndoWithoutTouchingOtherElements()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Shown");
        Select(button, category, hidden);

        _editCommands.AskToDeleteState(hidden, button);
        _fileCommands.Invocations.Clear();
        _undoManager.PerformUndo();

        button.Categories.Single().States.Select(item => item.Name).ShouldBe(new[] { "Shown", "Hidden" });
        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBe("Shown");
        _fileCommands.Verify(x => x.TryAutoSaveElement(screen), Times.Never);
        _undoManager.CurrentElementHistory.Actions.Single().CrossElementVariableChanges.ShouldBeNull();
    }

    [Fact]
    public void AskToDeleteState_Undo_WhenOtherElementDeletedSinceRecording_ShouldNotResurrectIt()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);
        _editCommands.AskToDeleteState(hidden, button);
        _project.Screens.Remove(screen);
        _fileCommands.Invocations.Clear();

        Should.NotThrow(() => _undoManager.PerformUndo());

        button.Categories.Single().States.Select(item => item.Name).ShouldBe(new[] { "Shown", "Hidden" });
        screen.DefaultState.Variables.ShouldBeEmpty();
        _fileCommands.Verify(x => x.TryAutoSaveElement(screen), Times.Never);
    }

    #endregion

    #region Delete category

    [Fact]
    public void AskToDeleteStateCategory_Undo_ShouldRestoreInstanceVariableOnOtherElement()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Shown");
        Select(button, category, category.States[0]);

        _editCommands.AskToDeleteStateCategory(category, button);
        button.Categories.ShouldBeEmpty();
        screen.DefaultState.Variables.ShouldBeEmpty();

        _undoManager.PerformUndo();

        button.Categories.Select(item => item.Name).ShouldBe(new[] { "Visibility" });
        screen.DefaultState.Variables.Count(item => item.Name == "MyButton.VisibilityState").ShouldBe(1);
        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBe("Shown");
    }

    [Fact]
    public void AskToDeleteStateCategory_UndoThenRedo_ShouldRemoveInstanceVariableAgain()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Shown");
        Select(button, category, category.States[0]);

        _editCommands.AskToDeleteStateCategory(category, button);
        _undoManager.PerformUndo();
        _undoManager.PerformRedo();

        button.Categories.ShouldBeEmpty();
        screen.DefaultState.Variables.ShouldBeEmpty();
    }

    [Fact]
    public void AskToDeleteStateCategory_Undo_ShouldRestoreVariablesAcrossSeveralElementsAndStates()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        ScreenSave screenA = AddScreenWithInstance("ScreenA", "ButtonA", "Button");
        ScreenSave screenB = AddScreenWithInstance("ScreenB", "ButtonB", "Button");
        StateSaveCategory screenBCategory = new StateSaveCategory { Name = "Mode" };
        StateSave screenBEdit = new StateSave { Name = "Edit", ParentContainer = screenB };
        screenBCategory.States.Add(screenBEdit);
        screenB.Categories.Add(screenBCategory);
        AddStateVariable(screenA.DefaultState, "ButtonA.VisibilityState", "Visibility", "Shown");
        AddStateVariable(screenB.DefaultState, "ButtonB.VisibilityState", "Visibility", "Hidden");
        AddStateVariable(screenBEdit, "ButtonB.VisibilityState", "Visibility", "Shown");
        Select(button, category, category.States[0]);

        _editCommands.AskToDeleteStateCategory(category, button);
        _undoManager.PerformUndo();

        screenA.DefaultState.GetValue("ButtonA.VisibilityState").ShouldBe("Shown");
        screenB.DefaultState.GetValue("ButtonB.VisibilityState").ShouldBe("Hidden");
        screenBEdit.GetValue("ButtonB.VisibilityState").ShouldBe("Shown");
    }

    [Fact]
    public void AskToDeleteStateCategory_Undo_ShouldRestoreOwnDefaultStateVariableWhenAnotherCategoryIsSelected()
    {
        // The owner's undo snapshot restores its categories and its selected state. With a state of
        // another category selected, the owner's default state is neither, so its removed
        // VisibilityState assignment must come back through the recorded change.
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory visibility = button.Categories[0];
        StateSaveCategory size = new StateSaveCategory { Name = "Size" };
        StateSave big = new StateSave { Name = "Big", ParentContainer = button };
        size.States.Add(big);
        button.Categories.Add(size);
        AddStateVariable(button.DefaultState, "VisibilityState", "Visibility", "Hidden");
        Select(button, size, big);

        _editCommands.AskToDeleteStateCategory(visibility, button);
        button.DefaultState.Variables.ShouldBeEmpty();

        _undoManager.PerformUndo();

        button.Categories.Select(item => item.Name).ShouldBe(new[] { "Visibility", "Size" });
        button.DefaultState.Variables.ShouldHaveSingleItem().Value.ShouldBe("Hidden");
    }

    #endregion

    #region Rename state

    [Fact]
    public void AskToRenameState_Undo_ShouldRestoreOldValueOnOtherElement()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        VariableSave variable = AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);
        RenameThroughDialog("Invisible");

        _editCommands.AskToRenameState(hidden, button);
        variable.Value.ShouldBe("Invisible");

        _undoManager.PerformUndo();

        button.Categories.Single().States.Select(item => item.Name).ShouldBe(new[] { "Shown", "Hidden" });
        screen.DefaultState.Variables.Count.ShouldBe(1);
        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBe("Hidden");
    }

    [Fact]
    public void AskToRenameState_UndoThenRedo_ShouldApplyNewValueAgain()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);
        RenameThroughDialog("Invisible");

        _editCommands.AskToRenameState(hidden, button);
        _undoManager.PerformUndo();
        _undoManager.PerformRedo();

        button.Categories.Single().States.Select(item => item.Name).ShouldBe(new[] { "Shown", "Invisible" });
        screen.DefaultState.Variables.Count.ShouldBe(1);
        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBe("Invisible");
    }

    [Fact]
    public void AskToRenameState_Undo_ShouldRestoreDerivedElementValue()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ComponentSave derived = new ComponentSave { Name = "FancyButton", BaseType = "Button" };
        derived.States.Add(new StateSave { Name = "Default", ParentContainer = derived });
        _project.Components.Add(derived);
        AddStateVariable(derived.DefaultState, "VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);
        RenameThroughDialog("Invisible");

        _editCommands.AskToRenameState(hidden, button);
        derived.DefaultState.GetValue("VisibilityState").ShouldBe("Invisible");

        _undoManager.PerformUndo();

        derived.DefaultState.GetValue("VisibilityState").ShouldBe("Hidden");
    }

    [Fact]
    public void AskToRenameState_Undo_WhenOtherVariableRemovedSinceRecording_ShouldNotReAddIt()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        VariableSave variable = AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);
        RenameThroughDialog("Invisible");
        _editCommands.AskToRenameState(hidden, button);
        screen.DefaultState.Variables.Remove(variable);

        Should.NotThrow(() => _undoManager.PerformUndo());

        screen.DefaultState.Variables.ShouldBeEmpty();
    }

    [Fact]
    public void AskToRenameState_Undo_ShouldNotChangeValueEditedToAnotherStateSinceRecording()
    {
        // A cross-element modification is replayed only onto the value it produced. If the user has
        // since pointed the instance at a different state, undoing the rename must not overwrite that.
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        VariableSave variable = AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);
        RenameThroughDialog("Invisible");
        _editCommands.AskToRenameState(hidden, button);
        variable.Value = "Shown";

        _undoManager.PerformUndo();

        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBe("Shown");
    }

    #endregion

    #region Rename category

    [Fact]
    public void AskToRenameStateCategory_Undo_ShouldRestoreOldVariableNameAndTypeOnOtherElement()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        VariableSave variable = AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, category.States[0]);
        RenameThroughDialog("Display");

        _editCommands.AskToRenameStateCategory(category, button);
        variable.Name.ShouldBe("MyButton.DisplayState");
        variable.Type.ShouldBe("Display");

        _undoManager.PerformUndo();

        button.Categories.Select(item => item.Name).ShouldBe(new[] { "Visibility" });
        VariableSave restored = screen.DefaultState.Variables.ShouldHaveSingleItem();
        restored.Name.ShouldBe("MyButton.VisibilityState");
        restored.Type.ShouldBe("Visibility");
        restored.Value.ShouldBe("Hidden");
    }

    [Fact]
    public void AskToRenameStateCategory_UndoThenRedo_ShouldApplyNewVariableNameAndTypeAgain()
    {
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, category.States[0]);
        RenameThroughDialog("Display");

        _editCommands.AskToRenameStateCategory(category, button);
        _undoManager.PerformUndo();
        _undoManager.PerformRedo();

        button.Categories.Select(item => item.Name).ShouldBe(new[] { "Display" });
        VariableSave redone = screen.DefaultState.Variables.ShouldHaveSingleItem();
        redone.Name.ShouldBe("MyButton.DisplayState");
        redone.Type.ShouldBe("Display");
        redone.Value.ShouldBe("Hidden");
    }

    #endregion

    #region Undo history boundaries

    [Fact]
    public void CrossElementChanges_ShouldAttachOnlyToTheActionThatMadeThem()
    {
        // A later ordinary edit on the owner must not carry the delete's cross-element changes: undoing
        // that edit alone must leave the other element's variable removed.
        ComponentSave button = AddComponentWithCategory("Button", "Visibility", "Shown", "Hidden");
        StateSaveCategory category = button.Categories[0];
        StateSave hidden = category.States[1];
        ScreenSave screen = AddScreenWithInstance("MainScreen", "MyButton", "Button");
        AddStateVariable(screen.DefaultState, "MyButton.VisibilityState", "Visibility", "Hidden");
        Select(button, category, hidden);
        _editCommands.AskToDeleteState(hidden, button);
        Select(button, null, button.DefaultState);
        using (_undoManager.RequestLock())
        {
            button.DefaultState.SetValue("X", 10f, "float");
        }

        _undoManager.PerformUndo();

        button.DefaultState.GetValue("X").ShouldBeNull();
        screen.DefaultState.Variables.ShouldBeEmpty();

        _undoManager.PerformUndo();

        screen.DefaultState.GetValue("MyButton.VisibilityState").ShouldBe("Hidden");
    }

    #endregion
}
