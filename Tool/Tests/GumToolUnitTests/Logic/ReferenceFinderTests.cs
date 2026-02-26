using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;

namespace GumToolUnitTests.Logic;

public class ReferenceFinderTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly ReferenceFinder _referenceFinder;
    private readonly GumProjectSave _project;

    public ReferenceFinderTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        _referenceFinder = _mocker.CreateInstance<ReferenceFinder>();
    }

    #region GetReferencesToElement

    [Fact]
    public void GetReferencesToElement_ComponentBaseType_IsDetected()
    {
        // A component that inherits from the renamed element should appear in ElementsWithBaseTypeReference.
        ComponentSave button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ComponentSave derivedButton = new ComponentSave { Name = "DerivedButton", BaseType = "Button" };
        derivedButton.States.Add(new StateSave { Name = "Default", ParentContainer = derivedButton });
        _project.Components.Add(derivedButton);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "Button");

        changes.ElementsWithBaseTypeReference.ShouldContain(derivedButton);
    }

    [Fact]
    public void GetReferencesToElement_ContainedTypeVariable_IsDetected()
    {
        // A ContainedType variable whose value matches elementName should appear in ContainedTypeVariableReferences.
        ComponentSave button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ComponentSave listBox = new ComponentSave { Name = "ListBox" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = listBox };
        listBox.States.Add(defaultState);
        VariableSave containedTypeVar = new VariableSave { Name = "ContainedType", Value = "Button" };
        defaultState.Variables.Add(containedTypeVar);
        _project.Components.Add(listBox);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "Button");

        changes.ContainedTypeVariableReferences.Count.ShouldBe(1);
        changes.ContainedTypeVariableReferences[0].Container.ShouldBe(listBox);
        changes.ContainedTypeVariableReferences[0].Variable.ShouldBe(containedTypeVar);
    }

    [Fact]
    public void GetReferencesToElement_InstanceBaseType_IsDetected()
    {
        // An instance of the element should appear in InstancesWithBaseTypeReference
        // with the correct container.
        ComponentSave button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        _project.Screens.Add(screen);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "Button");

        changes.InstancesWithBaseTypeReference.Count.ShouldBe(1);
        changes.InstancesWithBaseTypeReference[0].Container.ShouldBe(screen);
        changes.InstancesWithBaseTypeReference[0].Instance.ShouldBe(instance);
    }

    [Fact]
    public void GetReferencesToElement_NoMatchingReferences_ReturnsEmptyLists()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "OldButtonName");

        changes.ElementsWithBaseTypeReference.ShouldBeEmpty();
        changes.InstancesWithBaseTypeReference.ShouldBeEmpty();
        changes.ContainedTypeVariableReferences.ShouldBeEmpty();
    }

    [Fact]
    public void GetReferencesToElement_ScreenBaseType_IsDetected()
    {
        // A screen that inherits from the element should appear in ElementsWithBaseTypeReference.
        ComponentSave panel = new ComponentSave { Name = "PanelNew" };
        panel.States.Add(new StateSave { Name = "Default", ParentContainer = panel });
        _project.Components.Add(panel);

        ScreenSave screen = new ScreenSave { Name = "MainScreen", BaseType = "Panel" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(panel, elementName: "Panel");

        changes.ElementsWithBaseTypeReference.ShouldContain(screen);
    }

    [Fact]
    public void GetReferencesToElement_UnrelatedBaseType_IsNotDetected()
    {
        // An element whose BaseType doesn't match elementName should not be collected.
        ComponentSave button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ComponentSave otherComponent = new ComponentSave { Name = "OtherComponent", BaseType = "SomeOtherType" };
        otherComponent.States.Add(new StateSave { Name = "Default", ParentContainer = otherComponent });
        _project.Components.Add(otherComponent);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "Button");

        changes.ElementsWithBaseTypeReference.ShouldBeEmpty();
    }

    [Fact]
    public void GetReferencesToElement_VariableReferenceEntry_IsFound()
    {
        // Per spec: GetReferencesToElement_VariableReferenceEntry_IsFound
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/Button.SomeProperty");
        defaultState.VariableLists.Add(varRefList);
        _project.Screens.Add(screen);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "Button");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(screen);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
    }

    [Fact]
    public void GetReferencesToElement_VariableReferenceRightSideMatchingComponent_IsDetected()
    {
        // A VariableReferences entry whose right side is "Components/Button.SomeProperty"
        // should be detected when the component named "Button" is the element.
        ComponentSave button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/Button.SomeProperty");
        defaultState.VariableLists.Add(varRefList);
        _project.Screens.Add(screen);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "Button");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(screen);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
    }

    [Fact]
    public void GetReferencesToElement_VariableReferenceRightSideMultipleEntries_OnlyMatchingLineIsDetected()
    {
        // A VariableReferences list with two entries — only the one whose right side
        // references "Components/Button" should be detected; the other should be ignored.
        ComponentSave button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/Button.SomeProperty");
        varRefList.Value.Add("OtherVar = Components/OtherComponent.SomeProp");
        defaultState.VariableLists.Add(varRefList);
        _project.Screens.Add(screen);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "Button");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
    }

    [Fact]
    public void GetReferencesToElement_VariableReferenceRightSideNonMatchingComponent_IsNotDetected()
    {
        // A VariableReferences entry whose right side references a different component
        // should not be detected.
        ComponentSave button = new ComponentSave { Name = "ButtonNew" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = Components/OtherComponent.SomeProperty");
        defaultState.VariableLists.Add(varRefList);
        _project.Screens.Add(screen);

        ElementRenameChanges changes = _referenceFinder.GetReferencesToElement(button, elementName: "Button");

        changes.VariableReferenceChanges.ShouldBeEmpty();
    }

    #endregion

    #region GetReferencesToState

    [Fact]
    public void GetReferencesToState_MatchingVariable_IsDetected()
    {
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

        StateRenameChanges changes = _referenceFinder.GetReferencesToState(shownState, "Shown", button, visibilityCategory);

        changes.VariablesToUpdate.Count.ShouldBe(1);
        changes.VariablesToUpdate[0].Container.ShouldBe(screen);
        changes.VariablesToUpdate[0].Variable.ShouldBe(stateVar);
    }

    [Fact]
    public void GetReferencesToState_NonMatchingValue_IsNotDetected()
    {
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
        // Value is "Hidden", not "Shown" — should not match
        screenDefault.Variables.Add(new VariableSave { Name = "myButton.VisibilityState", Value = "Hidden" });
        _project.Screens.Add(screen);

        StateRenameChanges changes = _referenceFinder.GetReferencesToState(shownState, "Shown", button, visibilityCategory);

        changes.VariablesToUpdate.ShouldBeEmpty();
    }

    [Fact]
    public void GetReferencesToState_NullCategory_UsesPlainStateVariableName()
    {
        // State not in a category — variable name should be "instanceName.State", not "instanceName.CategoryState"
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        StateSave activeState = new StateSave { Name = "Active", ParentContainer = button };
        button.States.Add(activeState);
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        InstanceSave instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        VariableSave stateVar = new VariableSave { Name = "myButton.State", Value = "Active" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        StateRenameChanges changes = _referenceFinder.GetReferencesToState(activeState, "Active", button, category: null);

        changes.VariablesToUpdate.Count.ShouldBe(1);
        changes.VariablesToUpdate[0].Variable.ShouldBe(stateVar);
    }

    #endregion

    #region GetReferencesToStateCategory

    [Fact]
    public void GetReferencesToStateCategory_InheritedElement_IsDetected()
    {
        // Button has a "Visibility" category. ButtonChild inherits from Button.
        // A screen has an instance of ButtonChild with a variable of Type "Visibility".
        // Since ButtonChild is in Button's inheritance chain, the variable should be detected.
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        StateSaveCategory visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        ComponentSave buttonChild = new ComponentSave { Name = "ButtonChild", BaseType = "Button" };
        buttonChild.States.Add(new StateSave { Name = "Default", ParentContainer = buttonChild });
        _project.Components.Add(buttonChild);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        InstanceSave instance = new InstanceSave { Name = "myButton", BaseType = "ButtonChild", ParentContainer = screen };
        screen.Instances.Add(instance);
        VariableSave stateVar = new VariableSave { Name = "myButton.VisibilityState", Type = "Visibility" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        CategoryRenameChanges changes = _referenceFinder.GetReferencesToStateCategory(button, visibilityCategory, "Visibility");

        changes.VariableChanges.Count.ShouldBe(1);
        changes.VariableChanges[0].Variable.ShouldBe(stateVar);
    }

    [Fact]
    public void GetReferencesToStateCategory_InstanceWithMatchingType_IsDetected()
    {
        // Button has a "Visibility" category.
        // A screen has an instance of Button with a variable of Type "Visibility" (the category name).
        // GetReferencesToStateCategory should detect this variable.
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        StateSaveCategory visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        InstanceSave instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        VariableSave stateVar = new VariableSave { Name = "myButton.VisibilityState", Type = "Visibility" };
        screenDefault.Variables.Add(stateVar);
        _project.Screens.Add(screen);

        CategoryRenameChanges changes = _referenceFinder.GetReferencesToStateCategory(button, visibilityCategory, "Visibility");

        changes.VariableChanges.Count.ShouldBe(1);
        changes.VariableChanges[0].Variable.ShouldBe(stateVar);
    }

    [Fact]
    public void GetReferencesToStateCategory_NonMatchingType_IsNotDetected()
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        StateSaveCategory visibilityCategory = new StateSaveCategory { Name = "Visibility" };
        button.Categories.Add(visibilityCategory);
        _project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        InstanceSave instance = new InstanceSave { Name = "myButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(instance);
        // Type is "Appearance" not "Visibility" — should not match
        screenDefault.Variables.Add(new VariableSave { Name = "myButton.AppearanceState", Type = "Appearance" });
        _project.Screens.Add(screen);

        CategoryRenameChanges changes = _referenceFinder.GetReferencesToStateCategory(button, visibilityCategory, "Visibility");

        changes.VariableChanges.ShouldBeEmpty();
    }

    #endregion

    #region GetReferencesToInstance

    [Fact]
    public void GetReferencesToInstance_ParentVariableInOtherElement_IsDetected()
    {
        // ContainerComponent has a child instance "BeforeRename".
        // TestScreen has an instance of ContainerComponent ("componentInstance") and another
        // container ("OtherContainer") whose Parent is "componentInstance.BeforeRename".
        // Finding references for "BeforeRename" in ContainerComponent should detect the Parent
        // variable in TestScreen even when the component does not have a DefaultChildContainer set.
        ComponentSave containerComponent = new ComponentSave { Name = "ContainerComponent" };
        containerComponent.States.Add(new StateSave { Name = "Default", ParentContainer = containerComponent });
        InstanceSave beforeRenameInstance = new InstanceSave { Name = "BeforeRename", BaseType = "Container", ParentContainer = containerComponent };
        containerComponent.Instances.Add(beforeRenameInstance);
        _project.Components.Add(containerComponent);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        InstanceSave componentInstance = new InstanceSave { Name = "componentInstance", BaseType = "ContainerComponent", ParentContainer = screen };
        screen.Instances.Add(componentInstance);
        InstanceSave otherContainer = new InstanceSave { Name = "OtherContainer", BaseType = "Container", ParentContainer = screen };
        screen.Instances.Add(otherContainer);
        VariableSave parentVar = new VariableSave { Name = "OtherContainer.Parent", Value = "componentInstance.BeforeRename" };
        screenDefault.Variables.Add(parentVar);
        _project.Screens.Add(screen);

        InstanceRenameChanges changes = _referenceFinder.GetReferencesToInstance(containerComponent, beforeRenameInstance, "BeforeRename");

        changes.ParentVariablesInOtherElements.Count.ShouldBe(1);
        changes.ParentVariablesInOtherElements[0].Container.ShouldBe(screen);
        changes.ParentVariablesInOtherElements[0].Variable.ShouldBe(parentVar);
    }

    [Fact]
    public void GetReferencesToInstance_SameInstanceNameInDifferentComponent_OnlyTargetInstanceIsDetected()
    {
        // ComponentA has instance "Sprite" with a VariableReferences entry "Width = Sprite.Width".
        // ComponentB also has instance "Sprite" with the same VariableReferences pattern.
        // Finding references for "Sprite" in ComponentA must only detect ComponentA's entry,
        // not ComponentB's, because instance names are scoped to their containing element.
        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        StateSave defaultStateA = new StateSave { Name = "Default", ParentContainer = componentA };
        componentA.States.Add(defaultStateA);
        InstanceSave spriteA = new InstanceSave { Name = "Sprite", BaseType = "NineSlice", ParentContainer = componentA };
        componentA.Instances.Add(spriteA);
        VariableListSave<string> varRefListA = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefListA.Value.Add("Width = Sprite.Width");
        defaultStateA.VariableLists.Add(varRefListA);
        _project.Components.Add(componentA);

        ComponentSave componentB = new ComponentSave { Name = "ComponentB" };
        StateSave defaultStateB = new StateSave { Name = "Default", ParentContainer = componentB };
        componentB.States.Add(defaultStateB);
        InstanceSave spriteB = new InstanceSave { Name = "Sprite", BaseType = "NineSlice", ParentContainer = componentB };
        componentB.Instances.Add(spriteB);
        VariableListSave<string> varRefListB = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefListB.Value.Add("Width = Sprite.Width");
        defaultStateB.VariableLists.Add(varRefListB);
        _project.Components.Add(componentB);

        InstanceRenameChanges changes = _referenceFinder.GetReferencesToInstance(componentA, spriteA, "Sprite");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].VariableReferenceList.ShouldBe(varRefListA);
    }

    [Fact]
    public void GetReferencesToInstance_VariableReferenceRightSide_CrossComponentQualifiedReference_IsDetected()
    {
        // ComponentA has instance "Sprite". ComponentB also has instance "Sprite", and its
        // Sprite.VariableReferences contains "Width = Components/ComponentA.Sprite.Width".
        // Finding references for "Sprite" in ComponentA should detect this reference in ComponentB.
        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });
        InstanceSave spriteA = new InstanceSave { Name = "Sprite", BaseType = "Sprite", ParentContainer = componentA };
        componentA.Instances.Add(spriteA);
        _project.Components.Add(componentA);

        ComponentSave componentB = new ComponentSave { Name = "ComponentB" };
        StateSave defaultStateB = new StateSave { Name = "Default", ParentContainer = componentB };
        componentB.States.Add(defaultStateB);
        InstanceSave spriteB = new InstanceSave { Name = "Sprite", BaseType = "Sprite", ParentContainer = componentB };
        componentB.Instances.Add(spriteB);
        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "Sprite.VariableReferences" };
        varRefList.Value.Add("Width = Components/ComponentA.Sprite.Width");
        defaultStateB.VariableLists.Add(varRefList);
        _project.Components.Add(componentB);

        InstanceRenameChanges changes = _referenceFinder.GetReferencesToInstance(componentA, spriteA, "Sprite");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(componentB);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
        changes.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    [Fact]
    public void GetReferencesToInstance_VariableReferenceRightSide_InInheritingElement_IsDetected()
    {
        // ComponentB has instance "ChildA".
        // DerivedComponent inherits from ComponentB and has a VariableReferences entry
        // "SomeVar = ChildA.Width". Finding references for ChildA in ComponentB should detect
        // the entry in DerivedComponent since it inherits the instance.
        ComponentSave componentB = new ComponentSave { Name = "ComponentB" };
        componentB.States.Add(new StateSave { Name = "Default", ParentContainer = componentB });
        InstanceSave childA = new InstanceSave { Name = "ChildA", BaseType = "Sprite", ParentContainer = componentB };
        componentB.Instances.Add(childA);
        _project.Components.Add(componentB);

        ComponentSave derivedComponent = new ComponentSave { Name = "DerivedComponent", BaseType = "ComponentB" };
        StateSave derivedDefault = new StateSave { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefault);
        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeVar = ChildA.Width");
        derivedDefault.VariableLists.Add(varRefList);
        _project.Components.Add(derivedComponent);

        InstanceRenameChanges changes = _referenceFinder.GetReferencesToInstance(componentB, childA, "ChildA");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(derivedComponent);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
        changes.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    [Fact]
    public void GetReferencesToInstance_VariableReferenceRightSide_SameElement_IsDetected()
    {
        // ComponentA has instances "ChildA" and "ChildB".
        // ChildB.VariableReferences has entry "Width = ChildA.Width" — ChildA is on the right side.
        // Finding references for ChildA should detect this entry as a right-side change.
        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = componentA };
        componentA.States.Add(defaultState);
        InstanceSave childA = new InstanceSave { Name = "ChildA", BaseType = "Sprite", ParentContainer = componentA };
        InstanceSave childB = new InstanceSave { Name = "ChildB", BaseType = "Sprite", ParentContainer = componentA };
        componentA.Instances.Add(childA);
        componentA.Instances.Add(childB);
        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "ChildB.VariableReferences" };
        varRefList.Value.Add("Width = ChildA.Width");
        defaultState.VariableLists.Add(varRefList);
        _project.Components.Add(componentA);

        InstanceRenameChanges changes = _referenceFinder.GetReferencesToInstance(componentA, childA, "ChildA");

        changes.VariableReferenceChanges.Count.ShouldBe(1);
        changes.VariableReferenceChanges[0].Container.ShouldBe(componentA);
        changes.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
        changes.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    #endregion

    #region GetReferencesToVariable

    [Fact]
    public void GetReferencesToVariable_VariableReferenceLeftSideOnInstance_IsDetected()
    {
        // ComponentA has a variable MyVar.
        // ComponentB has instance myA of type ComponentA with a VariableReferences entry
        // where MyVar appears on the left side: "MyVar = someOtherVar".
        // Finding references for MyVar in ComponentA should detect this as a left-side reference change.
        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });
        _project.Components.Add(componentA);

        ComponentSave componentB = new ComponentSave { Name = "ComponentB" };
        StateSave defaultStateB = new StateSave { Name = "Default", ParentContainer = componentB };
        componentB.States.Add(defaultStateB);
        InstanceSave myA = new InstanceSave { Name = "myA", BaseType = "ComponentA", ParentContainer = componentB };
        componentB.Instances.Add(myA);

        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "myA.VariableReferences" };
        varRefList.Value.Add("MyVar = someOtherVar");
        defaultStateB.VariableLists.Add(varRefList);
        _project.Components.Add(componentB);

        VariableChangeResponse result = _referenceFinder.GetReferencesToVariable(
            componentA,
            oldFullName: "MyVar",
            oldStrippedOrExposedName: "MyVar");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Left);
    }

    [Fact]
    public void GetReferencesToVariable_VariableReferenceLeftSideOnInstance_OnlyDetectedForMatchingComponent()
    {
        // ComponentA and ComponentB both have a variable named BeforeRename.
        // ComponentC has one instance of each, both with a VariableReferences entry "BeforeRename = SomeVariable".
        // Finding references for BeforeRename in ComponentA should detect only instanceA's reference, not instanceB's.
        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        componentA.States.Add(new StateSave { Name = "Default", ParentContainer = componentA });
        _project.Components.Add(componentA);

        ComponentSave componentB = new ComponentSave { Name = "ComponentB" };
        componentB.States.Add(new StateSave { Name = "Default", ParentContainer = componentB });
        _project.Components.Add(componentB);

        ComponentSave componentC = new ComponentSave { Name = "ComponentC" };
        StateSave defaultStateC = new StateSave { Name = "Default", ParentContainer = componentC };
        componentC.States.Add(defaultStateC);

        InstanceSave instanceA = new InstanceSave { Name = "instanceA", BaseType = "ComponentA", ParentContainer = componentC };
        componentC.Instances.Add(instanceA);
        InstanceSave instanceB = new InstanceSave { Name = "instanceB", BaseType = "ComponentB", ParentContainer = componentC };
        componentC.Instances.Add(instanceB);

        VariableListSave<string> varRefListA = new VariableListSave<string> { Type = "string", Name = "instanceA.VariableReferences" };
        varRefListA.Value.Add("BeforeRename = SomeVariable");
        defaultStateC.VariableLists.Add(varRefListA);

        VariableListSave<string> varRefListB = new VariableListSave<string> { Type = "string", Name = "instanceB.VariableReferences" };
        varRefListB.Value.Add("BeforeRename = SomeVariable");
        defaultStateC.VariableLists.Add(varRefListB);

        _project.Components.Add(componentC);

        VariableChangeResponse result = _referenceFinder.GetReferencesToVariable(
            componentA,
            oldFullName: "BeforeRename",
            oldStrippedOrExposedName: "BeforeRename");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].VariableReferenceList.ShouldBe(varRefListA);
    }

    [Fact]
    public void GetReferencesToVariable_VariableReferenceRightSideOnSelf_IsDetected()
    {
        // ComponentA has a VariableReferences entry where MyVar appears on the right side:
        // "SomeOtherVar = MyVar". Finding references for MyVar in ComponentA should detect this.
        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        StateSave defaultStateA = new StateSave { Name = "Default", ParentContainer = componentA };
        componentA.States.Add(defaultStateA);

        VariableListSave<string> varRefList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varRefList.Value.Add("SomeOtherVar = MyVar");
        defaultStateA.VariableLists.Add(varRefList);
        _project.Components.Add(componentA);

        VariableChangeResponse result = _referenceFinder.GetReferencesToVariable(
            componentA,
            oldFullName: "MyVar",
            oldStrippedOrExposedName: "MyVar");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    #endregion
}
