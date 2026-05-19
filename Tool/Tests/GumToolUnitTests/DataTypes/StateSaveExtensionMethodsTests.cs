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
    public void GetVariableListRecursive_OnInstance_ShouldPreferCategorizedStateOverBaseTypeList()
    {
        // Repro for the H1 / TextCategoryState case:
        // - Owner derives from Button. TextInstance is DefinedByBase=true.
        // - Button.DefaultState defines TextInstance.VariableReferences (the
        //   "base-default" references — what the lookup currently returns).
        // - TextInstance's BaseType is Label. Label has a TextCategory state
        //   category with an H1 state that has its own VariableReferences.
        // - Owner.DefaultState sets TextInstance.TextCategoryState = "H1".
        //
        // Per the "most specific wins" principle, the lookup should return
        // Label.H1's VariableReferences, not Button's.

        ComponentSave label = new() { Name = "Label", BaseType = "Container" };
        StateSave labelDefault = new() { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);

        StateSaveCategory textCategory = new() { Name = "TextCategory" };
        StateSave h1State = new() { Name = "H1", ParentContainer = label };
        VariableListSave<string> h1Refs = new()
        {
            Name = "VariableReferences",
            Type = "string"
        };
        h1Refs.ValueAsIList.Add("Font = Styles.H1.Font");
        h1Refs.ValueAsIList.Add("FontSize = Styles.H1.FontSize");
        h1State.VariableLists.Add(h1Refs);
        textCategory.States.Add(h1State);
        label.Categories.Add(textCategory);

        ObjectFinder.Self.GumProjectSave!.Components.Add(label);

        ComponentSave button = new() { Name = "Button", BaseType = "Container" };
        StateSave buttonDefault = new() { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);

        InstanceSave textInButton = new()
        {
            Name = "TextInstance",
            BaseType = "Label",
            ParentContainer = button
        };
        button.Instances.Add(textInButton);

        VariableListSave<string> buttonRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        buttonRefs.ValueAsIList.Add("Red = Styles.Black.Red");
        buttonRefs.ValueAsIList.Add("Font = Styles.Strong.Font");
        buttonDefault.VariableLists.Add(buttonRefs);

        ObjectFinder.Self.GumProjectSave.Components.Add(button);

        ComponentSave owner = new() { Name = "Owner", BaseType = "Button" };
        StateSave ownerDefault = new() { Name = "Default", ParentContainer = owner };
        owner.States.Add(ownerDefault);

        InstanceSave textInOwner = new()
        {
            Name = "TextInstance",
            BaseType = "Label",
            DefinedByBase = true,
            ParentContainer = owner
        };
        owner.Instances.Add(textInOwner);

        // Set the categorized state on the instance.
        VariableSave categoryStateVar = new()
        {
            Name = "TextInstance.TextCategoryState",
            Type = "TextCategory",
            Value = "H1",
            SetsValue = true
        };
        ownerDefault.Variables.Add(categoryStateVar);

        ObjectFinder.Self.GumProjectSave.Components.Add(owner);

        var foundList = ownerDefault.GetVariableListRecursive("TextInstance.VariableReferences");

        foundList.ShouldNotBeNull();
        foundList!.ValueAsIList.Count.ShouldBe(2);
        foundList.ValueAsIList[0].ShouldBe("Font = Styles.H1.Font",
            "because the H1 state's VariableReferences is more specific than Button's base " +
            "TextInstance.VariableReferences and should win when TextCategoryState=H1.");
        foundList.ValueAsIList[1].ShouldBe("FontSize = Styles.H1.FontSize");
    }

    [Fact]
    public void GetVariableListRecursive_OnInstance_ShouldFindCategorizedStateOnInstanceTypesBaseType()
    {
        // Repro from BubbleGum5: BaseComponent defines a category whose state
        // has VariableReferences. DerivedComponent inherits from BaseComponent
        // with no overrides. A screen has an instance of DerivedComponent and
        // assigns DerivedComponentInstance.Category1State = "State1".
        //
        // The lookup must reach BaseComponent.Category1.State1.VariableReferences
        // because Category1 lives on BaseComponent, not DerivedComponent.
        // Previously this failed because the categorized-instance walk used
        // instanceType.AllStates, which doesn't include states inherited via
        // the instance type's own BaseType chain.

        ComponentSave baseComponent = new() { Name = "BaseComponentDerivedCat", BaseType = "Container" };
        StateSave baseDefault = new() { Name = "Default", ParentContainer = baseComponent };
        baseComponent.States.Add(baseDefault);

        StateSaveCategory category = new() { Name = "Category1" };
        StateSave state1 = new() { Name = "State1", ParentContainer = baseComponent };
        VariableListSave<string> state1Refs = new()
        {
            Name = "VariableReferences",
            Type = "string"
        };
        state1Refs.ValueAsIList.Add("X = SomeOtherInstance.X");
        state1.VariableLists.Add(state1Refs);
        category.States.Add(state1);
        baseComponent.Categories.Add(category);

        ObjectFinder.Self.GumProjectSave!.Components.Add(baseComponent);

        ComponentSave derivedComponent = new() { Name = "DerivedComponentDerivedCat", BaseType = "BaseComponentDerivedCat" };
        StateSave derivedDefault = new() { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefault);

        ObjectFinder.Self.GumProjectSave.Components.Add(derivedComponent);

        ScreenSave screen = new() { Name = "MyScreenForDerivedCat" };
        StateSave screenDefault = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave derivedInstance = new()
        {
            Name = "DerivedComponentInstance",
            BaseType = "DerivedComponentDerivedCat",
            ParentContainer = screen
        };
        screen.Instances.Add(derivedInstance);

        VariableSave categoryStateVar = new()
        {
            Name = "DerivedComponentInstance.Category1State",
            Type = "Category1",
            Value = "State1",
            SetsValue = true
        };
        screenDefault.Variables.Add(categoryStateVar);

        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        var foundList = screenDefault.GetVariableListRecursive("DerivedComponentInstance.VariableReferences");

        foundList.ShouldNotBeNull(
            "because Category1.State1 on BaseComponent has VariableReferences set, " +
            "and the lookup must walk through DerivedComponent's BaseType chain to reach it.");
        foundList!.ValueAsIList.Count.ShouldBe(1);
        foundList.ValueAsIList[0].ShouldBe("X = SomeOtherInstance.X");
    }

    [Fact]
    public void GetValueRecursive_OnInstance_ShouldFindCategorizedScalarOnInstanceTypesBaseType()
    {
        // Scalar analogue of GetVariableListRecursive_OnInstance_ShouldFindCategorizedStateOnInstanceTypesBaseType.
        // BaseComponent defines Category1 with State1, which has a materialized X=100
        // (e.g. propagated from a VariableReferences row by the tool).
        // DerivedComponent inherits with no overrides. A screen has a
        // DerivedComponentInstance and sets Category1State="State1". The scalar
        // value for X must be 100, found by walking the instance type's BaseType chain.

        ComponentSave baseComponent = new() { Name = "BaseComponentScalarCat", BaseType = "Container" };
        StateSave baseDefault = new() { Name = "Default", ParentContainer = baseComponent };
        baseComponent.States.Add(baseDefault);

        StateSaveCategory category = new() { Name = "Category1" };
        StateSave state1 = new() { Name = "State1", ParentContainer = baseComponent };
        state1.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });
        category.States.Add(state1);
        baseComponent.Categories.Add(category);

        ObjectFinder.Self.GumProjectSave!.Components.Add(baseComponent);

        ComponentSave derivedComponent = new() { Name = "DerivedComponentScalarCat", BaseType = "BaseComponentScalarCat" };
        StateSave derivedDefault = new() { Name = "Default", ParentContainer = derivedComponent };
        derivedComponent.States.Add(derivedDefault);

        ObjectFinder.Self.GumProjectSave.Components.Add(derivedComponent);

        ScreenSave screen = new() { Name = "MyScreenForScalarCat" };
        StateSave screenDefault = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave derivedInstance = new()
        {
            Name = "DerivedComponentInstance",
            BaseType = "DerivedComponentScalarCat",
            ParentContainer = screen
        };
        screen.Instances.Add(derivedInstance);

        screenDefault.Variables.Add(new VariableSave
        {
            Name = "DerivedComponentInstance.Category1State",
            Type = "Category1",
            Value = "State1",
            SetsValue = true
        });

        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        var value = screenDefault.GetValueRecursive("DerivedComponentInstance.X");

        value.ShouldBe(100f,
            "because Category1.State1 on BaseComponent sets X=100, and the lookup must walk " +
            "through DerivedComponent's BaseType chain to reach it.");
    }

    [Fact]
    public void GetVariableListRecursive_OnInstance_NoCategorizedStateSet_ShouldFallBackToBaseTypeList()
    {
        // Regression guard: when no categorized state is active on the
        // instance, the lookup should still walk the containing element's
        // BaseType chain and return Button's TextInstance.VariableReferences.

        ComponentSave label = new() { Name = "LabelNoCat", BaseType = "Container" };
        StateSave labelDefault = new() { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);

        ObjectFinder.Self.GumProjectSave!.Components.Add(label);

        ComponentSave button = new() { Name = "ButtonNoCat", BaseType = "Container" };
        StateSave buttonDefault = new() { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);

        InstanceSave textInButton = new()
        {
            Name = "TextInstance",
            BaseType = "LabelNoCat",
            ParentContainer = button
        };
        button.Instances.Add(textInButton);

        VariableListSave<string> buttonRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        buttonRefs.ValueAsIList.Add("Red = Styles.Black.Red");
        buttonDefault.VariableLists.Add(buttonRefs);

        ObjectFinder.Self.GumProjectSave.Components.Add(button);

        ComponentSave owner = new() { Name = "OwnerNoCat", BaseType = "ButtonNoCat" };
        StateSave ownerDefault = new() { Name = "Default", ParentContainer = owner };
        owner.States.Add(ownerDefault);

        InstanceSave textInOwner = new()
        {
            Name = "TextInstance",
            BaseType = "LabelNoCat",
            DefinedByBase = true,
            ParentContainer = owner
        };
        owner.Instances.Add(textInOwner);

        ObjectFinder.Self.GumProjectSave.Components.Add(owner);

        var foundList = ownerDefault.GetVariableListRecursive("TextInstance.VariableReferences");

        foundList.ShouldNotBeNull();
        foundList!.ValueAsIList.Count.ShouldBe(1);
        foundList.ValueAsIList[0].ShouldBe("Red = Styles.Black.Red",
            "because no categorized state is set on TextInstance, so the lookup falls back to " +
            "the base type's TextInstance.VariableReferences.");
    }

    [Fact]
    public void GetVariableListRecursive_OnInstance_CategorizedStateWithoutList_ShouldFallBackToBaseTypeList()
    {
        // Regression guard: if a categorized state is active but that state
        // doesn't have a VariableReferences list, the lookup should still
        // fall back to the base type's list.

        ComponentSave label = new() { Name = "LabelEmptyCat", BaseType = "Container" };
        StateSave labelDefault = new() { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);

        StateSaveCategory textCategory = new() { Name = "TextCategory" };
        StateSave h1State = new() { Name = "H1", ParentContainer = label };
        // Note: H1 does NOT have a VariableReferences list.
        textCategory.States.Add(h1State);
        label.Categories.Add(textCategory);

        ObjectFinder.Self.GumProjectSave!.Components.Add(label);

        ComponentSave button = new() { Name = "ButtonEmptyCat", BaseType = "Container" };
        StateSave buttonDefault = new() { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);

        InstanceSave textInButton = new()
        {
            Name = "TextInstance",
            BaseType = "LabelEmptyCat",
            ParentContainer = button
        };
        button.Instances.Add(textInButton);

        VariableListSave<string> buttonRefs = new()
        {
            Name = "TextInstance.VariableReferences",
            Type = "string"
        };
        buttonRefs.ValueAsIList.Add("Red = Styles.Black.Red");
        buttonDefault.VariableLists.Add(buttonRefs);

        ObjectFinder.Self.GumProjectSave.Components.Add(button);

        ComponentSave owner = new() { Name = "OwnerEmptyCat", BaseType = "ButtonEmptyCat" };
        StateSave ownerDefault = new() { Name = "Default", ParentContainer = owner };
        owner.States.Add(ownerDefault);

        InstanceSave textInOwner = new()
        {
            Name = "TextInstance",
            BaseType = "LabelEmptyCat",
            DefinedByBase = true,
            ParentContainer = owner
        };
        owner.Instances.Add(textInOwner);

        VariableSave categoryStateVar = new()
        {
            Name = "TextInstance.TextCategoryState",
            Type = "TextCategory",
            Value = "H1",
            SetsValue = true
        };
        ownerDefault.Variables.Add(categoryStateVar);

        ObjectFinder.Self.GumProjectSave.Components.Add(owner);

        var foundList = ownerDefault.GetVariableListRecursive("TextInstance.VariableReferences");

        foundList.ShouldNotBeNull();
        foundList!.ValueAsIList.Count.ShouldBe(1);
        foundList.ValueAsIList[0].ShouldBe("Red = Styles.Black.Red",
            "because the categorized state H1 has no VariableReferences list, the lookup " +
            "should fall back to the base type's list.");
    }

    [Fact]
    public void GetValueRecursive_OnInstance_CategorizedStateInheritedFromBase_ShouldFallBackToDerivedDefaults()
    {
        // Repro for the QuestToCompileGum BigButton bug:
        // - Button defines ButtonCategory.Enabled (no Width override) and DefaultState Width=128.
        // - BigButton inherits from Button and overrides Width=158 in its DefaultState.
        // - Screen has BigButtonInstance (BaseType=BigButton) with no Width set, and sets
        //   BigButtonInstance.ButtonCategoryState = "Enabled".
        //
        // The "Enabled" state's ParentContainer is Button (the state is inherited, not
        // redefined on BigButton). The categorized-instance walk previously called
        // matchingState.GetValueRecursive("Width"), which walked Button's defaults and
        // returned 128 — silently bypassing BigButton.DefaultState's override of 158.
        // The correct fallback chain is the instance type's own defaults (BigButton →
        // Button), not the state-defining type's defaults.

        ComponentSave button = new() { Name = "ButtonInheritedCat", BaseType = "Container" };
        StateSave buttonDefault = new() { Name = "Default", ParentContainer = button };
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "Width",
            Type = "float",
            Value = 128f,
            SetsValue = true
        });
        button.States.Add(buttonDefault);

        StateSaveCategory buttonCategory = new() { Name = "ButtonCategory" };
        StateSave enabledState = new() { Name = "Enabled", ParentContainer = button };
        // Enabled deliberately does NOT set Width.
        buttonCategory.States.Add(enabledState);
        button.Categories.Add(buttonCategory);

        ObjectFinder.Self.GumProjectSave!.Components.Add(button);

        ComponentSave bigButton = new() { Name = "BigButtonInheritedCat", BaseType = "ButtonInheritedCat" };
        StateSave bigButtonDefault = new() { Name = "Default", ParentContainer = bigButton };
        bigButtonDefault.Variables.Add(new VariableSave
        {
            Name = "Width",
            Type = "float",
            Value = 158f,
            SetsValue = true
        });
        bigButton.States.Add(bigButtonDefault);

        ObjectFinder.Self.GumProjectSave.Components.Add(bigButton);

        ScreenSave screen = new() { Name = "MyScreenForBigButton" };
        StateSave screenDefault = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave bigButtonInstance = new()
        {
            Name = "BigButtonInstance",
            BaseType = "BigButtonInheritedCat",
            ParentContainer = screen
        };
        screen.Instances.Add(bigButtonInstance);

        screenDefault.Variables.Add(new VariableSave
        {
            Name = "BigButtonInstance.ButtonCategoryState",
            Type = "ButtonCategory",
            Value = "Enabled",
            SetsValue = true
        });

        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        var value = screenDefault.GetValueRecursive("BigButtonInstance.Width");

        value.ShouldBe(158f,
            "because the Enabled categorized state does not set Width, so the fallback " +
            "must walk the instance type's own defaults (BigButton.DefaultState Width=158), " +
            "not the state-defining type's defaults (Button.DefaultState Width=128).");
    }

    [Fact]
    public void GetValueRecursive_OnInstance_CategorizedStateSetsScalar_ShouldWinOverBaseDefaults()
    {
        // Scalar analogue of GetVariableListRecursive_OnInstance_ShouldPreferCategorizedStateOverBaseTypeList.
        // When the categorized state itself sets the variable, that value wins over any
        // default-chain value. Pins the non-recursive pass at line ~138 of
        // GetValueRecursive — the path the fix relies on for "state actually sets X."

        ComponentSave button = new() { Name = "ButtonMostSpecificScalar", BaseType = "Container" };
        StateSave buttonDefault = new() { Name = "Default", ParentContainer = button };
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 128f,
            SetsValue = true
        });
        button.States.Add(buttonDefault);

        StateSaveCategory buttonCategory = new() { Name = "ButtonCategory" };
        StateSave enabledState = new() { Name = "Enabled", ParentContainer = button };
        enabledState.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 999f,
            SetsValue = true
        });
        buttonCategory.States.Add(enabledState);
        button.Categories.Add(buttonCategory);

        ObjectFinder.Self.GumProjectSave!.Components.Add(button);

        ScreenSave screen = new() { Name = "MyScreenForMostSpecificScalar" };
        StateSave screenDefault = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave buttonInstance = new()
        {
            Name = "ButtonInstance",
            BaseType = "ButtonMostSpecificScalar",
            ParentContainer = screen
        };
        screen.Instances.Add(buttonInstance);

        screenDefault.Variables.Add(new VariableSave
        {
            Name = "ButtonInstance.ButtonCategoryState",
            Type = "ButtonCategory",
            Value = "Enabled",
            SetsValue = true
        });

        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        var value = screenDefault.GetValueRecursive("ButtonInstance.X");

        value.ShouldBe(999f,
            "because the Enabled categorized state sets X=999, which is more specific than " +
            "Button.DefaultState X=128.");
    }

    [Fact]
    public void GetValueRecursive_OnInstance_NoCategorizedState_DerivedDefaultShouldWinOverBaseDefault()
    {
        // Scalar analogue of GetValueRecursive_OnDefaultState_LocallySetVariableList_ShouldWinOverInherited.
        // BigButton overrides Button's X in its own DefaultState. With no categorized state
        // active on the instance, the lookup must walk BigButton.DefaultState → Button.DefaultState
        // and return BigButton's override. This is the fallback path the BigButton fix depends on.

        ComponentSave button = new() { Name = "ButtonFallbackChain", BaseType = "Container" };
        StateSave buttonDefault = new() { Name = "Default", ParentContainer = button };
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 128f,
            SetsValue = true
        });
        button.States.Add(buttonDefault);

        ObjectFinder.Self.GumProjectSave!.Components.Add(button);

        ComponentSave bigButton = new() { Name = "BigButtonFallbackChain", BaseType = "ButtonFallbackChain" };
        StateSave bigButtonDefault = new() { Name = "Default", ParentContainer = bigButton };
        bigButtonDefault.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 158f,
            SetsValue = true
        });
        bigButton.States.Add(bigButtonDefault);

        ObjectFinder.Self.GumProjectSave.Components.Add(bigButton);

        ScreenSave screen = new() { Name = "MyScreenForFallbackChain" };
        StateSave screenDefault = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave bigButtonInstance = new()
        {
            Name = "BigButtonInstance",
            BaseType = "BigButtonFallbackChain",
            ParentContainer = screen
        };
        screen.Instances.Add(bigButtonInstance);

        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        var value = screenDefault.GetValueRecursive("BigButtonInstance.X");

        value.ShouldBe(158f,
            "because BigButton.DefaultState overrides X=158 over Button's X=128, and with no " +
            "categorized state active the lookup must walk the instance type's own chain.");
    }

    [Fact]
    public void GetValueRecursive_OnInstance_CategorizedStateRedefinedAtMultipleLevels_DerivedWins()
    {
        // BigButton redefines Button.ButtonCategory.Enabled with its own X value.
        // GetStateSaveRecursively returns the derived element's match first (it walks
        // AllStates on the derived element before recursing into BaseType), so the
        // derived component's state wins. Pins that behavior — a regression would
        // silently return Button's value through a BigButton instance.

        ComponentSave button = new() { Name = "ButtonRedefinedState", BaseType = "Container" };
        StateSave buttonDefault = new() { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);

        StateSaveCategory buttonCategory = new() { Name = "ButtonCategory" };
        StateSave buttonEnabled = new() { Name = "Enabled", ParentContainer = button };
        buttonEnabled.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 500f,
            SetsValue = true
        });
        buttonCategory.States.Add(buttonEnabled);
        button.Categories.Add(buttonCategory);

        ObjectFinder.Self.GumProjectSave!.Components.Add(button);

        ComponentSave bigButton = new() { Name = "BigButtonRedefinedState", BaseType = "ButtonRedefinedState" };
        StateSave bigButtonDefault = new() { Name = "Default", ParentContainer = bigButton };
        bigButton.States.Add(bigButtonDefault);

        StateSaveCategory bigButtonCategory = new() { Name = "ButtonCategory" };
        StateSave bigButtonEnabled = new() { Name = "Enabled", ParentContainer = bigButton };
        bigButtonEnabled.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 999f,
            SetsValue = true
        });
        bigButtonCategory.States.Add(bigButtonEnabled);
        bigButton.Categories.Add(bigButtonCategory);

        ObjectFinder.Self.GumProjectSave.Components.Add(bigButton);

        ScreenSave screen = new() { Name = "MyScreenForRedefinedState" };
        StateSave screenDefault = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave bigButtonInstance = new()
        {
            Name = "BigButtonInstance",
            BaseType = "BigButtonRedefinedState",
            ParentContainer = screen
        };
        screen.Instances.Add(bigButtonInstance);

        screenDefault.Variables.Add(new VariableSave
        {
            Name = "BigButtonInstance.ButtonCategoryState",
            Type = "ButtonCategory",
            Value = "Enabled",
            SetsValue = true
        });

        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        var value = screenDefault.GetValueRecursive("BigButtonInstance.X");

        value.ShouldBe(999f,
            "because BigButton redefines ButtonCategory.Enabled with X=999, and the derived " +
            "component's state must win over the base's X=500.");
    }

    [Fact]
    public void GetValueRecursive_OnInstance_CategorizedStateInheritedFromBase_FallbackChainContinuesToBaseDefaults()
    {
        // Chain-continuation guard for the BigButton fix: when the categorized state is
        // inherited and doesn't set X, AND BigButton.DefaultState also doesn't set X, the
        // fallback must continue walking past BigButton to Button.DefaultState X=128.
        // The earlier test only proves BigButton's override is seen; this proves the chain
        // doesn't stop at BigButton.

        ComponentSave button = new() { Name = "ButtonChainContinues", BaseType = "Container" };
        StateSave buttonDefault = new() { Name = "Default", ParentContainer = button };
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 128f,
            SetsValue = true
        });
        button.States.Add(buttonDefault);

        StateSaveCategory buttonCategory = new() { Name = "ButtonCategory" };
        StateSave enabledState = new() { Name = "Enabled", ParentContainer = button };
        // Enabled deliberately does NOT set X.
        buttonCategory.States.Add(enabledState);
        button.Categories.Add(buttonCategory);

        ObjectFinder.Self.GumProjectSave!.Components.Add(button);

        // BigButton has no X override and no redefined category state.
        ComponentSave bigButton = new() { Name = "BigButtonChainContinues", BaseType = "ButtonChainContinues" };
        StateSave bigButtonDefault = new() { Name = "Default", ParentContainer = bigButton };
        bigButton.States.Add(bigButtonDefault);

        ObjectFinder.Self.GumProjectSave.Components.Add(bigButton);

        ScreenSave screen = new() { Name = "MyScreenForChainContinues" };
        StateSave screenDefault = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave bigButtonInstance = new()
        {
            Name = "BigButtonInstance",
            BaseType = "BigButtonChainContinues",
            ParentContainer = screen
        };
        screen.Instances.Add(bigButtonInstance);

        screenDefault.Variables.Add(new VariableSave
        {
            Name = "BigButtonInstance.ButtonCategoryState",
            Type = "ButtonCategory",
            Value = "Enabled",
            SetsValue = true
        });

        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);

        var value = screenDefault.GetValueRecursive("BigButtonInstance.X");

        value.ShouldBe(128f,
            "because the Enabled state doesn't set X and BigButton has no override, so the " +
            "fallback chain must continue past BigButton.DefaultState to Button.DefaultState X=128.");
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
