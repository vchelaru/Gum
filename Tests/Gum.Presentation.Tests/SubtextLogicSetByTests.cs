using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>Which categories a row's "Set by" line and color bar name: any state of a category that sets the variable counts (#7647).</summary>
public class SubtextLogicSetByTests
{
    private readonly SubtextLogic _sut = new SubtextLogic();

    private static ComponentSave CreateComponent()
    {
        ComponentSave component = new ComponentSave { Name = "Slider" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
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
    public void GetCategoriesSettingVariable_FirstStateSetsIt_NamesTheCategory()
    {
        ComponentSave element = CreateComponent();
        VariableSave width = SetFloat(element.States[0], "Width");
        StateSaveCategory category = AddCategory(element, "Size", null, "Small", "Large");
        SetFloat(category.States[0], "Width");

        Names(_sut.GetCategoriesSettingVariable(width, "Width", element, null)).ShouldBe(new[] { "Size" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_OnlyALaterStateSetsIt_NamesTheCategory()
    {
        ComponentSave element = CreateComponent();
        VariableSave width = SetFloat(element.States[0], "Width");
        StateSaveCategory category = AddCategory(element, "Size", null, "Small", "Large");
        SetFloat(category.States[1], "Width");

        Names(_sut.GetCategoriesSettingVariable(width, "Width", element, null)).ShouldBe(new[] { "Size" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_NoStateSetsIt_NamesNothing()
    {
        ComponentSave element = CreateComponent();
        VariableSave width = SetFloat(element.States[0], "Width");
        StateSaveCategory category = AddCategory(element, "Size", null, "Small", "Large");
        SetFloat(category.States[1], "Height");

        _sut.GetCategoriesSettingVariable(width, "Width", element, null).ShouldBeEmpty();
    }

    [Fact]
    public void GetCategoriesSettingVariable_SeveralStatesOfOneCategory_NamesItOnce()
    {
        ComponentSave element = CreateComponent();
        VariableSave width = SetFloat(element.States[0], "Width");
        StateSaveCategory category = AddCategory(element, "Size", null, "Small", "Large");
        SetFloat(category.States[0], "Width");
        SetFloat(category.States[1], "Width");

        Names(_sut.GetCategoriesSettingVariable(width, "Width", element, null)).ShouldBe(new[] { "Size" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_SeveralCategories_AreInCategoryOrder()
    {
        ComponentSave element = CreateComponent();
        VariableSave width = SetFloat(element.States[0], "Width");
        StateSaveCategory first = AddCategory(element, "Size", null, "A", "B");
        StateSaveCategory second = AddCategory(element, "Layout", null, "C", "D");
        SetFloat(first.States[1], "Width");
        SetFloat(second.States[0], "Width");

        Names(_sut.GetCategoriesSettingVariable(width, "Width", element, null)).ShouldBe(new[] { "Size", "Layout" });
        _sut.GetDefaultSubtext(width, null, "Width", element, null).ShouldBe("Set by Size, Layout");
    }

    [Fact]
    public void GetCategoriesSettingVariable_InstanceVariableInALaterState_NamesTheCategory()
    {
        ComponentSave element = CreateComponent();
        InstanceSave instance = new InstanceSave { Name = "Track", BaseType = "Rectangle", ParentContainer = element };
        element.Instances.Add(instance);
        VariableSave width = new VariableSave { Name = "Width", Type = "float", Value = 1f };
        StateSaveCategory category = AddCategory(element, "Size", null, "A", "B");
        SetFloat(category.States[1], "Track.Width");

        Names(_sut.GetCategoriesSettingVariable(width, "Width", element, instance)).ShouldBe(new[] { "Size" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_ExposedVariableSetByItsInnerName_NamesTheCategory()
    {
        ComponentSave element = CreateComponent();
        InstanceSave instance = new InstanceSave { Name = "Track", BaseType = "Rectangle", ParentContainer = element };
        element.Instances.Add(instance);
        VariableSave exposed = SetFloat(element.States[0], "Track.Width");
        exposed.ExposedAsName = "TrackWidth";
        StateSaveCategory category = AddCategory(element, "Size", null, "A", "B");
        SetFloat(category.States[1], "Track.Width");

        Names(_sut.GetCategoriesSettingVariable(exposed, "TrackWidth", element, null)).ShouldBe(new[] { "Size" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_ListVariableInTheFirstState_NamesTheCategory()
    {
        ComponentSave element = CreateComponent();
        VariableListSave list = SetList(element.States[0], "VariableReferences");
        StateSaveCategory category = AddCategory(element, "SliderCategory", null, "Enabled", "Disabled");
        SetList(category.States[0], "VariableReferences");

        Names(_sut.GetCategoriesSettingVariable(list, "VariableReferences", element, null)).ShouldBe(new[] { "SliderCategory" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_ListVariableInALaterState_NamesTheCategory()
    {
        ComponentSave element = CreateComponent();
        VariableListSave list = SetList(element.States[0], "VariableReferences");
        StateSaveCategory category = AddCategory(element, "SliderCategory", null, "Enabled", "Disabled");
        SetList(category.States[1], "VariableReferences");

        Names(_sut.GetCategoriesSettingVariable(list, "VariableReferences", element, null)).ShouldBe(new[] { "SliderCategory" });
    }

    [Fact]
    public void GetCategoriesSettingVariable_ListVariableOnlyInDefault_NamesNothing()
    {
        ComponentSave element = CreateComponent();
        VariableListSave list = SetList(element.States[0], "VariableReferences");
        AddCategory(element, "SliderCategory", null, "Enabled", "Disabled");

        _sut.GetCategoriesSettingVariable(list, "VariableReferences", element, null).ShouldBeEmpty();
    }

    [Fact]
    public void GetCategoriesSettingVariable_InstanceListVariableInALaterState_NamesTheCategory()
    {
        ComponentSave element = CreateComponent();
        InstanceSave instance = new InstanceSave { Name = "Track", BaseType = "Rectangle", ParentContainer = element };
        element.Instances.Add(instance);
        VariableListSave list = new VariableListSave<string> { Name = "VariableReferences", Type = "string" };
        StateSaveCategory category = AddCategory(element, "SliderCategory", null, "Enabled", "Disabled");
        SetList(category.States[1], "Track.VariableReferences");

        Names(_sut.GetCategoriesSettingVariable(list, "VariableReferences", element, instance)).ShouldBe(new[] { "SliderCategory" });
    }

    [Fact]
    public void AppendSetBy_ListVariableInALaterState_AddsTheCategoryAfterTheDescription()
    {
        ComponentSave element = CreateComponent();
        VariableListSave list = SetList(element.States[0], "VariableReferences");
        list.DetailText = "Describes it.";
        StateSaveCategory category = AddCategory(element, "SliderCategory", null, "Enabled", "Disabled");
        SetList(category.States[1], "VariableReferences");

        SubtextLogic.AppendSetBy(list.DetailText, _sut.GetCategoriesSettingVariable(list, "VariableReferences", element, null)).ShouldBe("Describes it.\nSet by SliderCategory");
    }

    [Fact]
    public void GetCategoryColorName_ColoredCategorySettingItInALaterState_ColorsTheRow()
    {
        ComponentSave element = CreateComponent();
        VariableSave width = SetFloat(element.States[0], "Width");
        VariableListSave list = SetList(element.States[0], "VariableReferences");
        AddCategory(element, "Uncolored", null, "A", "B");
        StateSaveCategory colored = AddCategory(element, "Size", "Indigo", "C", "D");
        SetFloat(colored.States[1], "Width");
        SetList(colored.States[1], "VariableReferences");

        _sut.GetCategoryColorName(width, "Width", element, null).ShouldBe("Indigo");
        _sut.GetCategoryColorName(list, "VariableReferences", element, null).ShouldBe("Indigo");
    }
}
