using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A shader parameter variable (<c>Icon.Outline_Thickness</c>) exists only in the container that saved it, so no
/// root variable lookup through the instance's type finds it. <see cref="ObjectFinder.GetRootVariable(string, InstanceSave)"/>
/// answers from the saved, marked variable (#7010).
/// </summary>
public class ShaderParameterDefinitionHookTests : BaseTestClass
{
    private const string SavedName = "Icon.Outline_Thickness";

    private readonly ComponentSave _panel;
    private readonly InstanceSave _icon;

    public ShaderParameterDefinitionHookTests()
    {
        _panel = AddComponent("Panel", "Container");
        _icon = new InstanceSave { Name = "Icon", BaseType = "Container", ParentContainer = _panel };
        _panel.Instances.Add(_icon);
        ObjectFinder.Self.GumProjectSave = Project;
    }

    private ComponentSave AddComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        return component;
    }

    private VariableSave AddVariable(ElementSave element, string name, string? marker, bool setsValue, object? value = null, string? exposedAs = null)
    {
        VariableSave variable = new VariableSave
        {
            Name = name,
            Type = "float",
            ShaderParameter = marker,
            SetsValue = setsValue,
            Value = value,
            ExposedAsName = exposedAs
        };
        element.DefaultState!.Variables.Add(variable);
        return variable;
    }

    #region Instance overload

    [Fact]
    public void GetRootVariable_ShouldReturnRootShapedDefinition_WhenMarkedPlaceholderIsSaved()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        VariableSave? definition = ObjectFinder.Self.GetRootVariable(SavedName, _icon);

        definition.ShouldNotBeNull();
        definition.Name.ShouldBe("Outline_Thickness");
        definition.Type.ShouldBe("float");
        definition.ShaderParameter.ShouldBe("Thickness");
    }

    [Fact]
    public void GetRootVariable_ShouldReturnDefinition_WhenMarkedVariableHasValue()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: true, value: 3f);

        VariableSave? definition = ObjectFinder.Self.GetRootVariable(SavedName, _icon);

        definition.ShouldNotBeNull();
        definition.Name.ShouldBe("Outline_Thickness");
        definition.Type.ShouldBe("float");
    }

    [Fact]
    public void GetRootVariable_ShouldReturnDefinition_WhenSavedEntryIsOneVectorComponent()
    {
        AddVariable(_panel, "Icon.Outline_Tint__X", "Tint#0/4", setsValue: false);

        VariableSave? definition = ObjectFinder.Self.GetRootVariable("Icon.Outline_Tint__X", _icon);

        definition.ShouldNotBeNull();
        definition.Name.ShouldBe("Outline_Tint__X");
        definition.ShaderParameter.ShouldBe("Tint#0/4");
    }

    [Fact]
    public void GetRootVariable_ShouldReturnNull_WhenNameIsATypo()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        ObjectFinder.Self.GetRootVariable("Icon.Outline_Thicknes", _icon).ShouldBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldReturnNull_WhenPlaceholderHasNoMarker()
    {
        AddVariable(_panel, SavedName, marker: null, setsValue: false);

        ObjectFinder.Self.GetRootVariable(SavedName, _icon).ShouldBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldReturnNull_WhenParameterWasNeverNamed()
    {
        ObjectFinder.Self.GetRootVariable(SavedName, _icon).ShouldBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldReturnNull_WhenMarkedVariableBelongsToAnotherInstance()
    {
        AddVariable(_panel, "Other.Outline_Thickness", "Thickness", setsValue: false);

        ObjectFinder.Self.GetRootVariable(SavedName, _icon).ShouldBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldPreferTheTypesOwnVariable_WhenBothExist()
    {
        // A name the type itself declares keeps resolving to the type's variable.
        StandardElementSave container = Project.StandardElements.Single(item => item.Name == "Container");
        container.DefaultState!.Variables.Add(new VariableSave { Name = "Outline_Thickness", Type = "int" });
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        ObjectFinder.Self.GetRootVariable(SavedName, _icon)!.Type.ShouldBe("int");
    }

    #endregion

    #region Element overload

    [Fact]
    public void GetRootVariable_ShouldReturnDefinition_WhenElementOverloadReceivesQualifiedName()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        ObjectFinder.Self.GetRootVariable(SavedName, _panel)!.Type.ShouldBe("float");
    }

    [Fact]
    public void GetRootVariable_ShouldReturnDefinition_WhenNameIsExposedByTheContainer()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: false, exposedAs: "Thick");

        VariableSave? definition = ObjectFinder.Self.GetRootVariable("Thick", _panel);

        definition.ShouldNotBeNull();
        definition.Name.ShouldBe("Outline_Thickness");
        definition.Type.ShouldBe("float");
    }

    [Fact]
    public void GetRootVariable_ShouldReturnDefinition_WhenExposedNameIsReadThroughAnInstanceOfTheContainer()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: false, exposedAs: "Thick");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Panel1", BaseType = "Panel", ParentContainer = screen });
        Project.Screens.Add(screen);

        VariableSave? definition = ObjectFinder.Self.GetRootVariable("Panel1.Thick", screen);

        definition.ShouldNotBeNull();
        definition.Type.ShouldBe("float");
    }

    [Fact]
    public void GetRootVariable_ShouldReturnNull_WhenExposedNameHasNoMarker()
    {
        AddVariable(_panel, SavedName, marker: null, setsValue: false, exposedAs: "Thick");

        ObjectFinder.Self.GetRootVariable("Thick", _panel).ShouldBeNull();
    }

    [Fact]
    public void GetShaderParameterDefinition_ShouldSearchBaseElements()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");

        VariableSave? definition = ObjectFinder.Self.GetShaderParameterDefinition(SavedName, derived);

        definition.ShouldNotBeNull();
        definition.Name.ShouldBe("Outline_Thickness");
    }

    [Fact]
    public void GetShaderParameterDefinition_ShouldReturnNull_WhenNoElementInTheChainSavedIt()
    {
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");

        ObjectFinder.Self.GetShaderParameterDefinition(SavedName, derived).ShouldBeNull();
    }

    [Fact]
    public void GetShaderParameterDefinition_ShouldReturnNull_WhenNameHasNoInstancePart()
    {
        AddVariable(_panel, "Outline_Thickness", "Thickness", setsValue: false);

        ObjectFinder.Self.GetShaderParameterDefinition("Outline_Thickness", _panel).ShouldBeNull();
    }

    #endregion

    #region Instance owned by a base element, entry saved by a derived one

    [Fact]
    public void GetRootVariable_ShouldReturnDefinition_WhenEditingElementIsDerivedAndSavedTheEntry()
    {
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");
        AddVariable(derived, "Icon.Outline_Derived", "Derived", setsValue: false);

        VariableSave? definition = ObjectFinder.Self.GetRootVariable("Icon.Outline_Derived", _icon, derived);

        definition.ShouldNotBeNull();
        definition.Name.ShouldBe("Outline_Derived");
        definition.Type.ShouldBe("float");
    }

    [Fact]
    public void GetRootVariable_ShouldStillFindEntryOfInstancesOwnContainer_WhenEditingElementIsGiven()
    {
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        ObjectFinder.Self.GetRootVariable(SavedName, _icon, derived).ShouldNotBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldPreferDerivedEntry_WhenBothSavedOne()
    {
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");
        AddVariable(_panel, SavedName, "Thickness", setsValue: false).Type = "float";
        AddVariable(derived, SavedName, "Thickness", setsValue: false).Type = "int";

        ObjectFinder.Self.GetRootVariable(SavedName, _icon, derived)!.Type.ShouldBe("int");
    }

    [Fact]
    public void GetRootVariable_ShouldReturnNull_WhenOnlyTheInstanceIsGiven_AndTheDerivedElementSavedTheEntry()
    {
        // Without the editing element the instance's own container chain is all that is known.
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");
        AddVariable(derived, "Icon.Outline_Derived", "Derived", setsValue: false);

        ObjectFinder.Self.GetRootVariable("Icon.Outline_Derived", _icon).ShouldBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldReturnDefinition_WhenElementOverloadIsGivenDerivedElementAndBaseInstance()
    {
        // ElementSave.GetInstance ignores base instances, so the element overload finds the instance in the chain.
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");
        AddVariable(derived, "Icon.Outline_Derived", "Derived", setsValue: false);

        ObjectFinder.Self.GetRootVariable("Icon.Outline_Derived", derived).ShouldNotBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldReturnNull_WhenElementOverloadNamesNoInstanceInTheChain()
    {
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");
        AddVariable(derived, "Ghost.Outline_Derived", "Derived", setsValue: false);

        ObjectFinder.Self.GetRootVariable("Ghost.Outline_Derived", derived).ShouldBeNull();
    }

    [Fact]
    public void SetValue_ShouldStampMarker_WhenDerivedCategoricalStateWritesBaseInstanceEntryTheDerivedDefaultSaved()
    {
        ComponentSave derived = AddComponent("DerivedPanel", "Panel");
        AddVariable(derived, "Icon.Outline_Derived", "Derived", setsValue: false);
        StateSaveCategory category = new StateSaveCategory { Name = "Look" };
        StateSave state = new StateSave { Name = "Bold", ParentContainer = derived };
        category.States.Add(state);
        derived.Categories.Add(category);

        state.SetValue("Icon.Outline_Derived", 5f, _icon, "float");

        state.GetVariableSave("Icon.Outline_Derived")!.ShaderParameter.ShouldBe("Derived");
    }

    #endregion

    #region Unqualified names

    [Fact]
    public void GetRootVariable_ShouldTakeRootName_WhenInstanceOverloadIsGivenUnqualifiedName()
    {
        // The instance overload takes the instance's variable either qualified or by root name: callers such as
        // codegen and the variable grid hand it root names, and the instance supplies the prefix.
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        ObjectFinder.Self.GetRootVariable("Outline_Thickness", _icon).ShouldNotBeNull();
    }

    [Fact]
    public void GetRootVariable_ShouldReturnNull_WhenElementOverloadIsGivenUnqualifiedName()
    {
        // The element has no instance to prefix, so a bare root name names nothing.
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        ObjectFinder.Self.GetRootVariable("Outline_Thickness", _panel).ShouldBeNull();
    }

    #endregion

    #region Existing lookups stay the same

    [Fact]
    public void GetValueRecursive_ShouldReturnNull_WhenOnlyAPlaceholderIsSaved()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        _panel.DefaultState!.GetValueRecursive(SavedName).ShouldBeNull();
    }

    [Fact]
    public void GetValueRecursive_ShouldReturnSavedValue_WhenMarkedVariableHasOne()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: true, value: 3f);

        _panel.DefaultState!.GetValueRecursive(SavedName).ShouldBe(3f);
    }

    [Fact]
    public void GetVariableRecursive_ShouldFindSavedEntryByItsQualifiedName()
    {
        VariableSave saved = AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        _panel.DefaultState!.GetVariableRecursive(SavedName).ShouldBeSameAs(saved);
    }

    [Fact]
    public void GetVariableRecursive_ShouldReturnNull_WhenNothingIsSaved()
    {
        _panel.DefaultState!.GetVariableRecursive(SavedName).ShouldBeNull();
    }

    #endregion

    #region Marker stamping

    [Fact]
    public void SetValue_ShouldStampMarker_WhenCategoricalStateWritesAnEntryTheDefaultStateSaved()
    {
        AddVariable(_panel, SavedName, "Thickness", setsValue: false);
        StateSaveCategory category = new StateSaveCategory { Name = "Look" };
        StateSave state = new StateSave { Name = "Bold", ParentContainer = _panel };
        category.States.Add(state);
        _panel.Categories.Add(category);

        state.SetValue(SavedName, 5f, _icon, "float");

        state.GetVariableSave(SavedName)!.ShaderParameter.ShouldBe("Thickness");
    }

    [Fact]
    public void SetValue_ShouldKeepMarker_WhenWritingOverTheSavedEntry()
    {
        VariableSave saved = AddVariable(_panel, SavedName, "Thickness", setsValue: false);

        _panel.DefaultState!.SetValue(SavedName, 4f, _icon, "float");

        saved.ShaderParameter.ShouldBe("Thickness");
        saved.Value.ShouldBe(4f);
    }

    [Fact]
    public void SetValue_ShouldNotStampMarker_WhenNothingNamedTheParameter()
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Look" };
        StateSave state = new StateSave { Name = "Bold", ParentContainer = _panel };
        category.States.Add(state);
        _panel.Categories.Add(category);

        state.SetValue(SavedName, 5f, _icon, "float");

        state.GetVariableSave(SavedName)!.ShaderParameter.ShouldBeNull();
    }

    [Fact]
    public void SetValue_ShouldNotStampMarker_WhenWritingOrdinaryVariable()
    {
        _panel.DefaultState!.SetValue("Icon.X", 5f, _icon, "float");

        _panel.DefaultState.GetVariableSave("Icon.X")!.ShaderParameter.ShouldBeNull();
    }

    #endregion
}
