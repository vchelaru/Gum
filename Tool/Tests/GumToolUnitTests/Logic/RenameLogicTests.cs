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

    [Fact]
    public void GetChangesForRenamedElement_ComponentBaseType_IsDetected()
    {
        // A component that inherits from the renamed element should appear in ElementsWithBaseTypeReference.
        var button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var derivedButton = new ComponentSave { Name = "DerivedButton", BaseType = "Button" };
        derivedButton.States.Add(new StateSave { Name = "Default", ParentContainer = derivedButton });
        _project.Components.Add(derivedButton);

        var changes = _renameLogic.GetChangesForRenamedElement(button, oldName: "Button");

        changes.ElementsWithBaseTypeReference.ShouldContain(derivedButton);
    }

    [Fact]
    public void GetChangesForRenamedElement_ContainedTypeVariable_IsDetected()
    {
        // A ContainedType variable whose value matches oldName should appear in ContainedTypeVariableReferences.
        var button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var listBox = new ComponentSave { Name = "ListBox" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = listBox };
        listBox.States.Add(defaultState);
        var containedTypeVar = new VariableSave { Name = "ContainedType", Value = "Button" };
        defaultState.Variables.Add(containedTypeVar);
        _project.Components.Add(listBox);

        var changes = _renameLogic.GetChangesForRenamedElement(button, oldName: "Button");

        changes.ContainedTypeVariableReferences.Count.ShouldBe(1);
        changes.ContainedTypeVariableReferences[0].Container.ShouldBe(listBox);
        changes.ContainedTypeVariableReferences[0].Variable.ShouldBe(containedTypeVar);
    }

    [Fact]
    public void GetChangesForRenamedElement_InstanceBaseType_IsDetected()
    {
        // An instance of the renamed element should appear in InstancesWithBaseTypeReference
        // with the correct container.
        var button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedElement(button, oldName: "Button");

        changes.InstancesWithBaseTypeReference.Count.ShouldBe(1);
        changes.InstancesWithBaseTypeReference[0].Container.ShouldBe(screen);
        changes.InstancesWithBaseTypeReference[0].Instance.ShouldBe(instance);
    }

    [Fact]
    public void GetChangesForRenamedElement_NoMatchingReferences_ReturnsEmptyLists()
    {
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedElement(button, oldName: "OldButtonName");

        changes.ElementsWithBaseTypeReference.ShouldBeEmpty();
        changes.InstancesWithBaseTypeReference.ShouldBeEmpty();
        changes.ContainedTypeVariableReferences.ShouldBeEmpty();
    }

    [Fact]
    public void GetChangesForRenamedElement_ScreenBaseType_IsDetected()
    {
        // A screen that inherits from the renamed element should appear in ElementsWithBaseTypeReference.
        var panel = new ComponentSave { Name = "PanelNew" };
        panel.States.Add(new StateSave { Name = "Default", ParentContainer = panel });
        _project.Components.Add(panel);

        var screen = new ScreenSave { Name = "MainScreen", BaseType = "Panel" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedElement(panel, oldName: "Panel");

        changes.ElementsWithBaseTypeReference.ShouldContain(screen);
    }

    [Fact]
    public void GetChangesForRenamedElement_UnrelatedBaseType_IsNotDetected()
    {
        // An element whose BaseType doesn't match oldName should not be collected.
        var button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var otherComponent = new ComponentSave { Name = "OtherComponent", BaseType = "SomeOtherType" };
        otherComponent.States.Add(new StateSave { Name = "Default", ParentContainer = otherComponent });
        _project.Components.Add(otherComponent);

        var changes = _renameLogic.GetChangesForRenamedElement(button, oldName: "Button");

        changes.ElementsWithBaseTypeReference.ShouldBeEmpty();
    }

    [Fact]
    public void GetChangesForRenamedElement_VariableReferenceRightSideMatchingComponent_IsDetected()
    {
        // A VariableReferences entry whose right side is "Components/Button.SomeProperty"
        // should be detected when the component named "Button" is renamed.
        var button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        var varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/Button.SomeProperty");
        defaultState.VariableLists.Add(varRefList);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedElement(button, oldName: "Button");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(screen);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
    }

    [Fact]
    public void GetChangesForRenamedElement_VariableReferenceRightSideMultipleEntries_OnlyMatchingLineIsDetected()
    {
        // A VariableReferences list with two entries — only the one whose right side
        // references "Components/Button" should be detected; the other should be ignored.
        var button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        var varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/Button.SomeProperty");
        varRefList.Value.Add("OtherVar = Components/OtherComponent.SomeProp");
        defaultState.VariableLists.Add(varRefList);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedElement(button, oldName: "Button");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
    }

    [Fact]
    public void GetChangesForRenamedElement_VariableReferenceRightSideNonMatchingComponent_IsNotDetected()
    {
        // A VariableReferences entry whose right side references a different component
        // should not be detected.
        var button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        var varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/OtherComponent.SomeProperty");
        defaultState.VariableLists.Add(varRefList);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedElement(button, oldName: "Button");

        changes.VariableReferenceChanges.ShouldBeEmpty();
    }

    #region State rename

    [Fact]
    public void RenameState_UpdatesStateName()
    {
        var button = new ComponentSave { Name = "Button" };
        var category = new StateSaveCategory { Name = "Visibility" };
        var shownState = new StateSave { Name = "Shown", ParentContainer = button };
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
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        var visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        var shownState = new StateSave { Name = "Shown", ParentContainer = button };
        visibilityCategory.States.Add(shownState);
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        var stateVar = new VariableSave { Name = "myButton.VisibilityState", Value = "Shown" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        string? outValue = null;
        _mocker.GetMock<INameVerifier>()
            .Setup(x => x.IsStateNameValid(It.IsAny<string>(), It.IsAny<StateSaveCategory>(), It.IsAny<StateSave>(), out outValue))
            .Returns(true);

        _renameLogic.RenameState(shownState, visibilityCategory, "Visible");

        stateVar.Value.ShouldBe("Visible");
    }

    [Fact]
    public void GetChangesForRenamedState_MatchingVariable_IsDetected()
    {
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        var visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        var shownState = new StateSave { Name = "Shown", ParentContainer = button };
        visibilityCategory.States.Add(shownState);
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        var stateVar = new VariableSave { Name = "myButton.VisibilityState", Value = "Shown" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedState(shownState, "Shown", button, visibilityCategory);

        changes.VariablesToUpdate.Count.ShouldBe(1);
        changes.VariablesToUpdate[0].Container.ShouldBe(screen);
        changes.VariablesToUpdate[0].Variable.ShouldBe(stateVar);
    }

    [Fact]
    public void GetChangesForRenamedState_NonMatchingValue_IsNotDetected()
    {
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        var visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        var shownState = new StateSave { Name = "Shown", ParentContainer = button };
        visibilityCategory.States.Add(shownState);
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        // Value is "Hidden", not "Shown" — should not match
        screenDefault.Variables.Add(new VariableSave { Name = "myButton.VisibilityState", Value = "Hidden" });
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedState(shownState, "Shown", button, visibilityCategory);

        changes.VariablesToUpdate.ShouldBeEmpty();
    }

    [Fact]
    public void GetChangesForRenamedState_NullCategory_UsesPlainStateVariableName()
    {
        // State not in a category — variable name should be "instanceName.State", not "instanceName.CategoryState"
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        var activeState = new StateSave { Name = "Active", ParentContainer = button };
        button.States.Add(activeState);
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        var stateVar = new VariableSave { Name = "myButton.State", Value = "Active" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedState(activeState, "Active", button, category: null);

        changes.VariablesToUpdate.Count.ShouldBe(1);
        changes.VariablesToUpdate[0].Variable.ShouldBe(stateVar);
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
    public void GetChangesForRenamedCategory_InstanceWithMatchingType_IsDetected()
    {
        // Button has a "Visibility" category.
        // A screen has an instance of Button with a variable of Type "Visibility" (the category name).
        // GetChangesForRenamedCategory should detect this variable.
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        var visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        var stateVar = new VariableSave { Name = "myButton.VisibilityState", Type = "Visibility" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedCategory(button, visibilityCategory, "Visibility");

        changes.VariableChanges.Count.ShouldBe(1);
        changes.VariableChanges[0].Variable.ShouldBe(stateVar);
    }

    [Fact]
    public void GetChangesForRenamedCategory_NonMatchingType_IsNotDetected()
    {
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        var visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        var screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        // Type is "Appearance" not "Visibility" — should not match
        screenDefault.Variables.Add(new VariableSave { Name = "myButton.AppearanceState", Type = "Appearance" });
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedCategory(button, visibilityCategory, "Visibility");

        changes.VariableChanges.ShouldBeEmpty();
    }

    [Fact]
    public void GetChangesForRenamedCategory_InheritedElement_IsDetected()
    {
        // Button has a "Visibility" category. ButtonChild inherits from Button.
        // A screen has an instance of ButtonChild with a variable of Type "Visibility".
        // Since ButtonChild is in Button's inheritance chain, the variable should be detected.
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        var visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        var buttonChild = new ComponentSave { Name = "ButtonChild", BaseType = "Button" };
        buttonChild.States.Add(new StateSave { Name = "Default", ParentContainer = buttonChild });
        _project.Components.Add(buttonChild);

        var screen = new ScreenSave { Name = "TestScreen" };
        var screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        var instance = new InstanceSave { Name = "myButton", BaseType = "ButtonChild", ParentContainer = screen };
        screen.Instances.Add(instance);
        var stateVar = new VariableSave { Name = "myButton.VisibilityState", Type = "Visibility" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedCategory(button, visibilityCategory, "Visibility");

        changes.VariableChanges.Count.ShouldBe(1);
        changes.VariableChanges[0].Variable.ShouldBe(stateVar);
    }

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
    public void GetChangesForRenamedInstance_VariableReferenceRightSide_SameElement_IsDetected()
    {
        // ComponentA has instances "ChildA" and "ChildB".
        // ChildB.VariableReferences has entry "Width = ChildA.Width" — ChildA is on the right side.
        // Renaming ChildA should detect this entry as a right-side change.
        var componentA = new ComponentSave { Name = "ComponentA" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = componentA };
        componentA.States.Add(defaultState);
        var childA = new InstanceSave { Name = "ChildA", BaseType = "Sprite", ParentContainer = componentA };
        var childB = new InstanceSave { Name = "ChildB", BaseType = "Sprite", ParentContainer = componentA };
        componentA.Instances.Add(childA);
        componentA.Instances.Add(childB);
        var varRefList = new VariableListSave<string> { Type = "string", Name = "ChildB.VariableReferences" };
        varRefList.Value.Add("Width = ChildA.Width");
        defaultState.VariableLists.Add(varRefList);
        _project.Components.Add(componentA);

        var changes = _renameLogic.GetChangesForRenamedInstance(componentA, childA, "ChildA");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(componentA);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
        changes.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    [Fact]
    public void GetChangesForRenamedInstance_VariableReferenceRightSide_InInheritingElement_IsDetected()
    {
        // ComponentB has instance "ChildA".
        // DerivedComponent inherits from ComponentB and has a VariableReferences entry
        // "SomeVar = ChildA.Width". Renaming ChildA in ComponentB should detect the entry
        // in DerivedComponent since it inherits the instance.
        var componentB = new ComponentSave { Name = "ComponentB" };
        componentB.States.Add(new StateSave { Name = "Default", ParentContainer = componentB });
        var childA = new InstanceSave { Name = "ChildA", BaseType = "Sprite", ParentContainer = componentB };
        componentB.Instances.Add(childA);
        _project.Components.Add(componentB);

        var derivedComponent = new ComponentSave { Name = "DerivedComponent", BaseType = "ComponentB" };
        var derivedDefault = new StateSave { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefault);
        var varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = ChildA.Width");
        derivedDefault.VariableLists.Add(varRefList);
        _project.Components.Add(derivedComponent);

        var changes = _renameLogic.GetChangesForRenamedInstance(componentB, childA, "ChildA");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(derivedComponent);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
        changes.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    [Fact]
    public void GetChangesForRenamedInstance_SameInstanceNameInDifferentComponent_OnlyRenamedInstanceIsDetected()
    {
        // ComponentA has instance "Sprite" with a VariableReferences entry "Width = Sprite.Width".
        // ComponentB also has instance "Sprite" with the same VariableReferences pattern.
        // Renaming "Sprite" in ComponentA must only detect ComponentA's entry, not ComponentB's,
        // because instance names are scoped to their containing element.
        var componentA = new ComponentSave { Name = "ComponentA" };
        var defaultStateA = new StateSave { Name = "Default", ParentContainer = componentA };
        componentA.States.Add(defaultStateA);
        var spriteA = new InstanceSave { Name = "Sprite", BaseType = "NineSlice", ParentContainer = componentA };
        componentA.Instances.Add(spriteA);
        var varRefListA = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefListA.Value.Add("Width = Sprite.Width");
        defaultStateA.VariableLists.Add(varRefListA);
        _project.Components.Add(componentA);

        var componentB = new ComponentSave { Name = "ComponentB" };
        var defaultStateB = new StateSave { Name = "Default", ParentContainer = componentB };
        componentB.States.Add(defaultStateB);
        var spriteB = new InstanceSave { Name = "Sprite", BaseType = "NineSlice", ParentContainer = componentB };
        componentB.Instances.Add(spriteB);
        var varRefListB = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefListB.Value.Add("Width = Sprite.Width");
        defaultStateB.VariableLists.Add(varRefListB);
        _project.Components.Add(componentB);

        var changes = _renameLogic.GetChangesForRenamedInstance(componentA, spriteA, "Sprite");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].VariableReferenceList.ShouldBe(varRefListA);
    }

    [Fact]
    public void GetChangesForRenamedInstance_VariableReferenceRightSide_CrossComponentQualifiedReference_IsDetected()
    {
        // ComponentA has instance "Sprite". ComponentB also has instance "Sprite", and its
        // Sprite.VariableReferences contains "Width = Components/ComponentA/Sprite.Width" —
        // a cross-component qualified reference to ComponentA's Sprite instance.
        // Renaming "Sprite" in ComponentA should detect this reference in ComponentB so it
        // can be updated to "Width = Components/ComponentA/SpriteRename.Width".
        var componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });
        var spriteA = new InstanceSave { Name = "Sprite", BaseType = "Sprite", ParentContainer = componentA };
        componentA.Instances.Add(spriteA);
        _project.Components.Add(componentA);

        var componentB = new ComponentSave { Name = "ComponentB" };
        var defaultStateB = new StateSave { Name = "Default", ParentContainer = componentB };
        componentB.States.Add(defaultStateB);
        var spriteB = new InstanceSave { Name = "Sprite", BaseType = "Sprite", ParentContainer = componentB };
        componentB.Instances.Add(spriteB);
        var varRefList = new VariableListSave<string> { Type = "string", Name = "Sprite.VariableReferences" };
        varRefList.Value.Add("Width = Components/ComponentA.Sprite.Width");
        defaultStateB.VariableLists.Add(varRefList);
        _project.Components.Add(componentB);

        var changes = _renameLogic.GetChangesForRenamedInstance(componentA, spriteA, "Sprite");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(componentB);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
        changes.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

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
    public void GetChangesForRenamedVariable_VariableReferenceLeftSideOnInstance_IsDetected()
    {
        // ComponentA has a variable MyVar.
        // ComponentB has instance myA of type ComponentA with a VariableReferences entry
        // where MyVar appears on the left side: "MyVar = someOtherVar".
        // Renaming MyVar in ComponentA should detect this as a left-side reference change.
        var componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });
        _project.Components.Add(componentA);

        var componentB = new ComponentSave { Name = "ComponentB" };
        var defaultStateB = new StateSave { Name = "Default", ParentContainer = componentB };
        componentB.States.Add(defaultStateB);
        var myA = new InstanceSave { Name = "myA", BaseType = "ComponentA", ParentContainer = componentB };
        componentB.Instances.Add(myA);

        var varRefList = new VariableListSave<string> { Type = "string", Name = "myA.VariableReferences" };
        varRefList.Value.Add("MyVar = someOtherVar");
        defaultStateB.VariableLists.Add(varRefList);
        _project.Components.Add(componentB);

        var result = _renameLogic.GetChangesForRenamedVariable(
            componentA,
            oldFullName: "MyVar",
            oldStrippedOrExposedName: "MyVar");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Left);
    }

    [Fact]
    public void GetChangesForRenamedVariable_VariableReferenceLeftSideOnInstance_OnlyDetectedForMatchingComponent()
    {
        // ComponentA and ComponentB both have a variable named BeforeRename.
        // ComponentC has one instance of each, both with a VariableReferences entry "BeforeRename = SomeVariable".
        // Renaming BeforeRename on ComponentA should detect only instanceA's reference, not instanceB's.
        var componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });
        _project.Components.Add(componentA);

        var componentB = new ComponentSave { Name = "ComponentB" };
        componentB.States.Add(new StateSave { Name = "Default", ParentContainer = componentB });
        _project.Components.Add(componentB);

        var componentC = new ComponentSave { Name = "ComponentC" };
        var defaultStateC = new StateSave { Name = "Default", ParentContainer = componentC };
        componentC.States.Add(defaultStateC);

        var instanceA = new InstanceSave { Name = "instanceA", BaseType = "ComponentA", ParentContainer = componentC };
        componentC.Instances.Add(instanceA);
        var instanceB = new InstanceSave { Name = "instanceB", BaseType = "ComponentB", ParentContainer = componentC };
        componentC.Instances.Add(instanceB);

        var varRefListA = new VariableListSave<string> { Type = "string", Name = "instanceA.VariableReferences" };
        varRefListA.Value.Add("BeforeRename = SomeVariable");
        defaultStateC.VariableLists.Add(varRefListA);

        var varRefListB = new VariableListSave<string> { Type = "string", Name = "instanceB.VariableReferences" };
        varRefListB.Value.Add("BeforeRename = SomeVariable");
        defaultStateC.VariableLists.Add(varRefListB);

        _project.Components.Add(componentC);

        var result = _renameLogic.GetChangesForRenamedVariable(
            componentA,
            oldFullName: "BeforeRename",
            oldStrippedOrExposedName: "BeforeRename");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].VariableReferenceList.ShouldBe(varRefListA);
    }

    [Fact]
    public void GetChangesForRenamedVariable_VariableReferenceRightSideOnQualifiedName_OnlyDetectedForMatchingComponent()
    {
        // ComponentA and ComponentB both have a variable named BeforeRename.
        // ComponentC has a VariableReferences list with two entries using qualified names:
        // one referencing Components/ComponentA.BeforeRename and one referencing Components/ComponentB.BeforeRename.
        // Renaming BeforeRename on ComponentA should detect only the ComponentA entry (index 0).
        var componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });
        _project.Components.Add(componentA);

        var componentB = new ComponentSave { Name = "ComponentB" };
        componentB.States.Add(new StateSave { Name = "Default", ParentContainer = componentB });
        _project.Components.Add(componentB);

        var componentC = new ComponentSave { Name = "ComponentC" };
        var defaultStateC = new StateSave { Name = "Default", ParentContainer = componentC };
        componentC.States.Add(defaultStateC);

        var varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/ComponentA.BeforeRename");
        varRefList.Value.Add("SomeOtherVar = Components/ComponentB.BeforeRename");
        defaultStateC.VariableLists.Add(varRefList);

        _project.Components.Add(componentC);

        var result = _renameLogic.GetChangesForRenamedVariable(
            componentA,
            oldFullName: "BeforeRename",
            oldStrippedOrExposedName: "BeforeRename");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].VariableReferenceList.ShouldBe(varRefList);
        result.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
        result.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    [Fact]
    public void GetChangesForRenamedVariable_VariableReferenceRightSideOnSelf_IsDetected()
    {
        // ComponentA has a VariableReferences entry where MyVar appears on the right side:
        // "SomeOtherVar = MyVar". Renaming MyVar in ComponentA should detect this.
        var componentA = new ComponentSave { Name = "ComponentA" };
        var defaultStateA = new StateSave { Name = "Default", ParentContainer = componentA };
        componentA.States.Add(defaultStateA);

        var varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeOtherVar = MyVar");
        defaultStateA.VariableLists.Add(varRefList);
        _project.Components.Add(componentA);

        var result = _renameLogic.GetChangesForRenamedVariable(
            componentA,
            oldFullName: "MyVar",
            oldStrippedOrExposedName: "MyVar");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    [Fact]
    public void ApplyVariableRenameChanges_InstanceVariableRename_PreservesValue()
    {
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        var instanceVar = new VariableSave { Name = "myButton.Color", Value = "Red" };
        screen.DefaultState.Variables.Add(instanceVar);
        _project.Screens.Add(screen);

        var changes = _renameLogic.GetChangesForRenamedVariable(button, "Color", "Color");
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
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var buttonChild = new ComponentSave { Name = "ButtonChild", BaseType = "Button" };
        buttonChild.States.Add(new StateSave { Name = "Default", ParentContainer = buttonChild });
        var childVar = new VariableSave
        {
            Name = "InnerSprite.Color",
            ExposedAsName = "ButtonColor",
            Value = "Blue"
        };
        buttonChild.DefaultState.Variables.Add(childVar);
        _project.Components.Add(buttonChild);

        var changes = _renameLogic.GetChangesForRenamedVariable(button, "InnerSprite.Color", "ButtonColor");
        _renameLogic.ApplyVariableRenameChanges(changes, "ButtonColor", "BgColor", new HashSet<ElementSave>());

        childVar.ExposedAsName.ShouldBe("BgColor");
        childVar.Name.ShouldBe("InnerSprite.Color");
        childVar.Value.ShouldBe("Blue");
    }

    [Fact]
    public void ApplyVariableRenameChanges_InstanceVariableRename_AddsElementToNeedingSave()
    {
        var button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        var screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        var instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        screen.DefaultState.Variables.Add(new VariableSave { Name = "myButton.Color", Value = "Red" });
        _project.Screens.Add(screen);

        var elementsNeedingSave = new HashSet<ElementSave>();
        var changes = _renameLogic.GetChangesForRenamedVariable(button, "Color", "Color");
        _renameLogic.ApplyVariableRenameChanges(changes, "Color", "BackgroundColor", elementsNeedingSave);

        elementsNeedingSave.ShouldContain(screen);
    }
}
