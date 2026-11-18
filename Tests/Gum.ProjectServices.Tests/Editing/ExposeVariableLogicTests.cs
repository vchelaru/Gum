using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class ExposeVariableLogicTests : BaseTestClass
{
    [Fact]
    public void Expose_UnsetVariable_AddsItWithoutSettingAValue()
    {
        Project.StandardElements.Single(item => item.Name == "Text").DefaultState.Variables
            .Add(new VariableSave { Name = "Text", Type = "string", Value = "Hello" });
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        InstanceSave counter = new InstanceSave { Name = "Counter1", BaseType = "Text", ParentContainer = hud };
        hud.Instances.Add(counter);
        ExposeVariableLogic logic = new ExposeVariableLogic();

        VariableSave exposed = logic.Expose(hud, counter, "Text", "Counter1Text");

        exposed.Name.ShouldBe("Counter1.Text");
        exposed.Type.ShouldBe("string");
        exposed.SetsValue.ShouldBeFalse();
        exposed.ExposedAsName.ShouldBe("Counter1Text");
        hud.DefaultState.Variables.ShouldContain(exposed);
        logic.GetUnauthoredVariableType(counter, "Nope", hud).ShouldBeNull();
    }
}
