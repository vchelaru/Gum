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
    public void GetIfVariableIsDefinedByType_MisspelledName_IsNotDefined()
    {
        ComponentSave component = EditingTestProject.AddComponent(Project, "Hud");

        _logic.GetIfVariableIsDefinedByType("Width", component).ShouldBeTrue();
        _logic.GetIfVariableIsDefinedByType("Widht", component).ShouldBeFalse();
    }
}
