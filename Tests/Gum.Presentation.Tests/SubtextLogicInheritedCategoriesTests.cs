using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>"Set by" and the color bar also name categories inherited through the BaseType chain (#7657).</summary>
public class SubtextLogicInheritedCategoriesTests : BaseTestClass
{
    private readonly SubtextLogic _sut = new SubtextLogic();
    private readonly GumProjectSave _project = new GumProjectSave();

    public SubtextLogicInheritedCategoriesTests()
    {
        ObjectFinder.Self.GumProjectSave = _project;
    }

    private ComponentSave CreateComponent(string name, string? baseType = null)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        _project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    private static StateSaveCategory AddCategory(ComponentSave owner, string name, string? color, params string[] stateNames)
    {
        StateSaveCategory category = new StateSaveCategory { Name = name, ColorName = color };
        foreach (string stateName in stateNames)
        {
            category.States.Add(new StateSave { Name = stateName, ParentContainer = owner });
        }
        owner.Categories.Add(category);
        return category;
    }

    private static VariableSave SetFloat(StateSave state, string name)
    {
        VariableSave variable = new VariableSave { Name = name, Type = "float", Value = 1f, SetsValue = true };
        state.Variables.Add(variable);
        return variable;
    }

    private static VariableListSave SetList(StateSave state, string name)
    {
        VariableListSave<string> list = new VariableListSave<string> { Name = name, Type = "string", Value = new List<string> { "A = B" } };
        state.VariableLists.Add(list);
        return list;
    }

    private static List<string> Names(List<StateSaveCategory> categories) => categories.Select(c => c.Name).ToList();

    [Fact]
    public void GetCategoriesSettingVariable_BaseCategorySetsItInALaterState_NamesTheCategory()
    {
        ComponentSave baseComponent = CreateComponent("Base");
        StateSaveCategory hover = AddCategory(baseComponent, "Hover", "Indigo", "Off", "On");
        SetFloat(hover.States[1], "Width");
        ComponentSave derived = CreateComponent("Derived", "Base");
        VariableSave width = SetFloat(derived.States[0], "Width");

        Names(_sut.GetCategoriesSettingVariable(width, "Width", derived, null)).ShouldBe(new[] { "Hover" });
        _sut.GetDefaultSubtext(width, null, "Width", derived, null).ShouldBe("Set by Hover");
        _sut.GetCategoryColorName(width, "Width", derived, null).ShouldBe("Indigo");
    }

    [Fact]
    public void GetCategoriesSettingVariable_DerivedRedefinesCategoryOfSameName_OnlyDerivedStatesCount()
    {
        ComponentSave baseComponent = CreateComponent("Base");
        StateSaveCategory baseHover = AddCategory(baseComponent, "Hover", "Indigo", "Off", "On");
        SetFloat(baseHover.States[1], "Width");
        ComponentSave derived = CreateComponent("Derived", "Base");
        StateSaveCategory derivedHover = AddCategory(derived, "Hover", "Rose", "Off", "On");
        SetFloat(derivedHover.States[1], "Height");
        VariableSave width = SetFloat(derived.States[0], "Width");
        VariableSave height = SetFloat(derived.States[0], "Height");

        _sut.GetCategoriesSettingVariable(width, "Width", derived, null).ShouldBeEmpty();
        Names(_sut.GetCategoriesSettingVariable(height, "Height", derived, null)).ShouldBe(new[] { "Hover" });
        _sut.GetCategoryColorName(height, "Height", derived, null).ShouldBe("Rose");
    }

    [Fact]
    public void GetCategoriesSettingVariable_TwoInheritanceLevels_NamesDerivedFirstThenBases()
    {
        ComponentSave grand = CreateComponent("Grand");
        StateSaveCategory grandCategory = AddCategory(grand, "Grand", null, "A", "B");
        SetFloat(grandCategory.States[1], "Width");
        ComponentSave middle = CreateComponent("Middle", "Grand");
        StateSaveCategory middleCategory = AddCategory(middle, "Middle", null, "A", "B");
        SetFloat(middleCategory.States[1], "Width");
        ComponentSave derived = CreateComponent("Derived", "Middle");
        StateSaveCategory ownCategory = AddCategory(derived, "Own", null, "A", "B");
        SetFloat(ownCategory.States[0], "Width");
        VariableSave width = SetFloat(derived.States[0], "Width");

        Names(_sut.GetCategoriesSettingVariable(width, "Width", derived, null)).ShouldBe(new[] { "Own", "Middle", "Grand" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_InstanceInDerivedComponent_NamesBaseCategory()
    {
        ComponentSave baseComponent = CreateComponent("Base");
        InstanceSave baseTrack = new InstanceSave { Name = "Track", BaseType = "Rectangle", ParentContainer = baseComponent };
        baseComponent.Instances.Add(baseTrack);
        StateSaveCategory category = AddCategory(baseComponent, "Size", "Indigo", "A", "B");
        SetFloat(category.States[1], "Track.Width");
        ComponentSave derived = CreateComponent("Derived", "Base");
        VariableSave width = new VariableSave { Name = "Width", Type = "float", Value = 1f };

        Names(_sut.GetCategoriesSettingVariable(width, "Width", derived, baseTrack)).ShouldBe(new[] { "Size" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_ListVariableSetByBaseCategory_NamesTheCategory()
    {
        ComponentSave baseComponent = CreateComponent("Base");
        StateSaveCategory category = AddCategory(baseComponent, "SliderCategory", "Indigo", "Enabled", "Disabled");
        SetList(category.States[1], "VariableReferences");
        ComponentSave derived = CreateComponent("Derived", "Base");
        VariableListSave list = SetList(derived.States[0], "VariableReferences");

        Names(_sut.GetCategoriesSettingVariable(list, "VariableReferences", derived, null)).ShouldBe(new[] { "SliderCategory" });
        _sut.GetCategoryColorName(list, "VariableReferences", derived, null).ShouldBe("Indigo");
    }

    [Fact]
    public void GetCategoriesSettingVariable_OwnCategoryAndNoBase_StillNamesOwnCategory()
    {
        ComponentSave component = CreateComponent("Solo");
        StateSaveCategory category = AddCategory(component, "Size", null, "A", "B");
        SetFloat(category.States[1], "Width");
        VariableSave width = SetFloat(component.States[0], "Width");

        Names(_sut.GetCategoriesSettingVariable(width, "Width", component, null)).ShouldBe(new[] { "Size" });
    }
}
