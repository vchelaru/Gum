using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class CustomVariableLogicTests : BaseTestClass
{
    [Fact]
    public void AddCustomVariable_MarksCustomAndKeepsVariablesSorted()
    {
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        hud.DefaultState.Variables.Add(new VariableSave { Name = "Z", Type = "float" });
        CustomVariableLogic logic = new CustomVariableLogic(new StateVariableSorter());

        VariableSave added = logic.AddCustomVariable(hud, "Score", "int", logic.GetDefaultValue("int"));

        added.IsCustomVariable.ShouldBeTrue();
        added.Value.ShouldBe(10);
        hud.DefaultState.Variables.Select(item => item.Name).ShouldBe(new[] { "Score", "Z" });
        logic.AvailableTypes.ShouldBe(new[] { "float", "int", "string", "bool" });
    }
}
