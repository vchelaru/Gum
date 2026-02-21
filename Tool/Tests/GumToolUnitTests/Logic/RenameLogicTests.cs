using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
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

    [Fact]
    public void GetVariableChangesForRenamedVariable_VariableReferenceLeftSideOnInstance_IsDetected()
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

        var result = _renameLogic.GetVariableChangesForRenamedVariable(
            componentA,
            oldFullName: "MyVar",
            oldStrippedOrExposedName: "MyVar");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Left);
    }

    [Fact]
    public void GetVariableChangesForRenamedVariable_VariableReferenceLeftSideOnInstance_OnlyDetectedForMatchingComponent()
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

        var result = _renameLogic.GetVariableChangesForRenamedVariable(
            componentA,
            oldFullName: "BeforeRename",
            oldStrippedOrExposedName: "BeforeRename");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].VariableReferenceList.ShouldBe(varRefListA);
    }

    [Fact]
    public void GetVariableChangesForRenamedVariable_VariableReferenceRightSideOnQualifiedName_OnlyDetectedForMatchingComponent()
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

        var result = _renameLogic.GetVariableChangesForRenamedVariable(
            componentA,
            oldFullName: "BeforeRename",
            oldStrippedOrExposedName: "BeforeRename");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].VariableReferenceList.ShouldBe(varRefList);
        result.VariableReferenceChanges[0].LineIndex.ShouldBe(0);
        result.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    [Fact]
    public void GetVariableChangesForRenamedVariable_VariableReferenceRightSideOnSelf_IsDetected()
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

        var result = _renameLogic.GetVariableChangesForRenamedVariable(
            componentA,
            oldFullName: "MyVar",
            oldStrippedOrExposedName: "MyVar");

        result.VariableReferenceChanges.Count.ShouldBe(1);
        result.VariableReferenceChanges[0].ChangedSide.ShouldBe(SideOfEquals.Right);
    }

    [Fact]
    public void PropagateVariableRename_InstanceVariableRename_PreservesValue()
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

        _renameLogic.PropagateVariableRename(
            button,
            variableFullName: "Color",
            oldStrippedOrExposedName: "Color",
            newStrippedOrExposedName: "BackgroundColor",
            elementsNeedingSave: new HashSet<ElementSave>());

        instanceVar.Name.ShouldBe("myButton.BackgroundColor");
        instanceVar.Value.ShouldBe("Red");
    }

    [Fact]
    public void PropagateVariableRename_ExposedVariableRenameInInheritingComponent_PreservesNameAndValue()
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

        _renameLogic.PropagateVariableRename(
            button,
            variableFullName: "InnerSprite.Color",
            oldStrippedOrExposedName: "ButtonColor",
            newStrippedOrExposedName: "BgColor",
            elementsNeedingSave: new HashSet<ElementSave>());

        childVar.ExposedAsName.ShouldBe("BgColor");
        childVar.Name.ShouldBe("InnerSprite.Color");
        childVar.Value.ShouldBe("Blue");
    }

    [Fact]
    public void PropagateVariableRename_InstanceVariableRename_AddsElementToNeedingSave()
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
        _renameLogic.PropagateVariableRename(
            button,
            variableFullName: "Color",
            oldStrippedOrExposedName: "Color",
            newStrippedOrExposedName: "BackgroundColor",
            elementsNeedingSave: elementsNeedingSave);

        elementsNeedingSave.ShouldContain(screen);
    }
}
