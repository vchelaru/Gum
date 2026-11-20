using Gum.DataTypes.Behaviors;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class VariableDefinedByTypeLogicTests : BaseTestClass
{
    private readonly VariableDefinedByTypeLogic _logic = new VariableDefinedByTypeLogic();

    [Fact]
    public void GetIfVariableIsDefinedByType_CategorySelectorOnBase_IsDefined()
    {
        ComponentSave baseComponent = EditingTestProject.AddComponent(Project, "Base");
        baseComponent.Categories.Add(new StateSaveCategory { Name = "Size" });
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", baseType: "Base");

        _logic.GetIfVariableIsDefinedByType("SizeState", derived).ShouldBeTrue();
    }

    [Fact]
    public void GetIfVariableIsDefinedByType_CustomVariableOnBase_IsDefined()
    {
        ComponentSave baseComponent = EditingTestProject.AddComponent(Project, "Base");
        baseComponent.DefaultState.Variables.Add(new VariableSave { Name = "Score", Type = "int", IsCustomVariable = true });
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", baseType: "Base");

        _logic.GetIfVariableIsDefinedByType("Score", derived).ShouldBeTrue();
    }

    [Fact]
    public void GetVariablesDefinedByType_FormsPropertyOfABehaviorOnTheBase_IsListedWithItsDeclaredTypeOnceAndAVisualNameStaysTheVisual()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "SliderBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Name = "Minimum", Type = "double", Value = 0.0 });
        behavior.FormsProperties.Add(new VariableSave { Name = "Width", Type = "string" });
        Project.Behaviors.Add(behavior);
        ComponentSave baseComponent = EditingTestProject.AddComponent(Project, "Base");
        baseComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "SliderBehavior" });
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", baseType: "Base");
        ComponentSave plain = EditingTestProject.AddComponent(Project, "Plain");

        List<DefinedVariable> variables = _logic.GetVariablesDefinedByType(derived).ToList();

        variables.Select(item => item.Name).ShouldBeUnique();
        variables.ShouldContain(item => item.Name == "Minimum" && item.Type == "double");
        variables.ShouldContain(item => item.Name == "Width" && item.Type == "float");
        _logic.GetIfVariableIsDefinedByType("Minimum", derived).ShouldBeTrue();
        _logic.GetIfVariableIsDefinedByType("Minimum", plain).ShouldBeFalse();
    }

    [Fact]
    public void GetIfVariableIsDefinedByType_MisspelledName_IsNotDefined()
    {
        ComponentSave component = EditingTestProject.AddComponent(Project, "Hud");

        _logic.GetIfVariableIsDefinedByType("Width", component).ShouldBeTrue();
        _logic.GetIfVariableIsDefinedByType("Widht", component).ShouldBeFalse();
    }
    [Fact]
    public void GetVariablesDefinedByType_DerivedComponent_ListsEachKindOnceWithItsType()
    {
        ComponentSave baseComponent = EditingTestProject.AddComponent(Project, "Base");
        baseComponent.Categories.Add(new StateSaveCategory { Name = "Size" });
        baseComponent.DefaultState.Variables.Add(new VariableSave { Name = "Score", Type = "int", IsCustomVariable = true });
        EditingTestProject.AddInstance(baseComponent, "Label", "Text");
        baseComponent.DefaultState.Variables.Add(new VariableSave { Name = "Label.Text", Type = "string", ExposedAsName = "LabelText" });
        ComponentSave derived = EditingTestProject.AddComponent(Project, "Derived", baseType: "Base");

        List<DefinedVariable> variables = _logic.GetVariablesDefinedByType(derived).ToList();

        variables.Select(item => item.Name).ShouldBeUnique();
        variables.ShouldContain(item => item.Name == "State" && item.Type == "State");
        variables.ShouldContain(item => item.Name == "SizeState" && item.Type == "Size" && item.Category!.Name == "Size");
        variables.ShouldContain(item => item.Name == "Score" && item.Type == "int");
        variables.ShouldContain(item => item.Name == "LabelText" && item.Type == "string");
        variables.ShouldContain(item => item.Name == "Width" && item.Type == "float");
        variables.ShouldNotContain(item => item.Name == "Label.Text");
    }
}
