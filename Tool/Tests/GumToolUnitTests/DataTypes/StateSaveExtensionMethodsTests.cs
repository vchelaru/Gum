using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.DataTypes;

public class StateSaveExtensionMethodsTests : BaseTestClass
{
    public StateSaveExtensionMethodsTests()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
    }

    [Fact]
    public void GetValueRecursive_OnDefaultState_ShouldWalkInheritanceForVariableLists()
    {
        // Repro for the inspector bug: when a derived component's DefaultState
        // is asked for an instance's VariableReferences value, GetValueRecursive
        // must walk up to the base component's matching VariableListSave.
        // Currently it short-circuits because the guard at
        // StateSaveExtensionMethods.cs line 65 skips the VariableList lookup
        // for default states, so the inspector sees an empty list.

        ComponentSave baseComponent = new() { Name = "BaseComponent", BaseType = "Container" };
        StateSave baseDefaultState = new() { Name = "Default", ParentContainer = baseComponent };
        baseComponent.States.Add(baseDefaultState);

        InstanceSave textInBase = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = baseComponent };
        baseComponent.Instances.Add(textInBase);

        VariableListSave<string> baseVarRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        baseVarRefs.ValueAsIList.Add("Red = SomeOtherInstance.Red");
        baseDefaultState.VariableLists.Add(baseVarRefs);

        ObjectFinder.Self.GumProjectSave!.Components.Add(baseComponent);

        ComponentSave derivedComponent = new() { Name = "DerivedComponent", BaseType = "BaseComponent" };
        StateSave derivedDefaultState = new() { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefaultState);

        InstanceSave textInDerived = new()
        {
            Name = "TextInstance",
            BaseType = "Text",
            DefinedByBase = true,
            ParentContainer = derivedComponent
        };
        derivedComponent.Instances.Add(textInDerived);

        ObjectFinder.Self.GumProjectSave.Components.Add(derivedComponent);

        var value = derivedDefaultState.GetValueRecursive("TextInstance.VariableReferences");

        value.ShouldNotBeNull(
            "because BaseComponent.DefaultState has TextInstance.VariableReferences set; " +
            "DerivedComponent inherits via BaseType='BaseComponent', and the recursive " +
            "lookup must walk to the base component's VariableListSave.");

        var asList = value as IList;
        asList.ShouldNotBeNull();
        asList!.Count.ShouldBe(1);
        asList[0].ShouldBe("Red = SomeOtherInstance.Red");
    }

    [Fact]
    public void GetValueRecursive_OnDefaultState_LocallySetVariableList_ShouldWinOverInherited()
    {
        // A local VariableListSave on the derived component must take precedence
        // over the base's value. Pins precedence so the fix doesn't unconditionally
        // walk past a local override.

        ComponentSave baseComponent = new() { Name = "BaseComponentForLocal", BaseType = "Container" };
        StateSave baseDefaultState = new() { Name = "Default", ParentContainer = baseComponent };
        baseComponent.States.Add(baseDefaultState);

        InstanceSave textInBase = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = baseComponent };
        baseComponent.Instances.Add(textInBase);

        VariableListSave<string> baseVarRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        baseVarRefs.ValueAsIList.Add("Red = Base.Red");
        baseDefaultState.VariableLists.Add(baseVarRefs);

        ObjectFinder.Self.GumProjectSave!.Components.Add(baseComponent);

        ComponentSave derivedComponent = new() { Name = "DerivedComponentForLocal", BaseType = "BaseComponentForLocal" };
        StateSave derivedDefaultState = new() { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefaultState);

        InstanceSave textInDerived = new()
        {
            Name = "TextInstance",
            BaseType = "Text",
            DefinedByBase = true,
            ParentContainer = derivedComponent
        };
        derivedComponent.Instances.Add(textInDerived);

        VariableListSave<string> derivedVarRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        derivedVarRefs.ValueAsIList.Add("Red = Derived.Red");
        derivedDefaultState.VariableLists.Add(derivedVarRefs);

        ObjectFinder.Self.GumProjectSave.Components.Add(derivedComponent);

        var value = derivedDefaultState.GetValueRecursive("TextInstance.VariableReferences");

        var asList = value as IList;
        asList.ShouldNotBeNull();
        asList!.Count.ShouldBe(1);
        asList[0].ShouldBe("Red = Derived.Red",
            "because a locally-set VariableList must take precedence over the inherited base value.");
    }

    [Fact]
    public void GetValueRecursive_OnCategorizedState_ShouldStillFindInheritedVariableList()
    {
        // The fix must not regress the existing non-default-state path. A
        // categorized state on the derived component should still be able to
        // walk through its element's default → base chain for VariableLists.

        ComponentSave baseComponent = new() { Name = "BaseComponentCat", BaseType = "Container" };
        StateSave baseDefaultState = new() { Name = "Default", ParentContainer = baseComponent };
        baseComponent.States.Add(baseDefaultState);

        InstanceSave textInBase = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = baseComponent };
        baseComponent.Instances.Add(textInBase);

        VariableListSave<string> baseVarRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        baseVarRefs.ValueAsIList.Add("Red = SomeOtherInstance.Red");
        baseDefaultState.VariableLists.Add(baseVarRefs);

        ObjectFinder.Self.GumProjectSave!.Components.Add(baseComponent);

        ComponentSave derivedComponent = new() { Name = "DerivedComponentCat", BaseType = "BaseComponentCat" };
        StateSave derivedDefaultState = new() { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefaultState);

        InstanceSave textInDerived = new()
        {
            Name = "TextInstance",
            BaseType = "Text",
            DefinedByBase = true,
            ParentContainer = derivedComponent
        };
        derivedComponent.Instances.Add(textInDerived);

        StateSaveCategory category = new() { Name = "Visibility" };
        StateSave hoveredState = new() { Name = "Hovered", ParentContainer = derivedComponent };
        category.States.Add(hoveredState);
        derivedComponent.Categories.Add(category);

        ObjectFinder.Self.GumProjectSave.Components.Add(derivedComponent);

        var value = hoveredState.GetValueRecursive("TextInstance.VariableReferences");

        var asList = value as IList;
        asList.ShouldNotBeNull(
            "because the categorized state falls back to its element's default state, " +
            "which inherits from the base component.");
        asList!.Count.ShouldBe(1);
        asList[0].ShouldBe("Red = SomeOtherInstance.Red");
    }

    [Fact]
    public void GetValueRecursive_OnDefaultState_NoInheritedVariableList_ShouldReturnNullOrEmpty()
    {
        // A component with no base that defines the list, and an instance whose
        // standard type has only the standard's empty VariableReferences row,
        // should not blow up. (Standard Text exposes an empty VariableReferences
        // entry; the lookup may legitimately return that empty list or null,
        // but it must not throw.)

        ComponentSave component = new() { Name = "PlainComponent", BaseType = "Container" };
        StateSave defaultState = new() { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);

        InstanceSave text = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = component };
        component.Instances.Add(text);

        ObjectFinder.Self.GumProjectSave!.Components.Add(component);

        // Just ensure no exception is thrown — the value itself can be null or
        // an empty list. The relevant assertion is that no items leak through
        // from somewhere they shouldn't.
        var value = defaultState.GetValueRecursive("TextInstance.VariableReferences");

        if (value is IList list)
        {
            list.Count.ShouldBe(0);
        }
    }

    [Fact]
    public void GetVariableListRecursive_OnDefaultState_ShouldFindInheritedList()
    {
        // Sanity check for the lower-level helper: confirms the
        // VariableList-only walk works for default states. GetValueRecursive
        // should be calling this; the test above pins that it does.

        ComponentSave baseComponent = new() { Name = "BaseComponent2", BaseType = "Container" };
        StateSave baseDefaultState = new() { Name = "Default", ParentContainer = baseComponent };
        baseComponent.States.Add(baseDefaultState);

        InstanceSave textInBase = new() { Name = "TextInstance", BaseType = "Text", ParentContainer = baseComponent };
        baseComponent.Instances.Add(textInBase);

        VariableListSave<string> baseVarRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        baseVarRefs.ValueAsIList.Add("Red = SomeOtherInstance.Red");
        baseDefaultState.VariableLists.Add(baseVarRefs);

        ObjectFinder.Self.GumProjectSave!.Components.Add(baseComponent);

        ComponentSave derivedComponent = new() { Name = "DerivedComponent2", BaseType = "BaseComponent2" };
        StateSave derivedDefaultState = new() { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefaultState);

        InstanceSave textInDerived = new()
        {
            Name = "TextInstance",
            BaseType = "Text",
            DefinedByBase = true,
            ParentContainer = derivedComponent
        };
        derivedComponent.Instances.Add(textInDerived);

        ObjectFinder.Self.GumProjectSave.Components.Add(derivedComponent);

        var foundList = derivedDefaultState.GetVariableListRecursive("TextInstance.VariableReferences");

        foundList.ShouldNotBeNull();
        foundList!.ValueAsIList.Count.ShouldBe(1);
        foundList.ValueAsIList[0].ShouldBe("Red = SomeOtherInstance.Red");
    }
}
