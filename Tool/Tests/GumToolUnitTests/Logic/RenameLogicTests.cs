using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;

namespace GumToolUnitTests.Logic;

public class RenameLogicTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly RenameLogic _renameLogic;
    private readonly GumProjectSave _project;

    public RenameLogicTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        // IReferenceFinder is now injected into RenameLogic; set up default returns
        // so that tests which call rename methods work through the delegation.
        _mocker.GetMock<IReferenceFinder>()
            .Setup(x => x.GetReferencesToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(new ElementRenameChanges());
        _mocker.GetMock<IReferenceFinder>()
            .Setup(x => x.GetReferencesToInstance(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>(), It.IsAny<string>()))
            .Returns(new InstanceRenameChanges());
        _mocker.GetMock<IReferenceFinder>()
            .Setup(x => x.GetReferencesToState(It.IsAny<StateSave>(), It.IsAny<string>(), It.IsAny<IStateContainer>(), It.IsAny<StateSaveCategory>()))
            .Returns(new StateRenameChanges());
        _mocker.GetMock<IReferenceFinder>()
            .Setup(x => x.GetReferencesToStateCategory(It.IsAny<IStateContainer>(), It.IsAny<StateSaveCategory>(), It.IsAny<string>()))
            .Returns(new CategoryRenameChanges());
        _mocker.GetMock<IReferenceFinder>()
            .Setup(x => x.GetReferencesToVariable(It.IsAny<IStateContainer>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new VariableChangeResponse());

        _renameLogic = _mocker.CreateInstance<RenameLogic>();
    }

    [Fact]
    public void ApplyElementRenameChanges_ElementBaseType_IsUpdated()
    {
        // After apply, an element whose BaseType matched oldName should be updated to the new name.
        var button = new ComponentSave { Name = "ButtonNew" };

        var screen = new ScreenSave { Name = "TestScreen", BaseType = "Button" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });

        var changes = new ElementRenameChanges();
        changes.ElementsWithBaseTypeReference.Add(screen);

        _renameLogic.ApplyElementRenameChanges(changes, button, oldName: "Button");

        screen.BaseType.ShouldBe("ButtonNew");
    }

    [Fact]
    public void ApplyElementRenameChanges_InstanceBaseType_IsUpdated()
    {
        // After apply, an instance whose BaseType matched oldName should be updated to the new name.
        var button = new ComponentSave { Name = "ButtonNew" };

        var screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);

        var changes = new ElementRenameChanges();
        changes.InstancesWithBaseTypeReference.Add((screen, instance));

        _renameLogic.ApplyElementRenameChanges(changes, button, oldName: "Button");

        instance.BaseType.ShouldBe("ButtonNew");
    }

    [Fact]
    public void ApplyElementRenameChanges_VariableReferenceEntry_StringIsUpdated()
    {
        // After apply, a VariableReferences list entry whose right side referenced
        // "Components/Button.SomeProperty" should be rewritten with the new element name.
        var button = new ComponentSave { Name = "ButtonNew" };

        var screen = new ScreenSave { Name = "TestScreen" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        var varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/Button.SomeProperty");
        defaultState.VariableLists.Add(varRefList);

        var changes = new ElementRenameChanges();
        changes.VariableReferenceChanges.Add(new VariableReferenceChange
        {
            Container = screen,
            VariableReferenceList = varRefList,
            LineIndex = 0,
            ChangedSide = SideOfEquals.Right
        });

        _renameLogic.ApplyElementRenameChanges(changes, button, oldName: "Button");

        varRefList.Value[0].ShouldBe("SomeVar = Components/ButtonNew.SomeProperty");
    }

    #region State rename

    [Fact]
    public void RenameState_UpdatesStateName()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        StateSaveCategory category = new StateSaveCategory { Name = "Visibility" };
        StateSave shownState = new StateSave { Name = "Shown", ParentContainer = button };
        category.States.Add(shownState);
        button.Categories.Add(category);
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        string? outValue = null;
        _mocker.GetMock<INameVerifier>()
            .Setup(x => x.IsStateNameValid(It.IsAny<string>(), It.IsAny<StateSaveCategory>(), It.IsAny<StateSave>(), out outValue))
            .Returns(true);

        _renameLogic.RenameState(shownState, category, "Visible");

        shownState.Name.ShouldBe("Visible");
    }

    [Fact]
    public void RenameState_UpdatesVariableValueInReferencingElement()
    {
        // Button has a "Visibility" category with state "Shown".
        // A screen has an instance of Button with a variable "myButton.VisibilityState = Shown".
        // After renaming "Shown" to "Visible", the variable value should update.
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        StateSaveCategory visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        StateSave shownState = new StateSave { Name = "Shown", ParentContainer = button };
        visibilityCategory.States.Add(shownState);
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        InstanceSave instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        VariableSave stateVar = new VariableSave { Name = "myButton.VisibilityState", Value = "Shown" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        StateRenameChanges stateChanges = new StateRenameChanges();
        stateChanges.VariablesToUpdate.Add((screen, stateVar));
        _mocker.GetMock<IReferenceFinder>()
            .Setup(x => x.GetReferencesToState(shownState, "Shown", button, visibilityCategory))
            .Returns(stateChanges);

        string? outValue = null;
        _mocker.GetMock<INameVerifier>()
            .Setup(x => x.IsStateNameValid(It.IsAny<string>(), It.IsAny<StateSaveCategory>(), It.IsAny<StateSave>(), out outValue))
            .Returns(true);

        _renameLogic.RenameState(shownState, visibilityCategory, "Visible");

        stateVar.Value.ShouldBe("Visible");
    }

    [Fact]
    public void ApplyStateRenameChanges_UpdatesVariableValue()
    {
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);

        var stateVar = new VariableSave { Name = "myButton.VisibilityState", Value = "Shown" };
        var renamedState = new StateSave { Name = "Visible", ParentContainer = button };

        var changes = new StateRenameChanges();
        changes.VariablesToUpdate.Add((screen, stateVar));

        _renameLogic.ApplyStateRenameChanges(changes, renamedState);

        stateVar.Value.ShouldBe("Visible");
    }

    #endregion

    #region Category rename

    [Fact]
    public void AskToRenameStateCategory_UpdatesCategoryName()
    {
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        var category = new StateSaveCategory { Name = "Visibility" };
        button.Categories.Add(category);
        _project.Components.Add(button);

        _mocker.GetMock<IDeleteLogic>()
            .Setup(x => x.GetBehaviorsNeedingCategory(It.IsAny<StateSaveCategory>(), It.IsAny<ComponentSave>()))
            .Returns(new List<BehaviorSave>());
        _mocker.GetMock<IDialogService>()
            .Setup(x => x.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Returns("Appearance");

        _renameLogic.AskToRenameStateCategory(category, button);

        category.Name.ShouldBe("Appearance");
    }

    [Fact]
    public void AskToRenameStateCategory_UpdatesVariableTypeInOwnerStates()
    {
        // Button has a "Visibility" category. Its default state has a variable of type
        // "VisibilityState" (a self-referencing state variable). After renaming the
        // category to "Appearance", the variable's Name and Type should update.
        var button = new ComponentSave { Name = "Button" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = button };
        var stateVar = new VariableSave { Name = "VisibilityState", Type = "VisibilityState" };
        defaultState.Variables.Add(stateVar);
        button.States.Add(defaultState);
        var category = new StateSaveCategory { Name = "Visibility" };
        button.Categories.Add(category);
        _project.Components.Add(button);

        _mocker.GetMock<IDeleteLogic>()
            .Setup(x => x.GetBehaviorsNeedingCategory(It.IsAny<StateSaveCategory>(), It.IsAny<ComponentSave>()))
            .Returns(new List<BehaviorSave>());
        _mocker.GetMock<IDialogService>()
            .Setup(x => x.GetUserString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetUserStringOptions>()))
            .Returns("Appearance");

        _renameLogic.AskToRenameStateCategory(category, button);

        stateVar.Type.ShouldBe("AppearanceState");
        stateVar.Name.ShouldBe("AppearanceState");
    }

    #endregion

    #region Instance rename

    [Fact]
    public void ApplyInstanceRenameChanges_VariableReferenceRightSide_IsUpdated()
    {
        // "Width = ChildA.Width" should become "Width = NewName.Width".
        var componentA = new ComponentSave { Name = "ComponentA" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = componentA };
        componentA.States.Add(defaultState);
        _project.Components.Add(componentA);

        var varRefList = new VariableListSave<string> { Type = "string", Name = "ChildB.VariableReferences" };
        varRefList.Value.Add("Width = ChildA.Width");
        defaultState.VariableLists.Add(varRefList);

        var changes = new InstanceRenameChanges();
        changes.VariableReferenceChanges.Add(new VariableReferenceChange
        {
            Container = componentA,
            VariableReferenceList = varRefList,
            LineIndex = 0,
            ChangedSide = SideOfEquals.Right
        });

        _renameLogic.ApplyInstanceRenameChanges(changes, "NewName", "ChildA", new HashSet<ElementSave>());

        varRefList.Value[0].ShouldBe("Width = NewName.Width");
    }

    [Fact]
    public void ApplyInstanceRenameChanges_ParentVariableInOtherElement_IsUpdated()
    {
        // An InstanceRenameChanges with a ParentVariablesInOtherElements entry whose value is
        // "componentInstance.BeforeRename" should have that value updated to
        // "componentInstance.AfterRename" after applying the changes.
        var screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);

        var parentVar = new VariableSave { Name = "OtherContainer.Parent", Value = "componentInstance.BeforeRename" };
        screen.DefaultState.Variables.Add(parentVar);

        var changes = new InstanceRenameChanges();
        changes.ParentVariablesInOtherElements.Add((screen, parentVar));

        _renameLogic.ApplyInstanceRenameChanges(changes, "AfterRename", "BeforeRename", new HashSet<ElementSave>());

        parentVar.Value.ShouldBe("componentInstance.AfterRename");
    }

    [Fact]
    public void ApplyInstanceRenameChanges_AddsContainerToElementsToSave()
    {
        var componentA = new ComponentSave { Name = "ComponentA" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = componentA };
        componentA.States.Add(defaultState);
        _project.Components.Add(componentA);

        var varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("Width = ChildA.Width");
        defaultState.VariableLists.Add(varRefList);

        var changes = new InstanceRenameChanges();
        changes.VariableReferenceChanges.Add(new VariableReferenceChange
        {
            Container = componentA,
            VariableReferenceList = varRefList,
            LineIndex = 0,
            ChangedSide = SideOfEquals.Right
        });

        var elementsToSave = new HashSet<ElementSave>();
        _renameLogic.ApplyInstanceRenameChanges(changes, "NewName", "ChildA", elementsToSave);

        elementsToSave.ShouldContain(componentA);
    }

    #endregion

    [Fact]
    public void ApplyVariableRenameChanges_InstanceVariableRename_PreservesValue()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        VariableSave instanceVar = new VariableSave { Name = "myButton.Color", Value = "Red" };
        screen.DefaultState.Variables.Add(instanceVar);
        _project.Screens.Add(screen);

        VariableChangeResponse changes = new VariableChangeResponse();
        changes.VariableChanges.Add(new VariableChange
        {
            Container = screen,
            State = screen.DefaultState,
            Variable = instanceVar
        });
        _renameLogic.ApplyVariableRenameChanges(changes, "Color", "BackgroundColor", new HashSet<ElementSave>());

        instanceVar.Name.ShouldBe("myButton.BackgroundColor");
        instanceVar.Value.ShouldBe("Red");
    }

    [Fact]
    public void ApplyVariableRenameChanges_ExposedVariableRenameInInheritingComponent_PreservesNameAndValue()
    {
        // Button exposes an internal variable as "ButtonColor".
        // ButtonChild inherits from Button and re-exposes the same variable with the same alias.
        // When Button renames the alias, ButtonChild's alias should update too,
        // but the underlying variable Name and Value should be unchanged.
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ComponentSave buttonChild = new ComponentSave { Name = "ButtonChild", BaseType = "Button" };
        buttonChild.States.Add(new StateSave { Name = "Default", ParentContainer = buttonChild });
        VariableSave childVar = new VariableSave
        {
            Name = "InnerSprite.Color",
            ExposedAsName = "ButtonColor",
            Value = "Blue"
        };
        buttonChild.DefaultState.Variables.Add(childVar);
        _project.Components.Add(buttonChild);

        VariableChangeResponse changes = new VariableChangeResponse();
        changes.VariableChanges.Add(new VariableChange
        {
            Container = buttonChild,
            State = buttonChild.DefaultState,
            Variable = childVar
        });
        _renameLogic.ApplyVariableRenameChanges(changes, "ButtonColor", "BgColor", new HashSet<ElementSave>());

        childVar.ExposedAsName.ShouldBe("BgColor");
        childVar.Name.ShouldBe("InnerSprite.Color");
        childVar.Value.ShouldBe("Blue");
    }

    [Fact]
    public void ApplyVariableRenameChanges_InstanceVariableRename_AddsElementToNeedingSave()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        VariableSave colorVar = new VariableSave { Name = "myButton.Color", Value = "Red" };
        screen.DefaultState.Variables.Add(colorVar);
        _project.Screens.Add(screen);

        HashSet<ElementSave> elementsNeedingSave = new HashSet<ElementSave>();
        VariableChangeResponse changes = new VariableChangeResponse();
        changes.VariableChanges.Add(new VariableChange
        {
            Container = screen,
            State = screen.DefaultState,
            Variable = colorVar
        });
        _renameLogic.ApplyVariableRenameChanges(changes, "Color", "BackgroundColor", elementsNeedingSave);

        elementsNeedingSave.ShouldContain(screen);
    }
}
