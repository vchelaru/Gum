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
